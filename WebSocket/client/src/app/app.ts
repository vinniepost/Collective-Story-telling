import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WebSocketService } from './websocket.service';
import { MapComponent } from './map-component/map-component';
import { TerminalComponent } from './terminal-component/terminal-component';
import { MessageComponent } from './message-component/message-component';
import { OperatorChatComponent } from './operator-chat-component/operator-chat-component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MapComponent, TerminalComponent, MessageComponent, OperatorChatComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class App {
  ws = inject(WebSocketService);
  
  hasVoted = signal(false);

  requiredVotes = computed(() => {
    const audienceCount = Math.max(0, this.ws.totalClients() - 1);
    if (audienceCount === 0) return 1;
    return Math.ceil(audienceCount / 2);
  });

  voteKeys = computed(() => {
    return Object.keys(this.ws.votes());
  });

  constructor() {
    // Reset vote when action happens
    effect(() => {
      const action = this.ws.lastAction();
      if (action) {
        this.hasVoted.set(false);
      }
    }, { allowSignalWrites: true });
  }

  vote(action: string) {
    if (this.hasVoted() || !this.ws.isConnected()) return;
    
    this.ws.sendMessage({ type: 'vote', option: action });
    this.hasVoted.set(true);
  }

  getVoteColor(count: number): string {
    const required = this.requiredVotes();
    if (count >= required && this.ws.totalClients() > 0) return '#ff0000';
    if (count > 0 && count >= required * 0.75) return '#ffaa00';
    return '#00ff00';
  }
}
