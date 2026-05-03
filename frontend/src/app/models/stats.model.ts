export interface DomainStats {
  domaine: string;
  nombreCandidatures: number;
  acceptees: number;
  refusees: number;
}

export interface ScoreStats {
  candidatureId: string;
  nomCandidat: string;
  titreOffre: string;
  score: number;
  statut: string;
}

export interface GlobalStats {
  totalCandidatures: number;
  candidaturesAcceptees: number;
  candidaturesRefusees: number;
  candidaturesEnAttente: number;
  offresActives: number;
  offresExpirees: number;
  offresDesactivees: number;
  statsParDomaine: DomainStats[];
  topCandidats: ScoreStats[];
  faiblesScores: ScoreStats[];
}

export interface EmailLogItem {
  id: string;
  candidatureId: string;
  nomCandidat: string;
  destinataire: string;
  typeDecision: string;
  sujet: string;
  reussi: boolean;
  erreur?: string;
  dateEnvoi: string;
}

