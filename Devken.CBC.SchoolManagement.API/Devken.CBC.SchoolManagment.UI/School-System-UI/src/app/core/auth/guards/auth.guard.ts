import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';

export const AuthGuard: CanActivateFn | CanActivateChildFn = (route, state) => {
    const router = inject(Router);
    const authService = inject(AuthService);

    return authService.authenticated$.pipe(
        map((authenticated) => {
            if (!authenticated) {
                // Redirect to sign-in with redirectURL param
                const redirectURL = state.url === '/sign-out' ? '' : `redirectURL=${state.url}`;
                return router.parseUrl(`sign-in?${redirectURL}`);
            }
            return true; // allow access
        })
    );
};
