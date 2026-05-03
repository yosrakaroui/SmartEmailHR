import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateOffreRequest, OffreDetail, OffreListItem, UpdateOffreRequest } from '../models/offre.model';

@Injectable({ providedIn: 'root' })
export class OffreService {
  private readonly apiUrl = `${environment.apiBaseUrl}/offres`;

  constructor(private readonly http: HttpClient) {}

  getAll(filters?: { domaine?: string; statut?: string }): Observable<OffreListItem[]> {
    let params = new HttpParams();
    if (filters?.domaine) {
      params = params.set('domaine', filters.domaine);
    }

    if (filters?.statut) {
      params = params.set('statut', filters.statut);
    }

    return this.http.get<OffreListItem[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<OffreDetail> {
    return this.http.get<OffreDetail>(`${this.apiUrl}/${id}`);
  }

  create(payload: CreateOffreRequest): Observable<OffreListItem> {
    return this.http.post<OffreListItem>(this.apiUrl, payload);
  }

  update(id: string, payload: UpdateOffreRequest): Observable<OffreListItem> {
    return this.http.patch<OffreListItem>(`${this.apiUrl}/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

