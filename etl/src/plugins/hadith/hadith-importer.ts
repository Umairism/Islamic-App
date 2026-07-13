import * as fs from 'fs/promises';
import * as path from 'path';
import * as zlib from 'zlib';
import { BaseImporter } from '../../core/base-importer';
import { DatasetManifest, ValidationReport, ImportMetrics } from '../../core/interfaces';
import { NormalizedHadithData, NormalizedHadithBook, NormalizedHadithChapter, NormalizedHadith } from '../../core/normalized-models';
import { ValidationError, ChecksumError } from '../../core/errors';

export class HadithImporter extends BaseImporter<any, NormalizedHadithData> {
  protected manifestPath: string;
  protected dateFolder: string;

  constructor(collectionFolder: string) {
    super();
    this.manifestPath = path.resolve(process.cwd(), `raw/${collectionFolder}/manifest.json`);
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
    const file = manifest.files[0];
    const filePath = path.join(manifestDir, file.path);
    
    const buffer = await fs.readFile(filePath);
    const decompressed = zlib.gunzipSync(buffer);
    return JSON.parse(decompressed.toString('utf-8'));
  }

  protected validate(data: any, manifest: DatasetManifest): ValidationReport {
    const errors: string[] = [];
    const warnings: string[] = [];

    if (!data.hadiths || !Array.isArray(data.hadiths)) {
      errors.push("Missing or invalid 'hadiths' array.");
    }
    if (!data.metadata) {
      errors.push("Missing dataset 'metadata' object.");
    }
    if (!data.chapters || !Array.isArray(data.chapters)) {
      errors.push("Missing or invalid 'chapters' array.");
    }

    return {
      hasErrors: errors.length > 0,
      errors,
      warnings
    };
  }

  protected normalize(data: any): NormalizedHadithData {
    const meta = data.metadata;
    const name = meta.english.title.toLowerCase().includes("bukhari") ? "Bukhari" : "Muslim";
    const slug = name === "Bukhari" ? "sahih-al-bukhari" : "sahih-muslim";

    const collection = {
      slug,
      shortName: name,
      displayName: meta.english.title,
      titleArabic: meta.arabic.title,
      titleEnglish: meta.english.title,
      authorArabic: meta.arabic.author,
      authorEnglish: meta.english.author,
      introductionArabic: meta.arabic.introduction || '',
      introductionEnglish: meta.english.introduction || '',
      totalHadiths: meta.length
    };

    // Books and Chapters are parsed dynamically from chapters list
    const books: NormalizedHadithBook[] = [];
    const chapters: NormalizedHadithChapter[] = [];

    // Check if chapters structure matches list
    // Arabic raw lists start book numbers (usually inferred or explicitly tracked)
    // To make it configuration-driven and simple, let's map:
    const bookMap = new Map<number, string>();

    (data.chapters as any[]).forEach((ch) => {
      // For Bukhari & Muslim chapters represent both Books and Chapters.
      // Let's model ch.id as chapter number. To associate them with bookNumber:
      // Typically, chapterId in hadiths corresponds to chapter index.
      // Let's extract bookNumber from chapterId or defaults:
      // For simplicity: book number is chapterId / 100 or mapped. 
      // Let's assume book number is ch.bookId || Math.ceil(ch.id / 5) (or simple grouping).
      // Wait, let's look at chapter element we printed:
      // Bukhari chapter 1 has: { "id": 1, "arabic": "كتاب بدء الوحى", "english": "Revelation" }.
      // This is indeed a "Book" name!
      // In Bukhari/Muslim, data.chapters array lists the Books! 
      // And in each hadith, chapterId corresponds to the Book index!
      // So Book Number is exactly chapterId (id in the chapters list).
      // Let's verify: Yes! The printed chapter 1 is "Revelation" which is Book 1 of Bukhari.
      // Therefore, data.chapters maps to Books, and chapterId in hadiths maps to these Books.
      // What about Chapters? Chapters inside the Book are not explicitly listed in a separate table in this schema;
      // instead, chapters represent the Books, and individual Hadith numbers group narrations.
      // Let's create a single HadithBook for each chapter entry:
      const bookNum = ch.id;
      books.push({
        bookNumber: bookNum,
        titleArabic: ch.arabic || '',
        titleEnglish: ch.english || ''
      });

      // Chapter is a sub-unit, let's create a single chapter 1 for each book to satisfy database constraint
      chapters.push({
        bookNumber: bookNum,
        chapterNumber: 1,
        titleArabic: ch.arabic || '',
        titleEnglish: ch.english || ''
      });
    });

    const hadiths = (data.hadiths as any[]).map((h) => {
      const arabicCleaned = this.cleanArabicText(h.arabic);
      
      return {
        bookNumber: h.chapterId, // maps to book
        chapterNumber: 1, // default chapter relation
        hadithNumber: h.id,
        canonicalNumber: h.id.toString(),
        originalNumber: h.id.toString(),
        arabicText: h.arabic || '',
        arabicCleaned,
        englishNarrator: h.english?.narrator || '',
        englishText: h.english?.text || ''
      };
    });

    return {
      collection,
      books,
      chapters,
      hadiths
    };
  }

  protected async saveIntermediateProcessedData(data: NormalizedHadithData): Promise<void> {
    const reportDir = path.resolve(process.cwd(), `reports/${this.dateFolder}`);
    await fs.mkdir(reportDir, { recursive: true });
    await fs.writeFile(
      path.join(reportDir, `intermediate-${data.collection.slug}.json`),
      JSON.stringify(data.collection, null, 2),
      'utf-8'
    );
  }

  protected async writeValidationReport(report: ValidationReport): Promise<void> {
    const reportDir = path.resolve(process.cwd(), `reports/${this.dateFolder}`);
    await fs.mkdir(reportDir, { recursive: true });
    await fs.writeFile(
      path.join(reportDir, 'validation-hadith.json'),
      JSON.stringify(report, null, 2),
      'utf-8'
    );
  }

  protected async writeExecutionReports(metrics: ImportMetrics, manifest: DatasetManifest): Promise<void> {
    const reportDir = path.resolve(process.cwd(), `reports/${this.dateFolder}`);
    await fs.mkdir(reportDir, { recursive: true });
    await fs.writeFile(
      path.join(reportDir, `execution-${manifest.dataset.name.replace(/\s+/g, '-').toLowerCase()}.json`),
      JSON.stringify(metrics, null, 2),
      'utf-8'
    );
  }

  private cleanArabicText(text: string): string {
    if (!text) return '';
    let cleaned = text.replace(/[\u064B-\u0652\u0670]/g, '');
    cleaned = cleaned.replace(/[\u0622\u0623\u0625]/g, '\u0627');
    cleaned = cleaned.replace(/\u0649/g, '\u064A');
    cleaned = cleaned.replace(/\u0640/g, '');
    cleaned = cleaned.replace(/[\s\p{P}]+/gu, ' ').trim();
    return cleaned;
  }
}
