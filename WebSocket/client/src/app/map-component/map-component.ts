import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { MapButton } from '../map-button/map-button';
import { PlayerLocation } from '../websocket.service';

@Component({
  selector: 'app-map-component',
  imports: [NgOptimizedImage, MapButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './map-component.html',
  styleUrls: ['./map-component.css'],
})
export class MapComponent {
  disabled = input(false);
  votes = input<{ [key: string]: number }>({});
  totalClients = input(0);
  playerLocation = input<PlayerLocation | null>(null);
  action = output<string>();

  onAction(act: string) {
    if (!this.disabled()) {
      this.action.emit(act);
    }
  }
}
