import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmailModalComponent } from '../../components/email-modal/email-modal.component';
import { CandidatureDetail } from '../../models/candidature.model';
import { CandidatureService } from '../../services/candidature.service';

@Component({
  selector: 'app-candidat-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, EmailModalComponent],
  templateUrl: './candidat-detail.page.html',
  styleUrl: './candidat-detail.page.scss'
})
export class CandidatDetailPageComponent implements OnInit {
  candidature: CandidatureDetail | null = null;
  loading = true;
  actionLoading = false;
  errorMessage = '';

  emailModalVisible = false;
  emailSujet = '';
  emailCorps = '';
  pendingDecision: 'Accepte' | 'Refuse' = 'Accepte';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly candidatureService: CandidatureService
  ) {}

  ngOnInit(): void {
    this.loadCandidature();
  }

  loadCandidature(): void {
    const candidatureId = this.route.snapshot.paramMap.get('id');
    if (!candidatureId) {
      this.router.navigateByUrl('/rh/dashboard');
      return;
    }

    this.loading = true;
    this.candidatureService.getById(candidatureId).subscribe({
      next: (data) => {
        this.loading = false;
        this.candidature = data;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Impossible de charger cette candidature.';
      }
    });
  }

  prepareDecision(decision: 'Accepte' | 'Refuse'): void {
    if (!this.candidature || this.actionLoading) {
      return;
    }

    this.pendingDecision = decision;
    this.actionLoading = true;
    this.candidatureService.generateEmail(this.candidature.id, decision).subscribe({
      next: (email) => {
        this.actionLoading = false;
        this.emailSujet = email.sujet;
        this.emailCorps = email.corps;
        this.emailModalVisible = true;
      },
      error: () => {
        this.actionLoading = false;
        this.errorMessage = 'Impossible de générer l’email.';
      }
    });
  }

  confirmEmail(payload: { sujet: string; corps: string }): void {
    if (!this.candidature) {
      return;
    }

    this.actionLoading = true;
    this.candidatureService
      .updateDecision(this.candidature.id, {
        decision: this.pendingDecision,
        envoyerEmail: true,
        sujetEmail: payload.sujet,
        corpsEmail: payload.corps
      })
      .subscribe({
        next: () => {
          this.actionLoading = false;
          this.emailModalVisible = false;
          this.loadCandidature();
        },
        error: () => {
          this.actionLoading = false;
          this.errorMessage = 'Erreur lors de la mise à jour de la décision.';
        }
      });
  }
}

