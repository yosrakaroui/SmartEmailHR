import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UserSummary } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'smartemailhr.token';
  private readonly userKey = 'smartemailhr.user';
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  private readonly userSubject = new BehaviorSubject<UserSummary | null>(this.readUserFromStorage());
  readonly user$ = this.userSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  login(email: string, motDePasse: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, { email, motDePasse })
      .pipe(tap((response) => this.setSession(response)));
  }

  loadCurrentUser(): Observable<UserSummary> {
    return this.http.get<UserSummary>(`${this.apiUrl}/me`).pipe(
      tap((user) => {
        localStorage.setItem(this.userKey, JSON.stringify(user));
        this.userSubject.next(user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.userSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): UserSummary | null {
    return this.userSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    return !!user && roles.includes(user.role);
  }

  getRedirectPathForRole(): string {
    const role = this.getCurrentUser()?.role;
    if (role === 'admin') {
      return '/admin/dashboard';
    }

    return '/rh/dashboard';
  }

  private setSession(response: LoginResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response.utilisateur));
    this.userSubject.next(response.utilisateur);
  }

  private readUserFromStorage(): UserSummary | null {
    const raw = localStorage.getItem(this.userKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as UserSummary;
    } catch {
      return null;
    }
  }
}

