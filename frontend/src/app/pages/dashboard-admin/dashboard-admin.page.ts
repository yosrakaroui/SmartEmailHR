import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { StatCardComponent } from '../../components/stat-card/stat-card.component';
import { OffreListItem } from '../../models/offre.model';
import { EmailLogItem, GlobalStats } from '../../models/stats.model';
import { UserSummary } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { OffreService } from '../../services/offre.service';
import { StatsService } from '../../services/stats.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-dashboard-admin-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, StatCardComponent],
  templateUrl: './dashboard-admin.page.html',
  styleUrl: './dashboard-admin.page.scss'
})
export class DashboardAdminPageComponent implements OnInit {
  loading = true;
  errorMessage = '';

  stats: GlobalStats | null = null;
  recentEmails: EmailLogItem[] = [];
  users: UserSummary[] = [];
  offres: OffreListItem[] = [];
  userActionLoading = false;

  readonly createUserForm = this.fb.nonNullable.group({
    nom: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    motDePasse: ['', [Validators.required, Validators.minLength(8)]],
    role: ['rh', [Validators.required]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly statsService: StatsService,
    private readonly offreService: OffreService,
    private readonly userService: UserService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      global: this.statsService.getGlobal().pipe(catchError(() => of(null))),
      emails: this.statsService.getRecentEmails().pipe(catchError(() => of([]))),
      users: this.userService.getAll().pipe(catchError(() => of([]))),
      offres: this.offreService.getAll().pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ global, emails, users, offres }) => {
        this.loading = false;
        this.stats = global;
        this.recentEmails = emails;
        this.users = users;
        this.offres = offres;

        if (!global) {
          this.errorMessage = 'Impossible de charger les statistiques admin. Verifiez que le backend et la base MySQL sont demarres.';
        }
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Impossible de charger le dashboard administrateur.';
      }
    });
  }

  get acceptanceRatio(): number {
    if (!this.stats || this.stats.totalCandidatures === 0) {
      return 0;
    }

    return Math.round((this.stats.candidaturesAcceptees / this.stats.totalCandidatures) * 100);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }

  createUser(): void {
    this.errorMessage = '';
    if (this.createUserForm.invalid || this.userActionLoading) {
      this.createUserForm.markAllAsTouched();
      return;
    }

    this.userActionLoading = true;
    this.userService.create(this.createUserForm.getRawValue()).subscribe({
      next: (user) => {
        this.userActionLoading = false;
        this.users = [user, ...this.users];
        this.createUserForm.patchValue({ nom: '', email: '', motDePasse: '', role: 'rh' });
      },
      error: () => {
        this.userActionLoading = false;
        this.errorMessage = 'Impossible de créer le compte RH.';
      }
    });
  }

  toggleUserStatus(user: UserSummary): void {
    if (this.userActionLoading) {
      return;
    }

    this.userActionLoading = true;
    this.userService.updateStatus(user.id, !user.actif).subscribe({
      next: (updated) => {
        this.userActionLoading = false;
        this.users = this.users.map((item) => (item.id === updated.id ? updated : item));
      },
      error: () => {
        this.userActionLoading = false;
        this.errorMessage = 'Impossible de modifier le statut utilisateur.';
      }
    });
  }
}
