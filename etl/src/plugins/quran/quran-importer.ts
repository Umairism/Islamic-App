import * as fs from 'fs/promises';
import * as path from 'path';
import { BaseImporter } from '../../core/base-importer';
import { DatasetManifest, ValidationReport, ImportMetrics } from '../../core/interfaces';
import { NormalizedQuranData, NormalizedSurah, NormalizedVerse, NormalizedTranslation } from '../../core/normalized-models';
import { ValidationError, ChecksumError } from '../../core/errors';

export class QuranImporter extends BaseImporter<any, NormalizedQuranData> {
  protected manifestPath: string;
  protected dateFolder: string;

  constructor() {
    super();
    this.manifestPath = path.resolve(process.cwd(), 'raw/quran/manifest.json');
    this.dateFolder = new Date().toISOString().split('T')[0];
  }

  protected async loadManifest(): Promise<DatasetManifest> {
    const content = await fs.readFile(this.manifestPath, 'utf-8');
    return JSON.parse(content) as DatasetManifest;
  }

  protected async verifyChecksums(manifest: DatasetManifest): Promise<void> {
    const manifestDir = path.dirname(this.manifestPath);
    
    for (const file of manifest.files) {
      const filePath = path.join(manifestDir, file.path);
      
      try {
        const checksum = await this.calculateChecksum(filePath);
        if (checksum !== file.expectedChecksum) {
          throw new ChecksumError(
            `Checksum mismatch for file ${file.path}. Expected: ${file.expectedChecksum}, Found: ${checksum}`
          );
        }
      } catch (err: any) {
        if (err instanceof ChecksumError) throw err;
        throw new ChecksumError(`Failed to verify checksum for ${file.path}: ${err.message}`);
      }
    }
  }

  protected async loadAndParse(manifest: DatasetManifest): Promise<any> {
    const manifestDir = path.dirname(this.manifestPath);
    const parsedData: { [key: string]: any } = {};

    for (const file of manifest.files) {
      const filePath = path.join(manifestDir, file.path);
      const content = await fs.readFile(filePath, 'utf-8');
      const filename = path.basename(file.path, '.json');
      parsedData[filename] = JSON.parse(content);
    }

    return parsedData;
  }

