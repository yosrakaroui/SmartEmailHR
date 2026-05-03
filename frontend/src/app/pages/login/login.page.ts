import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss'
})
export class LoginPageComponent implements OnInit {
  loading = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    motDePasse: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl(this.authService.getRedirectPathForRole());
    }
  }

  onSubmit(): void {
    this.errorMessage = '';
    if (this.form.invalid || this.loading) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const { email, motDePasse } = this.form.getRawValue();

    this.authService.login(email, motDePasse).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigateByUrl(this.authService.getRedirectPathForRole());
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Identifiants incorrects. Vérifiez votre email et mot de passe.';
      }
    });
  }
}

