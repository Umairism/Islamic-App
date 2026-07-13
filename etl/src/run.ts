import { QuranImporter } from './plugins/quran/quran-importer';
import { PrismaSink } from './prisma-sink';

async function main() {
  console.log("[Runner] Initializing Quran ETL Pipeline...");
  
  const importer = new QuranImporter();
  const prismaSink = new PrismaSink();
  
  // Inject database sink (Dependency Inversion)
  importer.registerSink(prismaSink);
  
  try {
    await importer.runPipeline();
    console.log("[Runner] Ingestion pipeline execution finished.");
    process.exit(0);
  } catch (err) {
    console.error("[Runner] Critical Pipeline Error:", err);
    process.exit(1);
  }
}

main();
