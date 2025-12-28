import { Component, inject, ElementRef, ViewChild, AfterViewChecked, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WebSocketService } from '../websocket.service';

@Component({
  selector: 'app-operator-chat-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './operator-chat-component.html',
  styleUrls: ['./operator-chat-component.css']
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
