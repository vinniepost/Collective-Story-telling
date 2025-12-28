import { ChangeDetectionStrategy, Component, computed, inject, signal, effect, OnDestroy } from '@angular/core';
import { WebSocketService } from '../websocket.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-message-component',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="message-panel">
      <div class="panel-header">COMMUNICATION LINK</div>
      
      <div class="content-grid">
        <!-- Left Column: Incoming Message -->
        <div class="left-column">
          <div class="vr-message-box">
            <div class="message-text">
              @if (ws.vrMessage()) {
                <span>{{ displayedMessage() }}<span class="cursor">█</span></span>
              } @else {
                <span class="placeholder">NO SIGNAL...</span>
              }
            </div>
          </div>
        </div>

        <!-- Right Column: Voting Options -->
        <div class="right-column">
          @if (ws.messageOptions().length > 0) {
            <div class="response-section">
              <div class="label">SELECT RESPONSE:</div>
              <div class="options-list">
                @for (option of ws.messageOptions(); track option) {
                  <button class="option-btn" 
                          (click)="vote(option)"
                          [class.voted]="hasVotedFor(option)">
                    <div class="option-header">
                      <span class="option-text">{{ option }}</span>
                      <span class="vote-count">{{ getVoteCount(option) }}/{{ requiredVotes() }}</span>
                    </div>
                    <div class="progress-bar">
                      <div class="progress-fill" [style.width.%]="getVotePercentage(option)"></div>
                    </div>
                  </button>
                }
              </div>
            </div>
          } @else {
            <div class="waiting-message">
              <div class="label">STATUS:</div>
              <div class="status-text">WAITING FOR INPUT...</div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: `
    .message-panel {
      background-color: #000;
      border: 2px solid #00FF00;
      padding: 15px;
      color: #fff;
      font-family: 'Courier New', Courier, monospace;
      box-shadow: 0 0 10px rgba(0, 255, 0, 0.2);
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
      margin-bottom: 15px;
      font-size: 14px;
      flex-shrink: 0;
    }

    .content-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
      flex-grow: 1;
      min-height: 0; /* Allow scrolling if needed */
    }

    .left-column, .right-column {
      display: flex;
      flex-direction: column;
      min-height: 0;
    }

    .vr-message-box {
      background-color: #050505;
      border: 1px solid #004d00;
      padding: 15px;
      flex-grow: 1;
      display: flex;
      flex-direction: column;
    }

    .label {
      font-size: 12px;
      color: #00FF00;
      margin-bottom: 10px;
      font-weight: bold;
      text-transform: uppercase;
    }

    .message-text {
      font-size: 22px;
      color: #fff;
      font-style: italic;
      flex-grow: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      text-align: center;
      line-height: 1.4;
      word-break: break-word;
    }

    .cursor {
      display: inline-block;
      width: 12px;
      height: 1.3em;
      font-size: 22px;
      animation: blink 1s step-end infinite;
      margin-left: 2px;
      vertical-align: text-bottom;
    }

    @keyframes blink {
      0%, 100% { opacity: 1; }
      50% { opacity: 0; }
    }

    .placeholder {
      color: #444;
      font-style: normal;
      font-size: 18px;
    }

    .response-section {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .options-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
      overflow-y: auto;
      flex-grow: 1;
      padding-right: 5px; /* Space for scrollbar */
    }

    /* Custom Scrollbar */
    .options-list::-webkit-scrollbar {
      width: 6px;
    }
    .options-list::-webkit-scrollbar-track {
      background: #001100;
    }
    .options-list::-webkit-scrollbar-thumb {
      background: #004d00;
    }
    .options-list::-webkit-scrollbar-thumb:hover {
      background: #00FF00;
    }

    .option-btn {
      background-color: #000;
      border: 1px solid #004d00;
      padding: 2px;
      color: #00FF00;
      cursor: pointer;
      position: relative;
      overflow: hidden;
      text-align: left;
      transition: all 0.2s;
      font-family: 'Courier New', Courier, monospace;
      width: 100%;
      flex-shrink: 0;
    }

    .option-btn:hover {
      border-color: #00FF00;
      background-color: #001100;
      box-shadow: 0 0 5px rgba(0, 255, 0, 0.3);
    }

    .option-btn.voted {
      border-color: #00FF00;
      background-color: #003300;
      color: #fff;
    }

    .option-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
      position: relative;
      z-index: 2;
    }

    .option-text {
      font-size: 16px;
      font-weight: bold;
    }

    .vote-count {
      font-size: 12px;
      color: #00aa00;
    }

    .progress-bar {
      position: absolute;
      bottom: 0;
      left: 0;
      height: 4px;
      width: 100%;
      background-color: #002200;
    }

    .progress-fill {
      height: 100%;
      background-color: #00FF00;
      transition: width 0.3s ease;
    }

    .waiting-message {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      border: 1px dashed #004d00;
      color: #444;
      background-color: #050505;
    }

    .status-text {
      font-size: 16px;
      margin-top: 10px;
    }
    
    /* Responsive adjustments */
    @media (max-width: 768px) {
      .content-grid {
        grid-template-columns: 1fr;
      }
      
      .vr-message-box {
        min-height: 150px;
      }
    }
  `
})
export class MessageComponent implements OnDestroy {
  ws = inject(WebSocketService);
  
  displayedMessage = signal<string>("");
  private typingInterval: any;

  constructor() {
    effect(() => {
      const targetMessage = this.ws.vrMessage();
      
      if (this.typingInterval) {
        clearInterval(this.typingInterval);
      }

      if (!targetMessage) {
        this.displayedMessage.set("");
        return;
      }

      this.displayedMessage.set("");
      let i = 0;
      
      this.typingInterval = setInterval(() => {
        if (i < targetMessage.length) {
          this.displayedMessage.update(m => m + targetMessage.charAt(i));
          i++;
        } else {
          clearInterval(this.typingInterval);
        }
      }, 50);
    });
  }

  ngOnDestroy() {
    if (this.typingInterval) clearInterval(this.typingInterval);
  }
  
  // Local tracking of what user voted for (for UI feedback)
  myVotes = signal<Set<string>>(new Set());

  requiredVotes = computed(() => {
    const audienceCount = Math.max(0, this.ws.totalClients() - 1);
    if (audienceCount === 0) return 1;
    return Math.ceil(audienceCount / 3);
  });

  vote(option: string) {
    this.ws.sendMessage({ type: 'vote_message', option });
    
    // Optimistic UI update (server is source of truth, but this gives instant feedback)
    this.myVotes.update(votes => {
      const newVotes = new Set(votes);
      if (newVotes.has(option)) {
        newVotes.delete(option);
      } else {
        newVotes.add(option);
      }
      return newVotes;
    });
  }

  hasVotedFor(option: string): boolean {
    return this.myVotes().has(option);
  }

  getVoteCount(option: string): number {
    return this.ws.messageVotes()[option] || 0;
  }

  getVotePercentage(option: string): number {
    const count = this.getVoteCount(option);
    const required = this.requiredVotes();
    return Math.min(100, (count / required) * 100);
  }
}
