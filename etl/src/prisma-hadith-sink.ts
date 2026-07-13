import { PrismaClient } from '@prisma/client';
import { DataSink, DataSinkResult } from './core/data-sink';
import { DatasetManifest, ImportMetrics } from './core/interfaces';
import { NormalizedHadithData } from './core/normalized-models';

export class PrismaHadithSink implements DataSink<NormalizedHadithData> {
  public readonly name = 'PrismaHadithSink';
  private prisma = new PrismaClient();

  public async publish(
    data: NormalizedHadithData,
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
        // 1. Create or Update Dataset record
        const datasetId = `hadith-${data.collection.slug}-${manifest.dataset.version}`;
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

        // 3. Clean existing collection to support idempotence
        console.log(`[PrismaHadithSink] Clearing existing Hadiths for collection: ${data.collection.slug}...`);
        const existingColl = await tx.hadithCollection.findUnique({
          where: { slug: data.collection.slug }
        });
        if (existingColl) {
          await tx.hadithCollection.delete({
            where: { id: existingColl.id }
          });
        }

        // 4. Create collection
        console.log(`[PrismaHadithSink] Creating Collection: ${data.collection.displayName}`);
        const collection = await tx.hadithCollection.create({
          data: {
            slug: data.collection.slug,
            shortName: data.collection.shortName,
            displayName: data.collection.displayName,
            titleArabic: data.collection.titleArabic,
            titleEnglish: data.collection.titleEnglish,
            authorArabic: data.collection.authorArabic,
            authorEnglish: data.collection.authorEnglish,
            introductionArabic: data.collection.introductionArabic,
            introductionEnglish: data.collection.introductionEnglish,
            totalHadiths: data.collection.totalHadiths
          }
        });
        result.rowsImported += 1;

        // 5. Bulk insert Books
        console.log(`[PrismaHadithSink] Bulk inserting ${data.books.length} Books...`);
        const bookMap = new Map<number, string>();
        
        for (const book of data.books) {
          const createdBook = await tx.hadithBook.create({
            data: {
              collectionId: collection.id,
              bookNumber: book.bookNumber,
              titleArabic: book.titleArabic,
              titleEnglish: book.titleEnglish
            }
          });
          bookMap.set(book.bookNumber, createdBook.id);
        }
        result.rowsImported += data.books.length;

        // 6. Bulk insert Chapters
        console.log(`[PrismaHadithSink] Bulk inserting ${data.chapters.length} Chapters...`);
        const chapterMap = new Map<string, string>();
        
        for (const ch of data.chapters) {
          const bookId = bookMap.get(ch.bookNumber);
          if (!bookId) {
            throw new Error(`Orphan chapter: Book number ${ch.bookNumber} not found.`);
          }

          const createdChapter = await tx.hadithChapter.create({
            data: {
              bookId,
              chapterNumber: ch.chapterNumber,
              titleArabic: ch.titleArabic,
              titleEnglish: ch.titleEnglish
            }
          });
          chapterMap.set(`${ch.bookNumber}:${ch.chapterNumber}`, createdChapter.id);
        }
        result.rowsImported += data.chapters.length;

        // 7. Bulk insert Hadiths
        console.log(`[PrismaHadithSink] Bulk inserting ${data.hadiths.length} Hadiths...`);
        
        const hadithsData = data.hadiths.map((h, i) => {
          const bookId = bookMap.get(h.bookNumber);
          const chapterId = chapterMap.get(`${h.bookNumber}:${h.chapterNumber}`);
          if (!bookId || !chapterId) {
            throw new Error(`Orphan hadith: Book ${h.bookNumber} Chapter ${h.chapterNumber} not found.`);
          }

          return {
            collectionId: collection.id,
            bookId,
            chapterId,
            hadithNumber: h.hadithNumber,
            canonicalNumber: h.canonicalNumber,
            originalNumber: h.originalNumber,
            arabicText: h.arabicText,
            arabicCleaned: h.arabicCleaned,
            englishNarrator: h.englishNarrator,
            englishText: h.englishText
          };
        });

        // Batch inserts of 1000 items to avoid query parameter limits
        const batchSize = 1000;
        for (let idx = 0; idx < hadithsData.length; idx += batchSize) {
          const batch = hadithsData.slice(idx, idx + batchSize);
          await tx.hadith.createMany({
            data: batch
          });
        }
        result.rowsImported += hadithsData.length;

      }, {
        timeout: 240000 // 4 minutes timeout for bulk operations
      });

    } catch (err: any) {
      console.error('[PrismaHadithSink] Transaction failed:', err);
      result.errors.push(err.message || String(err));
      throw err;
    }

    return result;
  }

  public async runHealthChecks(sessionLogId: string): Promise<void> {
    console.log(`[PrismaHadithSink] Running health checks for session ${sessionLogId}...`);
    const collections = await this.prisma.hadithCollection.findMany();
    
    for (const coll of collections) {
      const hadithCount = await this.prisma.hadith.count({
        where: { collectionId: coll.id }
      });
      if (hadithCount !== coll.totalHadiths) {
        throw new Error(`Health Check Failed: Collection ${coll.displayName} expected ${coll.totalHadiths} records, found ${hadithCount}.`);
      }
    }
    console.log('[PrismaHadithSink] All health checks passed successfully.');
  }

  public async complete(
    sessionLogId: string,
    status: 'SUCCESS' | 'FAILED',
    metrics: ImportMetrics
  ): Promise<void> {
    try {
      console.log(`[PrismaHadithSink] Finalizing session status: ${status}`);
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
      console.error('[PrismaHadithSink] Failed to update ImportSession:', err);
    } finally {
      await this.disconnect();
    }
  }

  public async disconnect(): Promise<void> {
    await this.prisma.$disconnect();
  }
}
