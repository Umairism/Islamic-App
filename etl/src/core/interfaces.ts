export interface FileConfig {
  path: string;
  expectedChecksum: string;
}

export interface DatasetManifest {
  schemaVersion: string;
  dataset: {
    name: string;
    edition: string;
    version: string;
    license: string;
    sourceUrl: string;
  };
  files: FileConfig[];
  supportedTranslators: Record<string, string>;
}

export interface ValidationReport {
  hasErrors: boolean;
  errors: string[];
  warnings: string[];
}

export interface ImportMetrics {
  executionTimeMs: number;
  memoryUsedMb: number;
  rowsImported: number;
  rowsUpdated: number;
  rowsSkipped: number;
  rowsDeleted: number;
  warnings: string[];
  errors: string[];
}
