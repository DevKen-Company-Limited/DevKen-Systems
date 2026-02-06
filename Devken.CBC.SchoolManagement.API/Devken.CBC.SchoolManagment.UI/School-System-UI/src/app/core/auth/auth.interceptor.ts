import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const AUTH_ENDPOINTS = [
    '/api/auth/login',
    '/api/auth/refresh',
    '/api/auth/super-admin/login',
    '/api/auth/register',
    '/api/auth/forgot-password',
    '/api/auth/reset-password'
  ];

  const isAuthEndpoint = AUTH_ENDPOINTS.some(url => req.url.includes(url));

  if (!isAuthEndpoint && authService.accessToken) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${authService.accessToken}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthEndpoint) {
        return authService.refreshAccessToken().pipe(
          switchMap(success => {
            if (!success) {
              authService.signOut();
              router.navigate(['/sign-in']);
              return throwError(() => error);
            }
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${authService.accessToken}`
              }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            authService.signOut();
            router.navigate(['/sign-in']);
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
