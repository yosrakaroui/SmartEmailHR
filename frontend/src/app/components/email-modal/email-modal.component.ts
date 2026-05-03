import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-email-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './email-modal.component.html',
  styleUrl: './email-modal.component.scss'
})
export class EmailModalComponent {
  @Input() visible = false;
  @Input() title = 'Prévisualisation email';
  @Input() sujet = '';
  @Input() corps = '';
  @Input() loading = false;

  @Output() close = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<{ sujet: string; corps: string }>();

  onConfirm(): void {
    this.confirm.emit({ sujet: this.sujet, corps: this.corps });
  }
}

