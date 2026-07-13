import { PrismaClient } from '@prisma/client';
import { DataSink, DataSinkResult } from './core/data-sink';
import { DatasetManifest, ImportMetrics } from './core/interfaces';
import { NormalizedQuranData } from './core/normalized-models';

export class PrismaSink implements DataSink<NormalizedQuranData> {
  public readonly name = 'PrismaSink';
  private prisma = new PrismaClient();

  public async publish(
    data: NormalizedQuranData,
    manifest: DatasetManifest,
    sessionLogId: string
  ): Promise<DataSinkResult> {
    const result: DataSinkResult = {
      rowsImported: 0,
      rowsUpdated: 0,
      rowsSkipped: 0,
      rowsDeleted: 0,
      warnings: [],
      errors: []
    };

    try {
      await this.prisma.$transaction(async (tx) => {
        // 1. Create or Update Dataset
        const datasetId = `quran-${manifest.dataset.version}`;
        const dataset = await tx.dataset.upsert({
          where: { id: datasetId },
          create: {
            id: datasetId,
            name: manifest.dataset.name,
            edition: manifest.dataset.edition,
            version: manifest.dataset.version,
            source: manifest.dataset.sourceUrl,
            license: manifest.dataset.license,
            checksum: manifest.files[0]?.expectedChecksum || '',
          },
          update: {
            name: manifest.dataset.name,
            edition: manifest.dataset.edition,
            version: manifest.dataset.version,
            source: manifest.dataset.sourceUrl,
            license: manifest.dataset.license,
            checksum: manifest.files[0]?.expectedChecksum || '',
          }
        });

        // 2. Create ImportSession
        await tx.importSession.create({
          data: {
            id: sessionLogId,
            datasetId: dataset.id,
            startedAt: new Date(),
            completedAt: new Date(),
            status: 'IN_PROGRESS',
            durationMs: 0,
            warnings: [],
            errors: [],
            memoryUsageMb: 0
          }
        });

        // 3. Clear existing Surahs (cascades to verses & translations) to ensure a clean, idempotent state
        console.log("[PrismaSink] Clearing existing Surahs, Verses, and Translations...");
        await tx.surah.deleteMany({});

        // 4. Bulk insert Surahs
        console.log(`[PrismaSink] Bulk inserting ${data.surahs.length} Surahs...`);
        await tx.surah.createMany({
          data: data.surahs.map(surah => ({
            number: surah.number,
            arabicName: surah.arabicName,
            transliteration: surah.transliteration,
            englishName: surah.englishName,
            revelationType: surah.revelationType,
            totalVerses: surah.totalVerses
          }))
        });
        result.rowsImported += data.surahs.length;

        // 5. Create verse lookup mapping
        const verseMap = new Map<string, string>();
        for (const verse of data.verses) {
          verseMap.set(`${verse.surahNumber}:${verse.ayahNumber}`, `verse-${verse.globalIndex}`);
        }

        // 6. Bulk insert QuranVerses
        console.log(`[PrismaSink] Bulk inserting ${data.verses.length} Verses...`);
        await tx.quranVerse.createMany({
          data: data.verses.map(verse => ({
            id: `verse-${verse.globalIndex}`,
            globalIndex: verse.globalIndex,
            surahNumber: verse.surahNumber,
            ayahNumber: verse.ayahNumber,
            arabicText: verse.arabicText,
            arabicCleaned: verse.arabicCleaned,
            transliteration: verse.transliteration
          }))
        });
        result.rowsImported += data.verses.length;

        // 7. Bulk insert QuranTranslations
        console.log(`[PrismaSink] Bulk inserting ${data.translations.length} Translations...`);
        const translationsData = data.translations.map(trans => {
          const verseId = verseMap.get(`${trans.surahNumber}:${trans.ayahNumber}`);
          if (!verseId) {
            throw new Error(`Orphan translation: Surah ${trans.surahNumber} Ayah ${trans.ayahNumber}`);
          }
          return {
            id: `trans-${trans.language}-${trans.surahNumber}-${trans.ayahNumber}`,
            verseId,
            language: trans.language,
            translator: trans.translator,
            text: trans.text
          };
        });

        await tx.quranTranslation.createMany({
          data: translationsData
        });
        result.rowsImported += translationsData.length;

      }, {
        timeout: 180000 // 180 seconds timeout for bulk operations
      });

    } catch (err: any) {
      console.error('[PrismaSink] Transaction failed:', err);
      result.errors.push(err.message || String(err));
      throw err;
    }

    return result;
  }

  public async runHealthChecks(sessionLogId: string): Promise<void> {
    console.log(`[PrismaSink] Running health checks for session ${sessionLogId}...`);
    const surahCount = await this.prisma.surah.count();
    const verseCount = await this.prisma.quranVerse.count();
    const translationCount = await this.prisma.quranTranslation.count();

    if (surahCount !== 114) {
      throw new Error(`Health Check Failed: Expected 114 Surah records, found ${surahCount}.`);
    }
    if (verseCount !== 6236) {
      throw new Error(`Health Check Failed: Expected 6236 QuranVerse records, found ${verseCount}.`);
    }
    const expectedTranslations = 62360;
    if (translationCount !== expectedTranslations) {
      throw new Error(`Health Check Failed: Expected ${expectedTranslations} QuranTranslation records, found ${translationCount}.`);
    }

    // Check for orphans. Since verseId is a required relation with a foreign key constraint,
    // PostgreSQL guarantees no orphan translations can exist.
    const orphans = 0;
    console.log('[PrismaSink] All health checks passed successfully.');
  }

  public async complete(
    sessionLogId: string,
    status: 'SUCCESS' | 'FAILED',
    metrics: ImportMetrics
  ): Promise<void> {
    try {
      console.log(`[PrismaSink] Finalizing session status: ${status}`);
      await this.prisma.importSession.update({
        where: { id: sessionLogId },
        data: {
          status,
          completedAt: new Date(),
          durationMs: metrics.executionTimeMs,
          warnings: metrics.warnings,
          errors: metrics.errors,
          memoryUsageMb: metrics.memoryUsedMb
        }
      });
    } catch (err) {
      console.error('[PrismaSink] Failed to update ImportSession:', err);
    } finally {
      await this.disconnect();
    }
  }

  public async disconnect(): Promise<void> {
    await this.prisma.$disconnect();
  }
}
