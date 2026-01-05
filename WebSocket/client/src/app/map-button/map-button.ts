import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-map-button',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button class="map-marker" 
            (click)="onAction()"
            [disabled]="disabled()"
            [title]="action() + ' (' + remainingVotes() + ' votes needed)'"
            [attr.aria-label]="action()"
            [style.--btn-color]="color()">
      <span class="icon">{{ icon() }}</span>
      <span class="vote-count" *ngIf="remainingVotes() > 0">{{ remainingVotes() }} left</span>
      <span class="vote-count" *ngIf="remainingVotes() === 0">Done</span>
      <span *ngIf="badge()" class="badge">{{ badge() }}</span>
    </button>
  `,
  styles: `
    :host {
      position: absolute;
      transform: translate(-50%, -50%);
      z-index: 10;
      
      /* Scalable sizing relative to map container */
      width: 4.5%; 
      aspect-ratio: 1;
      container-type: inline-size;
    }

    .map-marker {
      /* Reset button styles */
      appearance: none;
      border: none;
      outline: none;
      position: relative;
      
      width: 100%;
      height: 100%;
      border-radius: 50%;
      border: 2px solid var(--btn-color);
      background-color: rgba(0, 0, 0, 0.4);
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
      background-color: var(--btn-color);
      color: #000;
      transform: scale(1.1);
      box-shadow: 0 0 10px var(--btn-color);
      z-index: 11;
    }

    .map-marker:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      border-color: #555;
      background-color: rgba(50, 50, 50, 0.8);
    }

    .icon {
      /* Scale icon with button width */
      font-size: 50cqi;
      line-height: 1;
      margin-bottom: 2px;
    }

    .vote-count {
      /* Scale text with button width */
      font-size: 25cqi;
      font-weight: bold;
      white-space: nowrap;
    }

    .badge {
      position: absolute;
      top: -10cqi;
      right: -10cqi;
      font-size: 40cqi;
      text-shadow: 0 0 4px #000;
    }
  `
})
export class MapButton {
  action = input.required<string>();
  icon = input.required<string>();
  currentVotes = input(0);
  totalClients = input(0);
  disabled = input(false);
  badge = input<string | null>(null);

  actionTriggered = output<string>();

  requiredVotes = computed(() => {
    // "half of the connected clients - 1(the vr player doesn't need to be counted)"
    const audienceCount = Math.max(0, this.totalClients() - 1);
    if (audienceCount === 0) return 1; // Fallback if testing alone
    return Math.ceil(audienceCount / 2);
  });

  remainingVotes = computed(() => {
    return Math.max(0, this.requiredVotes() - this.currentVotes());
  });

  color = input('#00FF00');

  onAction() {
    if (!this.disabled()) {
      this.actionTriggered.emit(this.action());
    }
  }
}
