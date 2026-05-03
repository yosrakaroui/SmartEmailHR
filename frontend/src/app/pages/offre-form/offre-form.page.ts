import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OffreService } from '../../services/offre.service';

@Component({
  selector: 'app-offre-form-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './offre-form.page.html',
  styleUrl: './offre-form.page.scss'
})
export class OffreFormPageComponent implements OnInit {
  loading = false;
  submitting = false;
  errorMessage = '';
  offerId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    titre: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    competences: ['', [Validators.required]],
    niveauExperience: ['Junior', [Validators.required]],
    domaine: ['Développement Web', [Validators.required]],
    dateExpiration: ['', [Validators.required]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly offreService: OffreService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  get isEdit(): boolean {
    return !!this.offerId;
  }

  ngOnInit(): void {
    this.offerId = this.route.snapshot.paramMap.get('id');
    if (!this.offerId) {
      return;
    }

    this.loading = true;
    this.offreService.getById(this.offerId).subscribe({
      next: (offre) => {
        this.loading = false;
        this.form.patchValue({
          titre: offre.titre,
          description: offre.description,
          competences: offre.competencesRequises.join(', '),
          niveauExperience: offre.niveauExperience,
          domaine: offre.domaine,
          dateExpiration: this.toDateInput(offre.dateExpiration)
        });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Impossible de charger l’offre sélectionnée.';
      }
    });
  }

  onSubmit(): void {
    this.errorMessage = '';
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const value = this.form.getRawValue();
    const payload = {
      titre: value.titre,
      description: value.description,
      competencesRequises: value.competences.split(',').map((s) => s.trim()).filter(Boolean),
      niveauExperience: value.niveauExperience,
      domaine: value.domaine,
      dateExpiration: value.dateExpiration
    };

    const request$ = this.offerId
      ? this.offreService.update(this.offerId, payload)
      : this.offreService.create(payload);

    request$.subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigateByUrl('/rh/dashboard');
      },
      error: () => {
        this.submitting = false;
        this.errorMessage = 'Impossible d’enregistrer cette offre.';
      }
    });
  }

  private toDateInput(date: string): string {
    return new Date(date).toISOString().split('T')[0];
  }
}

