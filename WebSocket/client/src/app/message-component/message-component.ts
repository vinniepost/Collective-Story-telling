import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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
      
      <div class="vr-message-box">
        <div class="label">INCOMING TRANSMISSION:</div>
        <div class="message-text">
          @if (ws.vrMessage()) {
            "{{ ws.vrMessage() }}"
          } @else {
            <span class="placeholder">NO SIGNAL...</span>
          }
        </div>
      </div>

      @if (ws.messageOptions().length > 0) {
        <div class="response-section">
          <div class="label">SELECT RESPONSE ({{ requiredVotes() }} VOTES REQUIRED):</div>
          <div class="options-grid">
            @for (option of ws.messageOptions(); track option) {
              <button class="option-btn" 
                      (click)="vote(option)"
                      [class.voted]="hasVotedFor(option)">
                <div class="option-text">{{ option }}</div>
                <div class="progress-bar">
                  <div class="progress-fill" [style.width.%]="getVotePercentage(option)"></div>
                </div>
                <div class="vote-count">{{ getVoteCount(option) }}/{{ requiredVotes() }}</div>
              </button>
            }
          </div>
        </div>
      }
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
    }

    .panel-header {
      color: #00FF00;
      font-weight: bold;
      border-bottom: 1px solid #004d00;
      padding-bottom: 5px;
      margin-bottom: 15px;
      font-size: 14px;
    }

    .vr-message-box {
      background-color: #050505;
      border: 1px solid #004d00;
      padding: 15px;
      margin-bottom: 20px;
    }

    .label {
      font-size: 12px;
      color: #00FF00;
      margin-bottom: 5px;
      font-weight: bold;
    }

    .message-text {
      font-size: 18px;
      color: #fff;
      font-style: italic;
      min-height: 24px;
    }

    .placeholder {
      color: #444;
      font-style: normal;
    }

    .options-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 10px;
    }

    .option-btn {
      background-color: #000;
      border: 1px solid #004d00;
      padding: 10px;
      color: #00FF00;
      cursor: pointer;
      position: relative;
      overflow: hidden;
      text-align: left;
      transition: all 0.2s;
      font-family: 'Courier New', Courier, monospace;
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

    .option-text {
      position: relative;
      z-index: 2;
      margin-bottom: 5px;
      font-size: 14px;
      font-weight: bold;
    }

    .vote-count {
      position: relative;
      z-index: 2;
      font-size: 10px;
      text-align: right;
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
  `
})
export class MessageComponent {
  ws = inject(WebSocketService);
  
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