  protected validate(data: any, manifest: DatasetManifest): ValidationReport {
    const errors: string[] = [];
    const warnings: string[] = [];

    const baseData = data['quran'];
    const translitData = data['quran_transliteration'];

    // ==========================================
    // Level 1: Structure Validation
    // ==========================================
    if (!baseData || !Array.isArray(baseData)) {
      errors.push("Level 1 (Structure Error): Missing or invalid base 'quran.json' content.");
      return { hasErrors: true, errors, warnings };
    }
    if (baseData.length !== 114) {
      errors.push(`Level 1 (Structure Error): Expected 114 Surahs in base data, found ${baseData.length}.`);
    }
    if (!translitData || !Array.isArray(translitData)) {
      errors.push("Level 1 (Structure Error): Missing or invalid 'quran_transliteration.json' content.");
    } else if (translitData.length !== 114) {
      errors.push(`Level 1 (Structure Error): Expected 114 Surahs in transliteration data, found ${translitData.length}.`);
    }

    let totalBaseVerses = 0;
    baseData.forEach(surah => {
      if (surah.verses && Array.isArray(surah.verses)) {
        totalBaseVerses += surah.verses.length;
      }
    });

    if (totalBaseVerses !== 6236) {
      errors.push(`Level 1 (Structure Error): Expected 6236 total verses in base data, found ${totalBaseVerses}.`);
    }

    // Verify other translations listed in manifest exist and have valid structure
    for (const file of manifest.files) {
      const filename = path.basename(file.path, '.json');
      if (filename === 'quran' || filename === 'quran_transliteration') {
        continue;
      }
      const transData = data[filename];
      if (!transData || !Array.isArray(transData)) {
        errors.push(`Level 1 (Structure Error): Translation file ${file.path} is empty or invalid.`);
        continue;
      }
      if (transData.length !== 114) {
        errors.push(`Level 1 (Structure Error): Translation file ${file.path} has ${transData.length} Surahs, expected 114.`);
      }

      let totalTransVerses = 0;
      transData.forEach((surah: any) => {
        if (surah.verses && Array.isArray(surah.verses)) {
          totalTransVerses += surah.verses.length;
        }
      });
      if (totalTransVerses !== 6236) {
        errors.push(`Level 1 (Structure Error): Translation file ${file.path} has ${totalTransVerses} verses, expected 6236.`);
      }
    }

    // ==========================================
    // Level 2: Semantic Validation
    // ==========================================
    baseData.forEach((surah: any, index: number) => {
      const surahNum = surah.id;
      const expectedVerses = surah.total_verses;
      const actualVerses = surah.verses?.length || 0;

      if (expectedVerses !== actualVerses) {
        errors.push(`Level 2 (Semantic Error): Surah ${surahNum} total_verses metadata lists ${expectedVerses}, but contains ${actualVerses} verses.`);
      }

      // Check corresponding transliteration surah alignment
      const translitSurah = translitData ? translitData[index] : null;
      if (translitSurah) {
        if (translitSurah.id !== surahNum) {
          errors.push(`Level 2 (Semantic Error): Surah alignment mismatch at index ${index}. Base ID: ${surahNum}, Translit ID: ${translitSurah.id}`);
        }
        const translitVersesCount = translitSurah.verses?.length || 0;
        if (translitVersesCount !== actualVerses) {
          errors.push(`Level 2 (Semantic Error): Surah ${surahNum} has ${actualVerses} verses, but transliteration has ${translitVersesCount} verses.`);
        }
      }

      // Check individual verses for empty Arabic text
      surah.verses?.forEach((verse: any) => {
        if (!verse.text || verse.text.trim() === '') {
          errors.push(`Level 2 (Semantic Error): Surah ${surahNum} Ayah ${verse.id} has empty Arabic text.`);
        }
      });
    });

    // ==========================================
    // Level 3: Business Validation
    // ==========================================
    // Verify Bismillah presence and exact match at Surah 1 Ayah 1
    const firstSurah = baseData[0];
    if (firstSurah) {
      const firstVerse = firstSurah.verses?.[0];
      if (!firstVerse) {
        errors.push("Level 3 (Business Error): Surah 1 Ayah 1 is missing.");
      } else {
        const bismillah = "بِسۡمِ ٱللَّهِ ٱلرَّحۡمَٰنِ ٱلرَّحِيمِ";
        if (firstVerse.text.replace(/\s+/g, '') !== bismillah.replace(/\s+/g, '')) {
          errors.push(`Level 3 (Business Error): Surah 1 Ayah 1 text mismatch. Expected: "${bismillah}", Found: "${firstVerse.text}"`);
        }
      }
    }

    // Verify correct counts of key surahs (Fatihah = 7, Baqarah = 286, Nas = 6)
    const keySurahCounts: { [key: number]: number } = { 1: 7, 2: 286, 114: 6 };
    baseData.forEach((surah: any) => {
      const expected = keySurahCounts[surah.id];
      if (expected !== undefined && surah.total_verses !== expected) {
        errors.push(`Level 3 (Business Error): Surah ${surah.id} expected ${expected} verses, but found ${surah.total_verses}.`);
      }
    });

    // Confirm schemaVersion match
    if (manifest.schemaVersion !== "1.0") {
      errors.push(`Level 3 (Business Error): Unsupported schema version: ${manifest.schemaVersion}. Expected 1.0.`);
    }

    return {
      hasErrors: errors.length > 0,
      errors,
      warnings
    };
  }

  protected normalize(data: any): NormalizedQuranData {
    const surahs: NormalizedSurah[] = [];
    const verses: NormalizedVerse[] = [];
    const translations: NormalizedTranslation[] = [];

    const baseData = data['quran'];
    const translitData = data['quran_transliteration'];

    let globalIndex = 1;

    for (let i = 0; i < baseData.length; i++) {
      const rawSurah = baseData[i];
      const rawTranslitSurah = translitData[i];

      surahs.push({
        number: rawSurah.id,
        arabicName: rawSurah.name,
        transliteration: rawSurah.transliteration,
        englishName: rawTranslitSurah.translation || rawSurah.transliteration,
        revelationType: rawSurah.type,
        totalVerses: rawSurah.total_verses
      });

      for (let j = 0; j < rawSurah.verses.length; j++) {
        const rawVerse = rawSurah.verses[j];
        const rawTranslitVerse = rawTranslitSurah.verses[j];

        verses.push({
          globalIndex,
          surahNumber: rawSurah.id,
          ayahNumber: rawVerse.id,
          arabicText: rawVerse.text,
          arabicCleaned: this.cleanArabic(rawVerse.text),
          transliteration: rawTranslitVerse?.transliteration || ''
        });
        globalIndex++;
      }
    }

    // Process other translations
    const filenames = Object.keys(data);
    for (const filename of filenames) {
      if (filename === 'quran' || filename === 'quran_transliteration') {
        continue;
      }
      
      const lang = filename.split('_')[1];
      const translator = this.getTranslatorName(lang);

      const rawTranslationData = data[filename];
      for (const rawSurah of rawTranslationData) {
        const surahNum = rawSurah.id;
        for (const rawVerse of rawSurah.verses) {
          const ayahNum = rawVerse.id;
          translations.push({
            surahNumber: surahNum,
            ayahNumber: ayahNum,
            language: lang,
            translator,
            text: rawVerse.translation
          });
        }
      }
    }

    return { surahs, verses, translations };
  }

