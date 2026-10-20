import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { filter, map, take } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return authService.requiresCompanySelection() && !state.url.startsWith('/settings')
      ? router.createUrlTree(['/settings']) : true;
  }

  return authService.authInitialized$.pipe(
    filter(initialized => initialized),
    take(1),
    map(() => {
      if (authService.isAuthenticated()) {
        return authService.requiresCompanySelection() && !state.url.startsWith('/settings')
          ? router.createUrlTree(['/settings']) : true;
      }
      return router.createUrlTree(['/auth/login']);
    })
  );
};