import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const allowedRoles = (route.data?.['roles'] as string[] | undefined) ?? [];

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (allowedRoles.length === 0 || authService.hasAnyRole(allowedRoles)) {
    return true;
  }

  return router.createUrlTree([authService.getRedirectPathForRole()]);
};

