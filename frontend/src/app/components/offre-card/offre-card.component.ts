import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { OffreListItem } from '../../models/offre.model';

@Component({
  selector: 'app-offre-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './offre-card.component.html',
  styleUrl: './offre-card.component.scss'
})
export class OffreCardComponent {
  @Input({ required: true }) offre!: OffreListItem;

  @Output() open = new EventEmitter<string>();
  @Output() edit = new EventEmitter<string>();
}

