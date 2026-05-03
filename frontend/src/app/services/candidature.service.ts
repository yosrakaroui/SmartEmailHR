import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CandidatureDetail,
  CandidatureListItem,
  DecisionCandidatureRequest,
  DecisionCandidatureResponse
} from '../models/candidature.model';

@Injectable({ providedIn: 'root' })
export class CandidatureService {
  private readonly apiUrl = `${environment.apiBaseUrl}/candidatures`;
  private readonly emailUrl = `${environment.apiBaseUrl}/emails`;

  constructor(private readonly http: HttpClient) {}

  getAll(filters?: {
    offreId?: string;
    statut?: string;
    domaine?: string;
    recherche?: string;
  }): Observable<CandidatureListItem[]> {
    let params = new HttpParams();

    if (filters?.offreId) {
      params = params.set('offreId', filters.offreId);
    }

    if (filters?.statut) {
      params = params.set('statut', filters.statut);
    }

    if (filters?.domaine) {
      params = params.set('domaine', filters.domaine);
    }

    if (filters?.recherche) {
      params = params.set('recherche', filters.recherche);
    }

    return this.http.get<CandidatureListItem[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<CandidatureDetail> {
    return this.http.get<CandidatureDetail>(`${this.apiUrl}/${id}`);
  }

  updateDecision(id: string, payload: DecisionCandidatureRequest): Observable<DecisionCandidatureResponse> {
    return this.http.patch<DecisionCandidatureResponse>(`${this.apiUrl}/${id}/decision`, payload);
  }

  generateEmail(candidatureId: string, decision: string): Observable<{ sujet: string; corps: string }> {
    return this.http.post<{ sujet: string; corps: string }>(`${this.emailUrl}/generer`, {
      candidatureId,
      decision
    });
  }

  sendEmail(payload: {
    candidatureId: string;
    decision: string;
    sujet?: string;
    corps?: string;
    mettreAJourStatut?: boolean;
  }): Observable<{ success: boolean; error?: string; httpStatusCode?: number }> {
    return this.http.post<{ success: boolean; error?: string; httpStatusCode?: number }>(`${this.emailUrl}/envoyer`, payload);
  }
}

