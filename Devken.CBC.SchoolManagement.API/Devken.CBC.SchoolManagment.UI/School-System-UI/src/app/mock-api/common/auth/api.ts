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
        // Sign in - tries super admin first, then regular login
        this._fuseMockApiService
            .onPost('api/auth/sign-in')
            .reply(({ request }) => {
                return this._apiAuthService.signIn(request.body).pipe(
                    map((response) => {
                        if (response.success) {
                            return [
                                200,
                                {
                                    user: response.data.user,
                                    accessToken: response.data.accessToken,
                                    tokenType: 'bearer',
                                    refreshToken: response.data.refreshToken,
                                },
                            ];
                        }
                        return [401, { error: 'Invalid credentials' }];
                    }),
                    catchError((error) => {
                        console.error('Sign in error:', error);
                        return of([401, { error: 'Invalid credentials' }]);
                    })
                );
            });

        // Sign in using token
        this._fuseMockApiService
            .onPost('api/auth/sign-in-with-token')
            .reply(() => {
                return this._apiAuthService.signInUsingToken().pipe(
                    map((response) => {
                        if (response && response.success) {
                            return [
                                200,
                                {
                                    user: response.data.user,
                                    accessToken: response.data.accessToken,
                                    tokenType: 'bearer',
                                },
                            ];
                        }
                        return [401, { error: 'Invalid token' }];
                    }),
                    catchError((error) => {
                        console.error('Token sign in error:', error);
                        return of([401, { error: 'Invalid token' }]);
                    })
                );
            });

        // Refresh token
        this._fuseMockApiService
            .onPost('api/auth/refresh')
            .reply(({ request }) => {
                return this._apiAuthService.refreshToken(request.body?.token).pipe(
                    map((response) => {
                        if (response.success) {
                            return [
                                200,
                                {
                                    accessToken: response.data.accessToken,
                                    tokenType: 'bearer',
                                },
                            ];
                        }
                        return [401, { error: 'Invalid refresh token' }];
                    }),
                    catchError((error) => {
                        console.error('Refresh token error:', error);
                        return of([401, { error: 'Invalid refresh token' }]);
                    })
                );
            });

        // Sign out
        this._fuseMockApiService
            .onPost('api/auth/sign-out')
            .reply(() => {
                return this._apiAuthService.signOut().pipe(
                    map(() => [200, true]),
                    catchError((error) => {
                        console.error('Sign out error:', error);
                        return of([200, true]); // Return success even on error
                    })
                );
            });

        // Forgot password
        this._fuseMockApiService
            .onPost('api/auth/forgot-password')
            .reply(() => of([200, true]));

        // Reset password
        this._fuseMockApiService
            .onPost('api/auth/reset-password')
            .reply(() => of([200, true]));

        // Sign up
        this._fuseMockApiService
            .onPost('api/auth/sign-up')
            .reply(() => of([200, true]));

        // Unlock session (use sign in)
        this._fuseMockApiService
            .onPost('api/auth/unlock-session')
            .reply(({ request }) => {
                return this._apiAuthService.signIn(request.body).pipe(
                    map((response) => {
                        if (response.success) {
                            return [
                                200,
                                {
                                    user: response.data.user,
                                    accessToken: response.data.accessToken,
                                    tokenType: 'bearer',
                                },
                            ];
                        }
                        return [401, { error: 'Invalid credentials' }];
                    }),
                    catchError((error) => {
                        console.error('Unlock session error:', error);
                        return of([401, { error: 'Invalid credentials' }]);
                    })
                );
            });
    }
}