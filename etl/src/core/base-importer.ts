import * as fs from 'fs/promises';
import * as path from 'path';
import * as crypto from 'crypto';
import { DatasetManifest, ValidationReport, ImportMetrics } from './interfaces';
import { DataPublisher } from './data-publisher';
import { Logger } from './logger';
import { ChecksumError, ValidationError, HealthCheckError } from './errors';
import { EventBus } from './events';

/**
 * Generic Base Class for ETL Ingestion Pipelines
 * 
 * Defines the standard lifecycle workflow hooks and decouples data loading/transformation
 * from data persistence. Extenders specify their own Raw and Normalized Domain Models.
 */
export abstract class BaseImporter<TRaw, TNormalized> {
  protected abstract manifestPath: string;
  protected abstract dateFolder: string; // YYYY-MM-DD
  protected sessionLogId: string;
  
  protected logger!: Logger;
  protected publisher = new DataPublisher<TNormalized>();

  public registerSink(sink: any): void {
    this.publisher.registerSink(sink);
  }

  constructor() {
    this.sessionLogId = `session-${Date.now()}-${Math.random().toString(36).substring(2, 8)}`;
  }

  public async runPipeline(): Promise<void> {
    const startTime = Date.now();
    this.dateFolder = new Date().toISOString().split('T')[0];
    this.logger = new Logger(this.dateFolder);
    
    await this.logger.info("==========================================");
    await this.logger.info("Starting ETL Pipeline Workflow Execution");
    await this.logger.info(`Session Log ID: ${this.sessionLogId}`);
    await this.logger.info("==========================================");
    
    try {
      // 1. Dataset Discovery & Manifest Load
      await this.logger.info("Step 1: Discovering datasets & loading manifest...");
      const manifest = await this.loadManifest();
      await this.logger.info(`Manifest successfully loaded for: ${manifest.dataset.name} (v${manifest.dataset.version})`);
      
      // 2. Checksum Verification
      await this.logger.info("Step 2: Calculating and verifying source checksums...");
      await this.verifyChecksums(manifest);
      
      // 3. Load & Parse Raw Content
      await this.logger.info("Step 3: Loading raw data streams and parsing files...");
      const rawData = await this.loadAndParse(manifest);
      
      // 4. 3-Level Validation (Structure, Semantic, Business)
      await this.logger.info("Step 4: Executing 3-level data validation rules...");
      const validationReport = this.validate(rawData, manifest);
      await this.writeValidationReport(validationReport);
      
      if (validationReport.hasErrors) {
        throw new ValidationError(
          `Validation failed for ${manifest.dataset.name}. See etl/reports/${this.dateFolder}/validation.* for details.`
        );
      }
      await this.logger.info("Validation checks passed successfully.");

      // 5. Transform / Normalize
      await this.logger.info("Step 5: Transforming raw data into Domain Models...");
      const normalizedData = this.normalize(rawData);
      await this.saveIntermediateProcessedData(normalizedData);

      // 6. Ingest via Data Sinks
      await this.logger.info("Step 6: Publishing normalized domain entities to registered sinks...");
      const metrics = await this.publisher.publish(normalizedData, manifest, this.sessionLogId, startTime);

      if (metrics.errors.length > 0) {
        throw new Error(`Data publishing failed with ${metrics.errors.length} errors.`);
      }
      await this.logger.info(`Sinks ingestion completed. Rows imported: ${metrics.rowsImported}, updated: ${metrics.rowsUpdated}`);

      // 7. Health Checks (Database, Dataset, Relationships, Indices)
      await this.logger.info("Step 7: Executing post-ingestion database & schema health checks...");
      await this.runHealthChecks(metrics);
      
      // 8. Event Dispatching
      await this.logger.info("Step 8: Emitting pipeline domain events...");
      this.dispatchDomainEvents(manifest);

      // 9. Write Execution Reports (JSON + Markdown) and complete sinks
      await this.logger.info("Step 9: Compiling reports and finalizing session states...");
      await this.writeExecutionReports(metrics, manifest);
      await this.completeSinks('SUCCESS', metrics);
      
      await this.logger.info("ETL pipeline run finished successfully.");
      
    } catch (err: any) {
      await this.logger.error("ETL Pipeline crashed during execution", err);
      // Rollback session status inside sinks
      const failureMetrics: ImportMetrics = {
        executionTimeMs: Date.now() - startTime,
        memoryUsedMb: process.memoryUsage().heapUsed / 1024 / 1024,
        rowsImported: 0,
        rowsUpdated: 0,
        rowsSkipped: 0,
        rowsDeleted: 0,
        warnings: [],
        errors: [err.message || String(err)]
      };
      await this.completeSinks('FAILED', failureMetrics);
      throw err;
    }
  }

  protected abstract loadManifest(): Promise<DatasetManifest>;
  protected abstract verifyChecksums(manifest: DatasetManifest): Promise<void>;
  protected abstract loadAndParse(manifest: DatasetManifest): Promise<TRaw>;
  protected abstract validate(data: TRaw, manifest: DatasetManifest): ValidationReport;
  protected abstract normalize(data: TRaw): TNormalized;
  protected abstract saveIntermediateProcessedData(data: TNormalized): Promise<void>;
  protected abstract writeValidationReport(report: ValidationReport): Promise<void>;
  protected abstract writeExecutionReports(metrics: ImportMetrics, manifest: DatasetManifest): Promise<void>;

  protected async runHealthChecks(metrics: ImportMetrics): Promise<void> {
    for (const sink of this.publisher.getSinks()) {
      if (sink.runHealthChecks) {
        await this.logger.info(`Executing health verification on sink: ${sink.name}`);
        await sink.runHealthChecks(this.sessionLogId);
      }
    }
  }

  protected dispatchDomainEvents(manifest: DatasetManifest): void {
    EventBus.dispatch({
      name: `${manifest.dataset.name.replace(/[-\s]/g, '')}Imported`,
      timestamp: new Date(),
      payload: {
        edition: manifest.dataset.edition,
        version: manifest.dataset.version,
        sessionLogId: this.sessionLogId
      }
    });
  }

  protected async completeSinks(status: 'SUCCESS' | 'FAILED', metrics: ImportMetrics): Promise<void> {
    for (const sink of this.publisher.getSinks()) {
      if (sink.complete) {
        await sink.complete(this.sessionLogId, status, metrics);
      }
    }
  }

  protected async calculateChecksum(filePath: string): Promise<string> {
    const fileBuffer = await fs.readFile(filePath);
    const hashSum = crypto.createHash('sha256');
    hashSum.update(fileBuffer);
    return hashSum.digest('hex');
  }
}
