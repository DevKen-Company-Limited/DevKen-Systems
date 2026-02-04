import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of, catchError, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { AuthUtils } from 'app/core/auth/auth.utils';


export interface LoginRequest {
    email: string;
    password: string;
}

export interface SuperAdminLoginRequest {
    email: string;
    password: string;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    data: {
        accessToken: string;
        expiresInSeconds: number;
        refreshToken: string;
        user: {
            id: string;
            tenantId?: string;
            email: string;
            fullName: string;
            roles: string[];
            permissions: string[];
            requirePasswordChange: boolean;
        };
        roles?: string[];
        permissions?: string[];
        message?: string;
    };
}

export interface RefreshTokenRequest {
    token?: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
}
@Injectable({ providedIn: 'root' })
export class ApiAuthService {
    private readonly _http = inject(HttpClient);
    private readonly _apiBaseUrl = inject(API_BASE_URL);

    // ---------------- AUTH ----------------

    signIn(credentials: LoginRequest): Observable<AuthResponse> {
        return this.superAdminLogin(credentials).pipe(
            catchError(() => this.regularLogin(credentials))
        );
    }

    private superAdminLogin(credentials: SuperAdminLoginRequest) {
        return this._http
            .post<AuthResponse>(`${this._apiBaseUrl}/api/auth/super-admin/login`, credentials)
            .pipe(map(r => this.handleAuthSuccess(r)));
    }

    private regularLogin(credentials: LoginRequest) {
        return this._http
            .post<AuthResponse>(`${this._apiBaseUrl}/api/auth/login`, credentials)
            .pipe(map(r => this.handleAuthSuccess(r)));
    }

    signInUsingToken(): Observable<AuthResponse | null> {
        if (!this.accessToken) {
            return of(null);
        }

        return this.refreshToken().pipe(
            catchError(() => of(null))
        );
    }

    refreshToken(token?: string): Observable<AuthResponse> {
        const refreshToken = token ?? this.getRefreshToken();
        if (!refreshToken) {
            throw new Error('No refresh token');
        }

        const endpoint = this.isSuperAdmin()
            ? `${this._apiBaseUrl}/api/auth/super-admin/refresh`
            : `${this._apiBaseUrl}/api/auth/refresh`;

        return this._http
            .post<AuthResponse>(endpoint, { token: refreshToken })
            .pipe(map(r => this.handleAuthSuccess(r)));
    }

    signOut(): Observable<void> {
        return this._http.post<void>(`${this._apiBaseUrl}/api/auth/logout`, {}).pipe(
            map(() => this.clearAuthData()),
            catchError(() => {
                this.clearAuthData();
                return of(void 0);
            })
        );
    }

    // ---------------- HELPERS ----------------

    private handleAuthSuccess(response: AuthResponse): AuthResponse {
        if (response.success) {
            this.storeAuthData(response.data);
        }
        return response;
    }

    private storeAuthData(data: AuthResponse['data']) {
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        localStorage.setItem('user', JSON.stringify(data.user));
        localStorage.setItem('roles', JSON.stringify(data.user.roles));
        localStorage.setItem('permissions', JSON.stringify(data.user.permissions));
    }

    private clearAuthData() {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        localStorage.removeItem('roles');
        localStorage.removeItem('permissions');
    }

    get accessToken(): string | null {
        return localStorage.getItem('accessToken');
    }

    getUser() {
        const user = localStorage.getItem('user');
        return user ? JSON.parse(user) : null;
    }

    isSuperAdmin(): boolean {
        const user = this.getUser();
        return !!user && user.tenantId === null;
    }

    check(): boolean {
        const token = this.accessToken;
        if (!token) return false;
        return !AuthUtils.isTokenExpired(token);
    }

    requiresPasswordChange(): boolean {
        return !!this.getUser()?.requirePasswordChange;
    }

    private getRefreshToken(): string | null {
        return localStorage.getItem('refreshToken');
    }
}
