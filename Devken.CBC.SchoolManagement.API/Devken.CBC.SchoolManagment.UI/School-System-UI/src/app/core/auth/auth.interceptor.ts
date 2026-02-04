import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // List of endpoints that should not include auth token or trigger refresh
    const AUTH_ENDPOINTS = [
        '/api/auth/login',
        '/api/auth/refresh',
        '/api/auth/super-admin/login',
        '/api/auth/register',
        '/api/auth/forgot-password',
        '/api/auth/reset-password'
    ];

    const isAuthEndpoint = AUTH_ENDPOINTS.some(url => req.url.includes(url));

    // Add Authorization header to non-auth requests
    if (!isAuthEndpoint && authService.accessToken) {
        req = req.clone({
            setHeaders: {
                Authorization: `Bearer ${authService.accessToken}`
            }
        });
    }

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {
            // Only handle 401 errors for non-auth endpoints
            if (error.status === 401 && !isAuthEndpoint) {
                // Attempt to refresh token
                return authService.refreshAccessToken().pipe(
                    switchMap(success => {
                        if (!success) {
                            // Refresh failed - sign out and redirect
                            authService.signOut();
                            router.navigate(['/sign-in']);
                            return throwError(() => error);
                        }

                        // Refresh succeeded - retry original request with new token
                        const retryReq = req.clone({
                            setHeaders: {
                                Authorization: `Bearer ${authService.accessToken}`
                            }
                        });

                        return next(retryReq);
                    }),
                    catchError(refreshError => {
                        // Refresh itself failed
                        authService.signOut();
                        router.navigate(['/sign-in']);
                        return throwError(() => refreshError);
                    })
                );
            }

            // For all other errors, just pass them through
            return throwError(() => error);
        })
    );
};