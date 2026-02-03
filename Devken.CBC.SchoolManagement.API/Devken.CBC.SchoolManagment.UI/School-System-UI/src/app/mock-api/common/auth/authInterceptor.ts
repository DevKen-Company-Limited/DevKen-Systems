import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ApiAuthService } from './AuthService';


export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const apiAuthService = inject(ApiAuthService);
    const token = apiAuthService.accessToken;

    // Clone the request and add authorization header if token exists
    if (token && !req.url.includes('/auth/')) {
        req = req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`,
            },
        });
    }

    return next(req);
};