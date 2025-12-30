import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { MapButton } from '../map-button/map-button';
import { PlayerLocation, WebSocketService } from '../websocket.service';

@Component({
  selector: 'app-map-component',
  imports: [NgOptimizedImage, MapButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './map-component.html',
  styleUrls: ['./map-component.css'],
})
export class MapComponent {
  ws = inject(WebSocketService);

  disabled = input(false);
  votes = input<{ [key: string]: number }>({});
  sections = input<import('../websocket.service').MapSection[]>([]);
  doors = input<import('../websocket.service').MapDoor[]>([]);
  doorCooldown = input(0);
  totalClients = input(0);
  playerLocation = input<PlayerLocation | null>(null);

  // We handle map votes directly to allow multi-voting (toggling)
  voteForEntity(entityId: string) {
    if (!this.ws.isConnected()) return;
    this.ws.sendMessage({ type: 'vote_map', entityId: entityId });
  }
}
