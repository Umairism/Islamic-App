export class EtlError extends Error {
  constructor(message: string) {
    super(message);
    this.name = this.constructor.name;
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

export class DatasetError extends EtlError {}
export class ChecksumError extends EtlError {}
export class ValidationError extends EtlError {}
export class NormalizationError extends EtlError {}
export class ImportError extends EtlError {}
export class HealthCheckError extends EtlError {}
export class MigrationError extends EtlError {}
export class ConfigError extends EtlError {}
