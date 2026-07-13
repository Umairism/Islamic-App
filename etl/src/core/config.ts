import * as dotenv from 'dotenv';
import * as path from 'path';
import { ConfigError } from './errors';

export class ConfigService {
  constructor() {
    dotenv.config({ path: path.resolve(process.cwd(), '.env') });
  }

  public get(key: string, defaultValue?: string): string {
    const value = process.env[key];
    if (value === undefined) {
      if (defaultValue !== undefined) {
        return defaultValue;
      }
      throw new ConfigError(`Configuration Error: Missing environment variable ${key}`);
    }
    return value;
  }

  public getDatabaseUrl(): string {
    return this.get('DATABASE_URL');
  }
}
