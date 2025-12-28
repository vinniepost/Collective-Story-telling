import { Injectable, signal } from '@angular/core';

export interface TerminalMessage {
  timestamp: Date;
  text: string;
  type: 'info' | 'alert' | 'success' | 'warning';
}

export interface PlayerLocation {
  x: number;
  y: number;
}

export interface GameState {
  type: string;
  votes?: { [key: string]: number };
  totalClients?: number;
  playerInArea?: boolean;
  playerLocation?: PlayerLocation;
  command?: string;
  message?: string | ChatMessage;
  votedAction?: string;
  count?: number;
  vrMessage?: string;
  options?: string[];
  username?: string;
  messages?: ChatMessage[];
}

export interface ChatMessage {
  username: string;
  text: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket: WebSocket | null = null;
  
  // Signals for state
  public isConnected = signal<boolean>(false);
  public votes = signal<{ [key: string]: number }>({});
  public totalClients = signal<number>(0);
  public playerInArea = signal<boolean>(false);
  public playerLocation = signal<PlayerLocation | null>(null);
  public lastAction = signal<string | null>(null);
  public terminalMessages = signal<TerminalMessage[]>([]);
  
  // Message System Signals
  public vrMessage = signal<string>("");
  public messageOptions = signal<string[]>([]);
  public messageVotes = signal<{ [key: string]: number }>({});

  // Operator Chat Signals
  public username = signal<string>("");
  public chatMessages = signal<ChatMessage[]>([]);

  constructor() {
    this.connect();
  }

  public connect(): void {
    if (typeof window === 'undefined') return; // SSR check

    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return;
    }

    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const host = window.location.port === '4200' ? 'localhost:3000' : window.location.host;
    const url = `${protocol}//${host}`;

    this.socket = new WebSocket(url);

    this.socket.onopen = () => {
      console.log('Connected to WebSocket');
      this.isConnected.set(true);
      this.sendMessage({ type: 'request_state' });
    };

    this.socket.onmessage = (event) => {
      try {
        const data: GameState = JSON.parse(event.data);
        this.handleMessage(data);
      } catch (e) {
        console.error('Error parsing message', e);
      }
    };

    this.socket.onclose = () => {
      console.log('Disconnected. Reconnecting...');
      this.isConnected.set(false);
      setTimeout(() => this.connect(), 3000);
    };

    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };
  }

  private handleMessage(data: GameState) {
    switch (data.type) {
      case 'update':
        if (data.votes) this.votes.set(data.votes);
        if (data.totalClients !== undefined) this.totalClients.set(data.totalClients);
        if (data.playerInArea !== undefined) this.playerInArea.set(data.playerInArea);
        break;
      case 'player_location':
        if (data.playerLocation) this.playerLocation.set(data.playerLocation);
        break;
      case 'client_count':
        if (data.count !== undefined) this.totalClients.set(data.count);
        break;
      case 'action':
        if (data.command) {
          this.lastAction.set(data.command);
          this.addLog(`ACTION TRIGGERED: ${data.command.toUpperCase()}`, 'success');
        }
        break;
      case 'notification':
        if (data.message && typeof data.message === 'string') this.addLog(data.message, 'info');
        break;
      case 'feedback':
        if (data.message && typeof data.message === 'string') this.addLog(data.message, 'warning');
        break;
      case 'message_state':
        if (data.vrMessage !== undefined) this.vrMessage.set(data.vrMessage);
        if (data.options !== undefined) this.messageOptions.set(data.options);
        if (data.votes !== undefined) this.messageVotes.set(data.votes);
        break;
      case 'vr_message_sent':
        if (typeof data.message === 'string') {
          this.addLog(`MESSAGE SENT: "${data.message}"`, 'success');
          this.messageOptions.set([]); // Clear options locally immediately
        }
        break;
      case 'assign_username':
        if (data.username) this.username.set(data.username);
        break;
      case 'chat_history':
        if (data.messages) this.chatMessages.set(data.messages);
        break;
      case 'chat_message':
        if (data.message && typeof data.message === 'object') {
          const newMsg = data.message as ChatMessage;
          this.chatMessages.update(msgs => {
            const newMsgs = [...msgs, newMsg];
            return newMsgs.length > 50 ? newMsgs.slice(newMsgs.length - 50) : newMsgs;
          });
        }
        break;
    }
  }

  public sendChatMessage(text: string) {
    this.sendMessage({ type: 'chat_message', text });
  }

  public addLog(text: string, type: 'info' | 'alert' | 'success' | 'warning' = 'info') {
    this.terminalMessages.update(msgs => {
      const newMsg: TerminalMessage = { timestamp: new Date(), text, type };
      // Keep last 50 messages to allow filling larger screens.
      // The UI will handle hiding the overflow (clipping the top).
      const updated = [...msgs, newMsg];
      return updated.length > 50 ? updated.slice(updated.length - 50) : updated;
    });
  }

  public sendMessage(msg: any): void {
    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(msg));
    }
  }
}
