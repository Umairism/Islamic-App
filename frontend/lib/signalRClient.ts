import * as signalR from '@microsoft/signalr';
import { PipelineStage, ResearchEventSignalR, ResearchSessionStatus } from './types';

export class ResearchSignalRClient {
  private connection: signalR.HubConnection | null = null;
  private hubUrl: string;

  constructor(hubUrl: string = 'http://localhost:5000/hubs/research') {
    this.hubUrl = hubUrl;
  }

  public async connect(): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    await this.connection.start();
  }

  public async joinSessionGroup(sessionId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinSessionGroup', sessionId);
    }
  }

  public async leaveSessionGroup(sessionId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveSessionGroup', sessionId);
    }
  }

  public onStageCompleted(callback: (stage: PipelineStage, sessionId: string) => void): void {
    this.connection?.on('StageCompleted', callback);
  }

  public onSessionStatusChanged(callback: (sessionId: string, status: ResearchSessionStatus) => void): void {
    this.connection?.on('SessionStatusChanged', callback);
  }

  public onResearchEvent(callback: (event: ResearchEventSignalR) => void): void {
    this.connection?.on('ReceiveResearchEvent', callback);
  }

  public async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRClient = new ResearchSignalRClient();
