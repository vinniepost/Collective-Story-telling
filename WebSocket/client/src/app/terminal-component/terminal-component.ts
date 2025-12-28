import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { WebSocketService } from '../websocket.service';

@Component({
  selector: 'app-terminal-component',
  standalone: true,
  imports: [DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: `./terminal-component.html`,
  styleUrls: [`./terminal-component.css`]
})
export class TerminalComponent {
  ws = inject(WebSocketService);
}
