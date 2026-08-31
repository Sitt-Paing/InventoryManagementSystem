import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Global HTTP Interceptor that:
 * 1. Enables withCredentials (transmits HttpOnly access_token / refresh_token cookies)
 * 2. Attaches X-XSRF-TOKEN anti-forgery header for mutating requests
 * 3. Handles 401 Unauthorized by attempting a token refresh or redirecting to /auth/login
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Read Anti-CSRF Token from document cookie
  const xsrfToken = authService.getCookie('XSRF-TOKEN');

  let headers = req.headers;
  const isMutatingMethod = ['POST', 'PUT', 'DELETE', 'PATCH'].includes(req.method.toUpperCase());

  if (xsrfToken && isMutatingMethod && !headers.has('X-XSRF-TOKEN')) {
    headers = headers.set('X-XSRF-TOKEN', xsrfToken);
  }

  // Clone request with credentials & CSRF header
  const authReq = req.clone({
    withCredentials: true,
    headers
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // If not already on login or refresh-token endpoint, try refresh or navigate to login
        const isAuthEndpoint = req.url.includes('/Auth/login') || req.url.includes('/Auth/refresh-token');
        if (!isAuthEndpoint) {
          return authService.refreshToken().pipe(
            switchMap(res => {
              if (res.success) {
                // Retry failed request with new credentials
                return next(authReq);
              }
              authService.logout();
              router.navigate(['/auth/login']);
              return throwError(() => error);
            }),
            catchError(refreshErr => {
              authService.logout();
              router.navigate(['/auth/login']);
              return throwError(() => refreshErr);
            })
          );
        }
      }
      return throwError(() => error);
    })
  );
};

