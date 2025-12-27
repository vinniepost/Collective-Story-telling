import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-map-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button class="map-marker" 
            (click)="onAction()"
            [disabled]="disabled()"
            [title]="action()"
            [attr.aria-label]="action()">
      <span class="icon">{{ icon() }}</span>
      <span class="vote-count">{{ currentVotes() }}/{{ requiredVotes() }}</span>
    </button>
  `,
  styles: `
    :host {
      position: absolute;
      transform: translate(-50%, -50%);
      z-index: 10;
    }

    .map-marker {
      /* Reset button styles */
      appearance: none;
      border: none;
      outline: none;
      
      width: 50px;
      height: 50px;
      border-radius: 50%;
      border: 2px solid #00FF00;
      background-color: rgba(0, 0, 0, 0.8);
      color: #fff;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
      padding: 0;
    }

    .map-marker:hover:not(:disabled) {
      background-color: #00FF00;
      color: #000;
      transform: scale(1.1);
      box-shadow: 0 0 15px #00FF00;
      z-index: 11;
    }

    .map-marker:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      border-color: #555;
      background-color: rgba(50, 50, 50, 0.8);
    }

    .icon {
      font-size: 20px;
      line-height: 1;
      margin-bottom: 2px;
    }

    .vote-count {
      font-size: 10px;
      font-weight: bold;
    }
  `
})
export class MapButton {
  action = input.required<string>();
  icon = input.required<string>();
  currentVotes = input(0);
  totalClients = input(0);
  disabled = input(false);
  
  actionTriggered = output<string>();

  requiredVotes = computed(() => {
    // "half of the connected clients - 1(the vr player doesn't need to be counted)"
    const audienceCount = Math.max(0, this.totalClients() - 1);
    if (audienceCount === 0) return 1; // Fallback if testing alone
    return Math.ceil(audienceCount / 2);
  });

  onAction() {
    if (!this.disabled()) {
      this.actionTriggered.emit(this.action());
    }
  }
}
