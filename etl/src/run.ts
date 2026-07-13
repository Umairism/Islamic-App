import { QuranImporter } from './plugins/quran/quran-importer';
import { HadithImporter } from './plugins/hadith/hadith-importer';
import { PrismaSink } from './prisma-sink';
import { PrismaHadithSink } from './prisma-hadith-sink';

async function main() {
  console.log("[Runner] Initializing Unified Ingestion ETL Pipeline...");

  try {
    // 1. Ingest Quran
    console.log("\n--- [Runner] Stage 1: Ingesting Quran Dataset ---");
    const quranImporter = new QuranImporter();
    const quranSink = new PrismaSink();
    quranImporter.registerSink(quranSink);
    await quranImporter.runPipeline();

    // 2. Ingest Sahih al-Bukhari
    console.log("\n--- [Runner] Stage 2: Ingesting Sahih al-Bukhari ---");
    const bukhariImporter = new HadithImporter('sahih-al-bukhari');
    const bukhariSink = new PrismaHadithSink();
    bukhariImporter.registerSink(bukhariSink);
    await bukhariImporter.runPipeline();

    // 3. Ingest Sahih Muslim
    console.log("\n--- [Runner] Stage 3: Ingesting Sahih Muslim ---");
    const muslimImporter = new HadithImporter('sahih-muslim');
    const muslimSink = new PrismaHadithSink();
    muslimImporter.registerSink(muslimSink);
    await muslimImporter.runPipeline();

    console.log("\n[Runner] Unified ingestion pipeline execution finished successfully!");
    process.exit(0);
  } catch (err) {
    console.error("\n[Runner] Critical Pipeline Error:", err);
    process.exit(1);
  }
}

main();
