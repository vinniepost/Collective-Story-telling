import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { MapButton } from '../map-button/map-button';

@Component({
  selector: 'app-map-component',
  imports: [NgOptimizedImage, MapButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="map-wrapper">
      <div class="map-header">
        TACTICAL MAP OVERVIEW
      </div>
      <div class="map-container">
        <!-- 
             Using NgOptimizedImage for performance. 
             Ensure width/height match aspect ratio of your image or use 'fill' mode if container is sized.
             Here assuming a standard aspect ratio, but 'width: 100%; height: auto' in CSS handles responsiveness.
        -->
        <img ngSrc="assets/screenshotMap.png" width="800" height="450" priority alt="Tactical Map" class="map-image">
        
        <!-- Light Button -->
        <app-map-button 
                style="top: 45%; left: 55%;" 
                action="light"
                icon="💡"
                [currentVotes]="votes()['light'] || 0"
                [totalClients]="totalClients()"
                [disabled]="disabled()"
                (actionTriggered)="onAction($event)">
        </app-map-button>

        <!-- Close Door Button -->
        <app-map-button 
                style="top: 60%; left: 30%;" 
                action="close_door"
                icon="🚪"
                [currentVotes]="votes()['close_door'] || 0"
                [totalClients]="totalClients()"
                [disabled]="disabled()"
                (actionTriggered)="onAction($event)">
        </app-map-button>

        <!-- Sound 1 Button -->
        <app-map-button 
                style="top: 20%; left: 80%;" 
                action="sound_1"
                icon="🔊"
                [currentVotes]="votes()['sound_1'] || 0"
                [totalClients]="totalClients()"
                [disabled]="disabled()"
                (actionTriggered)="onAction($event)">
        </app-map-button>
        
        <!-- Sound 2 Button -->
        <app-map-button 
                style="top: 80%; left: 20%;" 
                action="sound_2"
                icon="🔔"
                [currentVotes]="votes()['sound_2'] || 0"
                [totalClients]="totalClients()"
                [disabled]="disabled()"
                (actionTriggered)="onAction($event)">
        </app-map-button>
      </div>
    </div>
  `,
  styles: `
    .map-wrapper {
      background-color: #000;
      border: 2px solid #00FF00;
      padding: 10px;
      box-sizing: border-box;
      box-shadow: 0 0 10px rgba(0, 255, 0, 0.2);
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .map-header {
      color: #00FF00;
      border-bottom: 1px solid #004d00;
      padding-bottom: 5px;
      margin-bottom: 10px;
      font-weight: bold;
      font-size: 14px;
      font-family: 'Courier New', Courier, monospace;
    }

    .map-container {
      position: relative;
      width: 100%;
      /* Removed border from here as it's now on wrapper */
      overflow: hidden;
      background-color: #000;
      flex: 1;
    }

    .map-image {
      width: 100%;
      height: auto;
      display: block;
      opacity: 0.9; 
    }
  `,
})
export class MapComponent {
  disabled = input(false);
  votes = input<{ [key: string]: number }>({});
  totalClients = input(0);
  action = output<string>();

  onAction(act: string) {
    if (!this.disabled()) {
      this.action.emit(act);
    }
  }
}
