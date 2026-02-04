import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AuthUtils } from 'app/core/auth/auth.utils';
import { UserService } from 'app/core/user/user.service';
import { catchError, Observable, of, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private _authenticated = false;

    private _httpClient = inject(HttpClient);
    private _userService = inject(UserService);
    private _apiBaseUrl = inject(API_BASE_URL);

    private get authUrl(): string {
        return `${this._apiBaseUrl}/api/auth`;
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    set accessToken(token: string) {
        localStorage.setItem('accessToken', token);
    }

    get accessToken(): string {
        return localStorage.getItem('accessToken') ?? '';
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    forgotPassword(email: string): Observable<any> {
        return this._httpClient.post(
            `${this.authUrl}/forgot-password`,
            email
        );
    }

    resetPassword(password: string): Observable<any> {
        return this._httpClient.post(
            `${this.authUrl}/reset-password`,
            password
        );
    }

    /**
     * Sign in
     * 1) Try normal user login
     * 2) If it fails → try super admin login
     */
    signIn(credentials: { email: string; password: string }): Observable<any> {
        if (this._authenticated) {
            return throwError(() => 'User is already logged in.');
        }

        // 1️⃣ Normal user login
        return this._httpClient.post<any>(
            `${this.authUrl}/login`,
            credentials
        ).pipe(
            switchMap((response) => {
                this.handleAuthSuccess(response);
                return of(response);
            }),

            // 2️⃣ Fallback → super admin login
            catchError(() =>
                this._httpClient.post<any>(
                    `${this.authUrl}/super-admin/login`,
                    credentials
                ).pipe(
                    switchMap((response) => {
                        this.handleAuthSuccess(response, true);
                        return of(response);
                    })
                )
            )
        );
    }

    /**
     * Shared auth success handler
     */
    private handleAuthSuccess(
        response: any,
        isSuperAdmin: boolean = false
    ): void {
        this.accessToken = response.accessToken;
        this._authenticated = true;

        this._userService.user = {
            ...response.user,
            isSuperAdmin,
        };
    }

    /**
     * Sign in using the access token
     */
    signInUsingToken(): Observable<any> {
        return this._httpClient.post(
            `${this.authUrl}/sign-in-with-token`,
            {
                accessToken: this.accessToken,
            }
        ).pipe(
            catchError(() => of(false)),
            switchMap((response: any) => {
                if (response?.accessToken) {
                    this.accessToken = response.accessToken;
                }

                this._authenticated = true;
                this._userService.user = response.user;

                return of(true);
            })
        );
    }

    signOut(): Observable<any> {
        localStorage.removeItem('accessToken');
        this._authenticated = false;
        return of(true);
    }

    signUp(user: {
        name: string;
        email: string;
        password: string;
        company: string;
    }): Observable<any> {
        return this._httpClient.post(
            `${this.authUrl}/sign-up`,
            user
        );
    }

    unlockSession(credentials: {
        email: string;
        password: string;
    }): Observable<any> {
        return this._httpClient.post(
            `${this.authUrl}/unlock-session`,
            credentials
        );
    }

    check(): Observable<boolean> {
        if (this._authenticated) {
            return of(true);
        }

        if (!this.accessToken) {
            return of(false);
        }

        if (AuthUtils.isTokenExpired(this.accessToken)) {
            return of(false);
        }

        return this.signInUsingToken();
    }
}
