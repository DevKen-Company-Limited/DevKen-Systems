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
    '/api/auth/reset-password',
    '/api/auth/change-password'
  ];

  const isAuthEndpoint = AUTH_ENDPOINTS.some(url => req.url.includes(url));

  // Add authorization header to non-auth endpoints
  if (!isAuthEndpoint && authService.accessToken) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${authService.accessToken}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized
      if (error.status === 401 && !isAuthEndpoint) {
        return authService.refreshAccessToken().pipe(
          switchMap(success => {
            if (!success) {
              authService.signOut();
              router.navigate(['/sign-in']);
              return throwError(() => error);
            }
            
            // Retry the request with new token
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

      // Handle 403 Forbidden - Password change required
      if (error.status === 403) {
        const errorMessage = error.error?.message?.toLowerCase() || '';
        if (errorMessage.includes('password change required') || 
            errorMessage.includes('must change password')) {
          router.navigate(['/change-password']);
          return throwError(() => error);
        }
      }

      return throwError(() => error);
    })
  );
};