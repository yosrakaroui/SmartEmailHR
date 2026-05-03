import { CandidatureListItem } from './candidature.model';

export interface OffreListItem {
  id: string;
  titre: string;
  description: string;
  competencesRequises: string[];
  niveauExperience: string;
  domaine: string;
  dateExpiration: string;
  statut: string;
  dateCreation: string;
  creePar: string;
  nombreCandidatures: number;
}

export interface OffreDetail extends OffreListItem {
  candidatures: CandidatureListItem[];
}

export interface CreateOffreRequest {
  titre: string;
  description: string;
  competencesRequises: string[];
  niveauExperience: string;
  domaine: string;
  dateExpiration: string;
}

export interface UpdateOffreRequest {
  titre?: string;
  description?: string;
  competencesRequises?: string[];
  niveauExperience?: string;
  domaine?: string;
  dateExpiration?: string;
  statut?: string;
}

