import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserSummary } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly apiUrl = `${environment.apiBaseUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(this.apiUrl);
  }

  create(payload: { nom: string; email: string; motDePasse: string; role: string }): Observable<UserSummary> {
    return this.http.post<UserSummary>(this.apiUrl, payload);
  }

  updateStatus(id: string, actif: boolean): Observable<UserSummary> {
    return this.http.patch<UserSummary>(`${this.apiUrl}/${id}/status`, { actif });
  }
}

