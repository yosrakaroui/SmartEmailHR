import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CandidatureListItem } from '../../models/candidature.model';

@Component({
  selector: 'app-candidat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './candidat-card.component.html',
  styleUrl: './candidat-card.component.scss'
})
export class CandidatCardComponent {
  @Input({ required: true }) candidature!: CandidatureListItem;
  @Output() open = new EventEmitter<string>();
}

