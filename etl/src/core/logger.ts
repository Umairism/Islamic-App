import * as fs from 'fs/promises';
import * as path from 'path';

export class Logger {
  private logFilePath: string | null = null;

  constructor(dateFolder?: string) {
    if (dateFolder) {
      this.logFilePath = path.resolve(process.cwd(), 'reports', dateFolder, 'execution.log');
    }
  }

  public async info(message: string): Promise<void> {
    const formatted = `[INFO] [${new Date().toISOString()}] ${message}`;
    console.log(formatted);
    await this.writeToFile(formatted);
  }

  public async warn(message: string): Promise<void> {
    const formatted = `[WARN] [${new Date().toISOString()}] ${message}`;
    console.warn(formatted);
    await this.writeToFile(formatted);
  }

  public async error(message: string, error?: any): Promise<void> {
    let formatted = `[ERROR] [${new Date().toISOString()}] ${message}`;
    if (error) {
      formatted += ` | Details: ${error.message || String(error)}`;
    }
    console.error(formatted);
    await this.writeToFile(formatted);
  }

  private async writeToFile(message: string): Promise<void> {
    if (!this.logFilePath) return;
    try {
      await fs.mkdir(path.dirname(this.logFilePath), { recursive: true });
      await fs.appendFile(this.logFilePath, message + '\n', 'utf-8');
    } catch (err) {
      console.error('Logger failed to write to file:', err);
    }
  }
}
