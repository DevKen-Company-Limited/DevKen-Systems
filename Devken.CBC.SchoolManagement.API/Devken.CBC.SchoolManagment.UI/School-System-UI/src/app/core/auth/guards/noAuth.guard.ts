import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { map } from 'rxjs/operators';

export const NoAuthGuard: CanActivateFn | CanActivateChildFn = (route, state) => {
    const router = inject(Router);
    const authService = inject(AuthService);

    return authService.authenticated$.pipe(
        map((authenticated) => {
            if (authenticated) {
                // Redirect to home if already authenticated
                return router.parseUrl('');
            }
            return true; // allow access
        })
    );
};
