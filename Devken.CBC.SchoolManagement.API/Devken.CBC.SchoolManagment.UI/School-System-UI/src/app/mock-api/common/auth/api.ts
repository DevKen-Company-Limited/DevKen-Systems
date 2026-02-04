import { Injectable, inject } from '@angular/core';
import { FuseMockApiService } from '@fuse/lib/mock-api';
import { catchError, map } from 'rxjs/operators';
import { of } from 'rxjs';

import { ApiAuthService } from './AuthService';

@Injectable({ providedIn: 'root' })
export class AuthMockApi {
    private readonly _fuseMockApiService = inject(FuseMockApiService);
    private readonly _apiAuthService = inject(ApiAuthService);

    constructor() {
        this.registerHandlers();
    }

    registerHandlers(): void {

        // ---------------------------------------------------------------------
        // LOGIN (tries super-admin first, then tenant login)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/login')
            .reply(({ request }) =>
                this._apiAuthService.signIn(request.body).pipe(
                    map(response => [200, response]),
                    catchError(err => {
                        console.error('Login error:', err);
                        return of([401, {
                            success: false,
                            message: 'Invalid credentials'
                        }]);
                    })
                )
            );

        // ---------------------------------------------------------------------
        // SUPER ADMIN LOGIN (explicit endpoint, optional)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/super-admin/login')
            .reply(({ request }) =>
                this._apiAuthService.signIn(request.body).pipe(
                    map(response => [200, response]),
                    catchError(err => {
                        console.error('Super admin login error:', err);
                        return of([401, {
                            success: false,
                            message: 'Invalid credentials'
                        }]);
                    })
                )
            );

        // ---------------------------------------------------------------------
        // SIGN IN USING TOKEN (AUTO LOGIN)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/token-login')
            .reply(() =>
                this._apiAuthService.signInUsingToken().pipe(
                    map(response => {
                        if (!response) {
                            return [401, {
                                success: false,
                                message: 'Invalid or expired token'
                            }];
                        }
                        return [200, response];
                    }),
                    catchError(err => {
                        console.error('Token login error:', err);
                        return of([401, {
                            success: false,
                            message: 'Invalid or expired token'
                        }]);
                    })
                )
            );

        // ---------------------------------------------------------------------
        // REFRESH TOKEN
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/refresh')
            .reply(({ request }) =>
                this._apiAuthService.refreshToken(request.body?.token).pipe(
                    map(response => [200, response]),
                    catchError(err => {
                        console.error('Refresh token error:', err);
                        return of([401, {
                            success: false,
                            message: 'Invalid refresh token'
                        }]);
                    })
                )
            );

        // ---------------------------------------------------------------------
        // LOGOUT
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/logout')
            .reply(() =>
                this._apiAuthService.signOut().pipe(
                    map(() => [200, { success: true }]),
                    catchError(err => {
                        console.error('Logout error:', err);
                        return of([200, { success: true }]);
                    })
                )
            );

        // ---------------------------------------------------------------------
        // PASSWORD FLOWS (MOCKED)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/forgot-password')
            .reply(() =>
                of([200, {
                    success: true,
                    message: 'Password reset link sent'
                }])
            );

        this._fuseMockApiService
            .onPost('api/auth/reset-password')
            .reply(() =>
                of([200, {
                    success: true,
                    message: 'Password reset successful'
                }])
            );

        // ---------------------------------------------------------------------
        // SIGN UP (MOCKED)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/register')
            .reply(() =>
                of([200, {
                    success: true,
                    message: 'Registration successful'
                }])
            );

        // ---------------------------------------------------------------------
        // UNLOCK SESSION (RE-LOGIN)
        // ---------------------------------------------------------------------
        this._fuseMockApiService
            .onPost('api/auth/unlock-session')
            .reply(({ request }) =>
                this._apiAuthService.signIn(request.body).pipe(
                    map(response => [200, response]),
                    catchError(err => {
                        console.error('Unlock session error:', err);
                        return of([401, {
                            success: false,
                            message: 'Invalid credentials'
                        }]);
                    })
                )
            );
    }
}
