import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { EmailLogItem, GlobalStats, ScoreStats } from '../models/stats.model';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/stats`;

  constructor(private readonly http: HttpClient) {}

  getGlobal(): Observable<GlobalStats> {
    return this.http.get<GlobalStats>(`${this.apiUrl}/global`);
  }

  getRecentEmails(): Observable<EmailLogItem[]> {
    return this.http.get<EmailLogItem[]>(`${this.apiUrl}/emails-recents`);
  }

  getTopCandidates(): Observable<ScoreStats[]> {
    return this.http.get<ScoreStats[]>(`${this.apiUrl}/top-candidats`);
  }
}

