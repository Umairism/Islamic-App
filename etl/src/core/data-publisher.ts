import { DatasetManifest, ImportMetrics } from './interfaces';
import { DataSink } from './data-sink';

export class DataPublisher<TNormalized> {
  private sinks: DataSink<TNormalized>[] = [];

  public registerSink(sink: DataSink<TNormalized>): void {
    this.sinks.push(sink);
  }

  public getSinks(): DataSink<TNormalized>[] {
    return this.sinks;
  }

  public async publish(
    data: TNormalized,
    manifest: DatasetManifest,
    sessionLogId: string,
    startTime: number
  ): Promise<ImportMetrics> {
    let rowsImported = 0;
    let rowsUpdated = 0;
    let rowsSkipped = 0;
    let rowsDeleted = 0;
    const warnings: string[] = [];
    const errors: string[] = [];

    for (const sink of this.sinks) {
      try {
        console.log(`[DataPublisher] Publishing to sink: ${sink.name}`);
        const result = await sink.publish(data, manifest, sessionLogId);
        rowsImported += result.rowsImported;
        rowsUpdated += result.rowsUpdated;
        rowsSkipped += result.rowsSkipped;
        rowsDeleted += result.rowsDeleted;
        warnings.push(...result.warnings.map(w => `[${sink.name}] ${w}`));
        errors.push(...result.errors.map(e => `[${sink.name}] ${e}`));
      } catch (err: any) {
        console.error(`[DataPublisher] Error publishing to sink ${sink.name}:`, err);
        errors.push(`[${sink.name}] ${err.message || String(err)}`);
      }
    }

    const executionTimeMs = Date.now() - startTime;
    const memoryUsedMb = process.memoryUsage().heapUsed / 1024 / 1024;

    return {
      executionTimeMs,
      memoryUsedMb,
      rowsImported,
      rowsUpdated,
      rowsSkipped,
      rowsDeleted,
      warnings,
      errors
    };
  }
}
