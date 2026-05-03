import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CandidatCardComponent } from '../../components/candidat-card/candidat-card.component';
import { OffreCardComponent } from '../../components/offre-card/offre-card.component';
import { StatCardComponent } from '../../components/stat-card/stat-card.component';
import { CandidatureListItem } from '../../models/candidature.model';
import { OffreListItem } from '../../models/offre.model';
import { AuthService } from '../../services/auth.service';
import { CandidatureService } from '../../services/candidature.service';
import { OffreService } from '../../services/offre.service';

@Component({
  selector: 'app-dashboard-rh-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatCardComponent, OffreCardComponent, CandidatCardComponent],
  templateUrl: './dashboard-rh.page.html',
  styleUrl: './dashboard-rh.page.scss'
})
export class DashboardRhPageComponent implements OnInit {
  loading = true;
  errorMessage = '';

  offres: OffreListItem[] = [];
  candidatures: CandidatureListItem[] = [];

  searchTerm = '';
  selectedDomain = 'Tous';
  selectedStatut = 'Tous';
  selectedOffreId = '';

  constructor(
    private readonly offreService: OffreService,
    private readonly candidatureService: CandidatureService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  get domains(): string[] {
    const unique = new Set(this.offres.map((o) => o.domaine));
    return ['Tous', ...Array.from(unique)];
  }

  get filteredOffres(): OffreListItem[] {
    if (this.selectedDomain === 'Tous') {
      return this.offres;
    }

    return this.offres.filter((o) => o.domaine === this.selectedDomain);
  }

  get filteredCandidatures(): CandidatureListItem[] {
    return this.candidatures.filter((c) => {
      const domainMatch = this.selectedDomain === 'Tous' || c.domaine === this.selectedDomain;
      const statusMatch = this.selectedStatut === 'Tous' || c.statut === this.selectedStatut;
      const offerMatch = !this.selectedOffreId || c.offreId === this.selectedOffreId;
      const query = this.searchTerm.trim().toLowerCase();

      const searchMatch =
        !query ||
        c.nomCandidat.toLowerCase().includes(query) ||
        c.emailCandidat.toLowerCase().includes(query) ||
        c.analyseIA?.competencesDetectees.some((skill) => skill.toLowerCase().includes(query)) ||
        false;

      return domainMatch && statusMatch && offerMatch && searchMatch;
    });
  }

  get totalCandidates(): number {
    return this.candidatures.length;
  }

  get acceptedCount(): number {
    return this.candidatures.filter((c) => c.statut === 'Accepte').length;
  }

  get refusedCount(): number {
    return this.candidatures.filter((c) => c.statut === 'Refuse').length;
  }

  get pendingCount(): number {
    return this.candidatures.filter((c) => c.statut === 'EnAttente').length;
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      offres: this.offreService.getAll(),
      candidatures: this.candidatureService.getAll()
    }).subscribe({
      next: ({ offres, candidatures }) => {
        this.offres = offres;
        this.candidatures = candidatures;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Impossible de charger le dashboard RH.';
      }
    });
  }

  clearOfferFilter(): void {
    this.selectedOffreId = '';
  }

  openOffer(offreId: string): void {
    this.selectedOffreId = offreId;
  }

  editOffer(offreId: string): void {
    this.router.navigate(['/rh/offres', offreId, 'edit']);
  }

  openCandidate(candidatureId: string): void {
    this.router.navigate(['/rh/candidatures', candidatureId]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}

