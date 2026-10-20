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

  const authPath = requestUrl.pathname.slice(apiUrl.pathname.replace(/\/$/, '').length).toLowerCase();
  const publicAuthPaths = [
    '/auth/login', '/auth/register', '/auth/csrf-token',
    '/auth/generate-reset-token', '/auth/reset-password', '/auth/logout'
  ];
  if (!authService.hasTabSession() && !publicAuthPaths.includes(authPath)) {
    authService.clearTabSession();
    router.navigate(['/auth/login']);
    // Do not send shared cookies or attempt refresh from a tab that has not logged in.
    return throwError(() => new HttpErrorResponse({
      status: 401, statusText: 'Login is required in this tab.', url: req.url
    }));
  }

  // Keep the originating screen's company when a refresh or another tab changes cookies.
  const requestCompanyId = authService.getCompanyId();
  const isCompanyRequest = authPath.startsWith('/master/') || authPath.startsWith('/process/');
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
      let headers = token ? req.headers.set('X-XSRF-TOKEN', token) : req.headers;
      if (isCompanyRequest && requestCompanyId !== null) {
        headers = headers.set('X-Company-Id', String(requestCompanyId));
      }
      return next(req.clone({ withCredentials: true, headers }));
    })
  );

  return sendRequest().pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // If not already on login or refresh-token endpoint, try refresh or navigate to login
        const isAuthEndpoint = publicAuthPaths.includes(authPath) ||
          authPath === '/auth/refresh-token' || authPath === '/auth/me';
        if (!isAuthEndpoint) {
          if (!authService.hasTabSession()) {
            authService.clearTabSession();
            router.navigate(['/auth/login']);
            return throwError(() => error);
          }
          return authService.refreshToken().pipe(
            switchMap(res => {
              if (res.success && authService.hasTabSession()) {
                // Retry failed request with new credentials
                return sendRequest();
              }
              authService.clearTabSession();
              router.navigate(['/auth/login']);
              return throwError(() => error);
            }),
            catchError(refreshErr => {
              authService.clearTabSession();
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

