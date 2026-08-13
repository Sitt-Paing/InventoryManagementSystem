import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Global HTTP Interceptor that sets default 'Content-Type: application/json' header
 * for POST, PUT, and PATCH requests when sending stringified JSON bodies.
 */
export const contentTypeInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.headers.has('Content-Type') && ['POST', 'PUT', 'PATCH'].includes(req.method.toUpperCase())) {
    const jsonReq = req.clone({
      setHeaders: {
        'Content-Type': 'application/json'
      }
    });
    return next(jsonReq);
  }
  return next(req);
};
