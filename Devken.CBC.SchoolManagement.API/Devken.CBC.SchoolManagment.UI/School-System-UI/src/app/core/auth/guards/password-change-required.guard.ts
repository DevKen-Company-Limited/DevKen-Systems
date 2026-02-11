import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { map } from 'rxjs/operators';
import { AuthService } from '../auth.service';

/**
 * Guard that redirects users who require password change to the change-password page
 */
export const passwordChangeRequiredGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // Check if user is authenticated
    if (!authService.check()) {
        router.navigate(['/sign-in']);
        return false;
    }

    // Check if user requires password change
    if (authService.requiresPasswordChange) {
        router.navigate(['/change-password']);
        return false;
    }

    return true;
};

/**
 * Guard that only allows access to change-password page if user requires it
 */
export const changePasswordGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // Only allow access if user is authenticated
    if (!authService.check()) {
        router.navigate(['/sign-in']);
        return false;
    }

    // If user doesn't require password change, redirect to home
    if (!authService.requiresPasswordChange) {
        router.navigate(['/']);
        return false;
    }

    return true;
};