  private cleanArabic(text: string): string {
    return text
      .replace(/[\u064B-\u065F\u0670]/g, "")
      .replace(/[\u06D6-\u06ED]/g, "")
      .trim();
  }

  private getTranslatorName(lang: string): string {
    const translators: { [key: string]: string } = {
      bn: "Bengali Translator",
      en: "Sahih International",
      es: "Muhammad Isa García",
      fr: "Muhammad Hamidullah",
      id: "Kementerian Agama",
      ru: "Elmir Kuliev",
      sv: "Knut Bernström",
      tr: "Diyanet İşleri",
      ur: "Abul A'la Maududi",
      zh: "Ma Jian"
    };
    return translators[lang] || 'Unknown';
  }

  protected async saveIntermediateProcessedData(data: NormalizedQuranData): Promise<void> {
    const processedDir = path.resolve(process.cwd(), 'processed');
    await fs.mkdir(processedDir, { recursive: true });
    const processedFilePath = path.join(processedDir, 'quran_normalized.json');
    await fs.writeFile(processedFilePath, JSON.stringify(data, null, 2), 'utf-8');
  }

  protected async writeValidationReport(report: ValidationReport): Promise<void> {
    const reportsDir = path.resolve(process.cwd(), 'reports', this.dateFolder);
    await fs.mkdir(reportsDir, { recursive: true });

    // Write JSON validation report
    await fs.writeFile(
      path.join(reportsDir, 'validation.json'),
      JSON.stringify(report, null, 2),
      'utf-8'
    );

    // Write Markdown validation report
    let md = `# Quran Dataset Validation Report - ${this.dateFolder}\n\n`;
    md += `**Status**: ${report.hasErrors ? '🔴 FAILED' : '🟢 PASSED'}\n`;
    md += `**Errors Count**: ${report.errors.length}\n`;
    md += `**Warnings Count**: ${report.warnings.length}\n\n`;

    if (report.errors.length > 0) {
      md += `## Errors\n`;
      report.errors.forEach(err => md += `- ${err}\n`);
      md += `\n`;
    }

    if (report.warnings.length > 0) {
      md += `## Warnings\n`;
      report.warnings.forEach(warn => md += `- ${warn}\n`);
      md += `\n`;
    }

    await fs.writeFile(path.join(reportsDir, 'validation.md'), md, 'utf-8');
  }

  protected async writeExecutionReports(metrics: ImportMetrics, manifest: DatasetManifest): Promise<void> {
    const reportsDir = path.resolve(process.cwd(), 'reports', this.dateFolder);
    await fs.mkdir(reportsDir, { recursive: true });

    // 1. Write metrics.json
    await fs.writeFile(
      path.join(reportsDir, 'metrics.json'),
      JSON.stringify(metrics, null, 2),
      'utf-8'
    );

    // 2. Write execution.json
    const executionJson = {
      sessionLogId: this.sessionLogId,
      dataset: manifest.dataset,
      status: metrics.errors.length > 0 ? 'FAILED' : 'SUCCESS',
      metrics
    };
    await fs.writeFile(
      path.join(reportsDir, 'execution.json'),
      JSON.stringify(executionJson, null, 2),
      'utf-8'
    );

    // 3. Write execution.md
    let md = `# Quran ETL Execution Report - ${this.dateFolder}\n\n`;
    md += `## Dataset Info\n`;
    md += `- **Name**: ${manifest.dataset.name}\n`;
    md += `- **Edition**: ${manifest.dataset.edition}\n`;
    md += `- **Version**: ${manifest.dataset.version}\n`;
    md += `- **Source**: ${manifest.dataset.sourceUrl}\n`;
    md += `- **License**: ${manifest.dataset.license}\n\n`;

    md += `## Execution Details\n`;
    md += `- **Session Log ID**: \`${this.sessionLogId}\`\n`;
    md += `- **Status**: ${metrics.errors.length > 0 ? '🔴 FAILED' : '🟢 SUCCESS'}\n`;
    md += `- **Duration**: ${metrics.executionTimeMs} ms\n`;
    md += `- **Memory Used**: ${metrics.memoryUsedMb.toFixed(2)} MB\n`;
    md += `- **Rows Imported**: ${metrics.rowsImported}\n`;
    md += `- **Rows Updated**: ${metrics.rowsUpdated}\n`;
    md += `- **Rows Skipped**: ${metrics.rowsSkipped}\n`;
    md += `- **Rows Deleted**: ${metrics.rowsDeleted}\n\n`;

    if (metrics.errors.length > 0) {
      md += `## Errors\n`;
      metrics.errors.forEach(err => md += `- ${err}\n`);
      md += `\n`;
    }

    if (metrics.warnings.length > 0) {
      md += `## Warnings\n`;
      metrics.warnings.forEach(warn => md += `- ${warn}\n`);
      md += `\n`;
    }

    await fs.writeFile(path.join(reportsDir, 'execution.md'), md, 'utf-8');
  }
}
