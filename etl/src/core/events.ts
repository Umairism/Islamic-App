export interface DomainEvent {
  name: string;
  timestamp: Date;
  payload: any;
}

export class EventBus {
  private static listeners: Record<string, ((event: DomainEvent) => void)[]> = {};

  public static subscribe(eventName: string, listener: (event: DomainEvent) => void): void {
    if (!this.listeners[eventName]) {
      this.listeners[eventName] = [];
    }
    this.listeners[eventName].push(listener);
  }

  public static dispatch(event: DomainEvent): void {
    const handlers = this.listeners[event.name] || [];
    for (const handler of handlers) {
      try {
        handler(event);
      } catch (err) {
        console.error(`[EventBus] Error in event listener for ${event.name}:`, err);
      }
    }
  }
}
