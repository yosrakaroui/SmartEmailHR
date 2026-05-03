export interface AnalyseIa {
  score: number;
  resumeCompetences: string;
  competencesDetectees: string[];
  classification: string;
  coherencePoste: boolean;
  decisionSuggeree: string;
  dateAnalyse: string;
}

export interface CandidatureListItem {
  id: string;
  offreId: string;
  titreOffre: string;
  domaine: string;
  nomCandidat: string;
  emailCandidat: string;
  dateReception: string;
  statut: string;
  emailReponseEnvoye: boolean;
  cvUrl?: string;
  analyseIA?: AnalyseIa;
}

export interface CandidatureDetail extends CandidatureListItem {
  contenuCv: string;
  objetEmail: string;
}

export interface DecisionCandidatureRequest {
  decision: 'Accepte' | 'Refuse';
  envoyerEmail: boolean;
  sujetEmail?: string;
  corpsEmail?: string;
}

export interface DecisionCandidatureResponse {
  candidatureId: string;
  statut: string;
  emailReponseEnvoye: boolean;
  sujetEmail: string;
  corpsEmail: string;
}

