export interface UserSummary {
  id: string;
  nom: string;
  email: string;
  role: 'rh' | 'admin';
  actif: boolean;
}

export interface LoginResponse {
  token: string;
  expireLe: string;
  utilisateur: UserSummary;
}

