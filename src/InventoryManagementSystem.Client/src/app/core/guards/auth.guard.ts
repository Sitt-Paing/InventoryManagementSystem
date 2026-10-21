import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { filter, map, take } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const authorizeRoute = () => {
    if (!authService.isAuthenticated()) return router.createUrlTree(['/auth/login']);
    const path = state.url.split('?')[0].split('#')[0];
    const supplierRoute = path === '/supplier' || path.startsWith('/supplier/');
    if (authService.isSupplierAccount()) {
      return supplierRoute ? true : router.createUrlTree(['/supplier/dashboard']);
    }
    if (supplierRoute) return router.createUrlTree([authService.getLandingRoute()]);
    return authService.requiresCompanySelection() && !state.url.startsWith('/settings')
      ? router.createUrlTree(['/settings']) : true;
  };

  if (authService.isAuthenticated()) {
    return authorizeRoute();
  }

  return authService.authInitialized$.pipe(
    filter(initialized => initialized),
    take(1),
    map(() => {
      return authorizeRoute();
    })
  );
};
