import { DatasetManifest, ImportMetrics } from './interfaces';

export interface DataSinkResult {
  rowsImported: number;
  rowsUpdated: number;
  rowsSkipped: number;
  rowsDeleted: number;
  warnings: string[];
  errors: string[];
}

/**
 * Generic Interface for Data Sinks
 * Decouples database technologies (Prisma/PostgreSQL, Elasticsearch, etc.) from ETL control flow.
 */
export interface DataSink<TNormalized> {
  name: string;
  publish(data: TNormalized, manifest: DatasetManifest, sessionLogId: string): Promise<DataSinkResult>;
  runHealthChecks?(sessionLogId: string): Promise<void>;
  complete?(sessionLogId: string, status: 'SUCCESS' | 'FAILED', metrics: ImportMetrics): Promise<void>;
}
