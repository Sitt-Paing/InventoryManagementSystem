import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, defer, of, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Global HTTP Interceptor that:
 * 1. Enables withCredentials (transmits HttpOnly access_token / refresh_token cookies)
 * 2. Attaches X-XSRF-TOKEN anti-forgery header for mutating requests
 * 3. Handles 401 Unauthorized by attempting a token refresh or redirecting to /auth/login
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Only attach credentials and tokens to our API, never third-party requests.
  const apiUrl = new URL(environment.main_url, document.baseURI);
  const requestUrl = new URL(req.url, document.baseURI);
  if (requestUrl.origin !== apiUrl.origin ||
      !requestUrl.pathname.startsWith(`${apiUrl.pathname.replace(/\/$/, '')}/`)) {
    return next(req);
  }

  const isMutatingMethod = !['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(req.method.toUpperCase());
  const sendRequest = () => defer(() =>
    isMutatingMethod ? authService.getCsrfToken() : of(null)
  ).pipe(
    switchMap(token => {
      if (isMutatingMethod && !token) {
        return throwError(() => new Error('Unable to obtain a CSRF token. Please try again.'));
      }
      // Fetch the token for the current server identity, including after login,
      // token expiry, and refresh. Reading the response also supports different hosts.
      const headers = token ? req.headers.set('X-XSRF-TOKEN', token) : req.headers;
      return next(req.clone({ withCredentials: true, headers }));
    })
  );

  return sendRequest().pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // If not already on login or refresh-token endpoint, try refresh or navigate to login
        const isAuthEndpoint = req.url.includes('/Auth/login') ||
          req.url.includes('/Auth/refresh-token') ||
          req.url.includes('/Auth/me');
        if (!isAuthEndpoint) {
          return authService.refreshToken().pipe(
            switchMap(res => {
              if (res.success) {
                // Retry failed request with new credentials
                return sendRequest();
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

