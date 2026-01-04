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

export interface MapSection {
  id: string;
  lightsOn: boolean;
}

export interface MapDoor {
  id: string;
  isClosed: boolean;
  lastClosedTime: number;
}

export interface MapPipe {
  id: string;
  sectionId: string;
  repaired: boolean;
}

export interface GameState {
  type: string;
  sections?: MapSection[];
  doors?: MapDoor[];
  votes?: { [key: string]: number };
  doorCooldown?: number;
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
  duration?: number;
  result?: 'escaped' | 'failed';
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

  // Map Signals
  public sections = signal<MapSection[]>([]);
  public doors = signal<MapDoor[]>([]);
  public pipes = signal<MapPipe[]>([]);
  public mapVotes = signal<{ [key: string]: number }>({});
  public doorCooldown = signal<number>(0);

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
  public codeRedActive = signal<boolean>(false);
  public codeRedTimer = signal<number>(0);
  public codeRedResult = signal<'escaped' | 'failed' | null>(null);

  constructor() {
    this.connect();
  }

  public connect(): void {
    if (typeof window === 'undefined') return;

    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return;
    }

    const isDev = window.location.port === '4200';
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    
    // CHANGE IS HERE:
    // Both Dev and Prod now need the "/ws" path because we added { path: '/ws' } to server.js
    if (isDev) {
      // Development: localhost:3000/ws
      this.socket = new WebSocket(`${protocol}//${window.location.hostname}:3000/ws`);
    } else {
      // Production: 139.59.215.136/ws
      this.socket = new WebSocket(`${protocol}//${window.location.host}/ws`);
    }

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
      case 'map_update':
        if (data.sections) this.sections.set(data.sections);
        if (data.doors) this.doors.set(data.doors);
        if ((data as any).pipes) this.pipes.set((data as any).pipes as MapPipe[]);
        if (data.votes) this.mapVotes.set(data.votes);
        if (data.doorCooldown !== undefined) this.doorCooldown.set(data.doorCooldown);
        if (data.totalClients !== undefined) this.totalClients.set(data.totalClients);
        break;
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
      case 'code_red':
        if (data.duration) {
          this.codeRedActive.set(true);
          this.codeRedResult.set(null); // Reset result
          this.codeRedTimer.set(data.duration);

          // Start countdown
          const interval = setInterval(() => {
            const current = this.codeRedTimer();
            if (current <= 0) {
              clearInterval(interval);
              this.codeRedActive.set(false);
            } else {
              this.codeRedTimer.set(current - 1);
            }
          }, 1000);
        }
        break;
      case 'code_red_result':
        if (data.result) {
          this.codeRedActive.set(false);
          this.codeRedResult.set(data.result as 'escaped' | 'failed');
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
