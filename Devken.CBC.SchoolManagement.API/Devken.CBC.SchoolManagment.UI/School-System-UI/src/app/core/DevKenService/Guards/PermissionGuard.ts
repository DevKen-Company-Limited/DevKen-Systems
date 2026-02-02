import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from 'app/core/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class PermissionGuard implements CanActivate {
    constructor(
        private _authService: AuthService,
        private _router: Router
    ) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        const requiredPermissions = route.data['permissions'] as string[];
        const requireAll = route.data['requireAll'] !== false; // Default to true

        if (!requiredPermissions || requiredPermissions.length === 0) {
            return true;
        }

        // Get user permissions from auth service (you'll need to implement this)
        const userPermissions = this._authService.getUserPermissions();

        const hasPermission = requireAll
            ? requiredPermissions.every((permission) => userPermissions.includes(permission))
            : requiredPermissions.some((permission) => userPermissions.includes(permission));

        if (!hasPermission) {
            // Redirect to unauthorized page or show error
            return this._router.parseUrl('/error/403');
        }

        return hasPermission;
    }
}