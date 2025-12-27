import { Component, inject, ElementRef, ViewChild, AfterViewChecked, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WebSocketService } from '../websocket.service';

@Component({
  selector: 'app-operator-chat-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat-panel">
      <div class="panel-header">OPERATOR CHAT CHANNEL</div>
      
      <div class="chat-history" #scrollContainer>
        @for (msg of ws.chatMessages(); track $index) {
          <div class="chat-message">
            <span class="timestamp">[{{ msg.timestamp | date:'HH:mm:ss' }}]</span>
            <span class="username" [class.me]="msg.username === ws.username()">{{ msg.username }}:</span>
            <span class="message-text">{{ msg.text }}</span>
          </div>
        }
      </div>

      <div class="chat-input-area">
        <div class="my-username">{{ ws.username() }}</div>
        <input 
          type="text" 
          [(ngModel)]="newMessage" 
          (keyup.enter)="sendMessage()"
          placeholder="Type command..."
          [disabled]="!ws.isConnected()"
        >
        <button (click)="sendMessage()" [disabled]="!ws.isConnected() || !newMessage.trim()">SEND</button>
      </div>
    </div>
  `,
  styles: [`
    .chat-panel {
      background-color: #000;
      border: 2px solid #00FF00;
      padding: 10px;
      color: #fff;
      font-family: 'Courier New', Courier, monospace;
      height: 100%;
      display: flex;
      flex-direction: column;
      box-sizing: border-box;
    }

    .panel-header {
      color: #00FF00;
      font-weight: bold;
      border-bottom: 1px solid #004d00;
      padding-bottom: 5px;
      margin-bottom: 10px;
      font-size: 14px;
    }

    .chat-history {
      flex: 1;
      overflow-y: auto;
      margin-bottom: 10px;
      padding-right: 5px;
      max-height: 100px;
    }

    /* Scrollbar styling */
    .chat-history::-webkit-scrollbar {
      width: 8px;
    }
    .chat-history::-webkit-scrollbar-track {
      background: #001100;
    }
    .chat-history::-webkit-scrollbar-thumb {
      background: #004d00;
    }
    .chat-history::-webkit-scrollbar-thumb:hover {
      background: #00FF00;
    }

    .chat-message {
      margin-bottom: 4px;
      font-size: 12px;
      line-height: 1.4;
      word-wrap: break-word;
    }

    .timestamp {
      color: #444;
      margin-right: 5px;
    }

    .username {
      color: #00aa00;
      font-weight: bold;
      margin-right: 5px;
    }

    .username.me {
      color: #00FF00; /* Brighter for self */
    }

    .message-text {
      color: #ccc;
    }

    .chat-input-area {
      display: flex;
      gap: 10px;
      align-items: center;
      border-top: 1px solid #004d00;
      padding-top: 10px;
    }

    .my-username {
      color: #00FF00;
      font-size: 12px;
      font-weight: bold;
      white-space: nowrap;
    }

    input {
      flex: 1;
      background-color: #001100;
      border: 1px solid #004d00;
      color: #00FF00;
      padding: 5px;
      font-family: 'Courier New', Courier, monospace;
      outline: none;
    }

    input:focus {
      border-color: #00FF00;
    }

    button {
      background-color: #002200;
      border: 1px solid #00FF00;
      color: #00FF00;
      padding: 5px 10px;
      cursor: pointer;
      font-family: 'Courier New', Courier, monospace;
      font-weight: bold;
    }

    button:hover:not(:disabled) {
      background-color: #004400;
    }

    button:disabled {
      border-color: #004d00;
      color: #004d00;
      cursor: not-allowed;
    }
  `]
})
export class OperatorChatComponent implements AfterViewChecked {
  ws = inject(WebSocketService);
  newMessage = '';
  
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  constructor() {
    // Auto-scroll when messages change
    effect(() => {
      this.ws.chatMessages(); // dependency
      setTimeout(() => this.scrollToBottom(), 50);
    });
  }

  ngAfterViewChecked() {
    // this.scrollToBottom(); // Can be too aggressive
  }

  scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }

  sendMessage() {
    if (this.newMessage.trim()) {
      this.ws.sendChatMessage(this.newMessage);
      this.newMessage = '';
    }
  }
}
