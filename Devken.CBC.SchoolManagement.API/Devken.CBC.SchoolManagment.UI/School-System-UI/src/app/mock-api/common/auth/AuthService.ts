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

    /**
     * Sign in - Try super admin first, then regular login
     */
    signIn(credentials: LoginRequest): Observable<AuthResponse> {
        // First try super admin login
        return this.superAdminLogin(credentials).pipe(
            catchError((superAdminError) => {
                console.log('Super admin login failed, trying regular login...');
                // If super admin fails, try regular login
                return this.regularLogin(credentials).pipe(
                    catchError((regularError) => {
                        console.error('Both login attempts failed');
                        // Return the regular login error as it's more common
                        throw regularError;
                    })
                );
            })
        );
    }

    /**
     * Super admin login
     */
    private superAdminLogin(credentials: SuperAdminLoginRequest): Observable<AuthResponse> {
        return this._http.post<AuthResponse>(
            `${this._apiBaseUrl}/api/auth/super-admin/login`,
            credentials
        ).pipe(
            map(response => {
                if (response.success) {
                    console.log('Super admin login successful');
                    this.storeAuthData(response.data);
                }
                return response;
            })
        );
    }

    /**
     * Regular login
     */
    private regularLogin(credentials: LoginRequest): Observable<AuthResponse> {
        return this._http.post<AuthResponse>(
            `${this._apiBaseUrl}/api/auth/login`,
            credentials
        ).pipe(
            map(response => {
                if (response.success) {
                    console.log('Regular login successful');
                    this.storeAuthData(response.data);
                }
                return response;
            })
        );
    }

    /**
     * Sign in with access token (auto-login)
     */
    signInUsingToken(): Observable<AuthResponse | null> {
        const accessToken = this.accessToken;
        
        if (!accessToken) {
            return of(null);
        }

        // Try to refresh the token
        return this.refreshToken().pipe(
            catchError(() => {
                // If refresh fails, return null
                return of(null);
            })
        );
    }

    /**
     * Refresh access token
     */
    refreshToken(token?: string): Observable<AuthResponse> {
        const refreshToken = token || this.getRefreshToken();
        
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }

        // Determine if this is a super admin token by checking stored user data
        const isSuperAdmin = this.isSuperAdmin();

        const endpoint = isSuperAdmin 
            ? `${this._apiBaseUrl}/api/auth/super-admin/refresh`
            : `${this._apiBaseUrl}/api/auth/refresh`;

        return this._http.post<AuthResponse>(endpoint, { token: refreshToken }).pipe(
            map(response => {
                if (response.success) {
                    this.storeAuthData(response.data);
                }
                return response;
            })
        );
    }

    /**
     * Sign out
     */
    signOut(): Observable<any> {
        return this._http.post(`${this._apiBaseUrl}/api/auth/logout`, {}).pipe(
            map(response => {
                this.clearAuthData();
                return response;
            }),
            catchError(error => {
                // Clear auth data even if logout fails
                this.clearAuthData();
                throw error;
            })
        );
    }

    /**
     * Change password
     */
    changePassword(request: ChangePasswordRequest): Observable<any> {
        return this._http.post(
            `${this._apiBaseUrl}/api/auth/change-password`,
            request
        );
    }

    /**
     * Get current user info
     */
    getCurrentUser(): Observable<any> {
        return this._http.get(`${this._apiBaseUrl}/api/auth/me`);
    }

    /**
     * Store authentication data in localStorage
     */
    private storeAuthData(data: AuthResponse['data']): void {
        if (data.accessToken) {
            this.accessToken = data.accessToken;
        }
        if (data.refreshToken) {
            localStorage.setItem('refreshToken', data.refreshToken);
        }
        if (data.user) {
            localStorage.setItem('user', JSON.stringify(data.user));
        }
        if (data.roles) {
            localStorage.setItem('roles', JSON.stringify(data.roles));
        }
        if (data.permissions) {
            localStorage.setItem('permissions', JSON.stringify(data.permissions));
        }
    }

    /**
     * Clear authentication data
     */
    private clearAuthData(): void {
        this.accessToken = null;
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        localStorage.removeItem('roles');
        localStorage.removeItem('permissions');
    }

    /**
     * Setter & getter for access token
     */
    set accessToken(token: string) {
        localStorage.setItem('accessToken', token);
    }

    get accessToken(): string {
        return localStorage.getItem('accessToken') ?? '';
    }

    /**
     * Get refresh token
     */
    private getRefreshToken(): string | null {
        return localStorage.getItem('refreshToken');
    }

    /**
     * Get stored user
     */
    getUser(): any {
        const user = localStorage.getItem('user');
        return user ? JSON.parse(user) : null;
    }

    /**
     * Check if user is super admin
     */
    isSuperAdmin(): boolean {
        const user = this.getUser();
        return user && user.tenantId === null;
    }

    /**
     * Check if user is authenticated
     */
    check(): boolean {
        // Check if the access token exists and is valid
        if (this.accessToken && AuthUtils.isTokenExpired(this.accessToken)) {
            return false;
        }

        return !!this.accessToken;
    }

    /**
     * Check if password change is required
     */
    requiresPasswordChange(): boolean {
        const user = this.getUser();
        return user?.requirePasswordChange || false;
    }
}