import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { CandidatDetailPageComponent } from './pages/candidat-detail/candidat-detail.page';
import { DashboardAdminPageComponent } from './pages/dashboard-admin/dashboard-admin.page';
import { DashboardRhPageComponent } from './pages/dashboard-rh/dashboard-rh.page';
import { LoginPageComponent } from './pages/login/login.page';
import { OffreFormPageComponent } from './pages/offre-form/offre-form.page';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent
  },
  {
    path: 'rh/dashboard',
    component: DashboardRhPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['rh', 'admin'] }
  },
  {
    path: 'rh/offres/new',
    component: OffreFormPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['rh', 'admin'] }
  },
  {
    path: 'rh/offres/:id/edit',
    component: OffreFormPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['rh', 'admin'] }
  },
  {
    path: 'rh/candidatures/:id',
    component: CandidatDetailPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['rh', 'admin'] }
  },
  {
    path: 'admin/dashboard',
    component: DashboardAdminPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['admin'] }
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
