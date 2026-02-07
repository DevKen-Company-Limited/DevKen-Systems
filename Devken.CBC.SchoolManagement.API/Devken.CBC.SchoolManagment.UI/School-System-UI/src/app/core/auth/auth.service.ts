import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from, of, throwError } from 'rxjs';
import { catchError, map, shareReplay, switchMap, tap } from 'rxjs/operators';
import { AuthUtils } from 'app/core/auth/auth.utils';
import { UserService } from 'app/core/user/user.service';
import { API_BASE_URL } from 'app/app.config';

/* =======================
   API RESPONSE WRAPPER
======================= */
interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
}

/* =======================
   AUTH USER
======================= */
export interface AuthUser {
    id: string;
    name: string;
    email: string;
    fullName: string;
    roles: string[];
    permissions: string[];
    isSuperAdmin: boolean;
    requirePasswordChange?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
    private _apiBaseUrl = inject(API_BASE_URL);
    private _userService = inject(UserService);

    private _authenticated$ = new BehaviorSubject<boolean>(false);
    readonly authenticated$ = this._authenticated$.asObservable();

    private _permissions$ = new BehaviorSubject<string[]>([]);
    readonly permissions$ = this._permissions$.asObservable();

    private _requirePasswordChange$ = new BehaviorSubject<boolean>(false);
    readonly requirePasswordChange$ = this._requirePasswordChange$.asObservable();

    // Prevent multiple simultaneous refresh calls
    private _refreshInProgress$: Observable<boolean> | null = null;

    private readonly ACCESS_TOKEN = 'accessToken';
    private readonly REFRESH_TOKEN = 'refreshToken';
    private readonly USER = 'authUser';

    /* =======================
       TOKEN STORAGE
    ======================= */
    get accessToken(): string {
        return localStorage.getItem(this.ACCESS_TOKEN) ?? '';
    }

    set accessToken(token: string) {
        token
            ? localStorage.setItem(this.ACCESS_TOKEN, token)
            : localStorage.removeItem(this.ACCESS_TOKEN);
    }

    get refreshToken(): string {
        return localStorage.getItem(this.REFRESH_TOKEN) ?? '';
    }

    set refreshToken(token: string) {
        token
            ? localStorage.setItem(this.REFRESH_TOKEN, token)
            : localStorage.removeItem(this.REFRESH_TOKEN);
    }

    get authUser(): AuthUser | null {
        const user = localStorage.getItem(this.USER);
        return user ? JSON.parse(user) : null;
    }

    set authUser(user: AuthUser | null) {
        user
            ? localStorage.setItem(this.USER, JSON.stringify(user))
            : localStorage.removeItem(this.USER);
    }

    /**
     * Check if the current user requires a password change
     */
    get requiresPasswordChange(): boolean {
        return this.authUser?.requirePasswordChange ?? false;
    }

    /* =======================
       LOGIN
    ======================= */
    signIn(credentials: { email: string; password: string }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/login', credentials).pipe(
            tap(res => this.handleLoginResponse(res.data, false))
        );
    }

    superAdminSignIn(credentials: { email: string; password: string }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/super-admin/login', credentials).pipe(
            tap(res => this.handleLoginResponse(res.data, true))
        );
    }

    /* =======================
       APP START / REFRESH
    ======================= */
    checkAuthOnStartup(): Observable<boolean> {
        // No access token - not authenticated
        if (!this.accessToken) {
            return of(false);
        }

        // Token is expired - try to refresh
        if (AuthUtils.isTokenExpired(this.accessToken)) {
            return this.refreshAccessToken();
        }

        // Token is valid - restore session from stored user
        const user = this.authUser;
        if (user) {
            this.setSession(user);
            return of(true);
        }

        // Edge case: valid token but no stored user - fetch from API
        return this.fetchCurrentUser().pipe(
            tap(user => this.setSession(user)),
            map(() => true),
            catchError(err => {
                console.error('Failed to fetch current user:', err);
                this.signOut();
                return of(false);
            })
        );
    }

    /**
     * Fetch current user from API (for edge cases)
     */
    private fetchCurrentUser(): Observable<AuthUser> {
        return this.get<any>('/api/auth/me').pipe(
            map(res => {
                const data = res.data;
                const fullName = `${data.firstName || ''} ${data.lastName || ''}`.trim();
                return {
                    id: data.id,
                    name: fullName || data.email,
                    email: data.email,
                    fullName,
                    roles: data.roles ?? [],
                    permissions: data.permissions ?? [],
                    isSuperAdmin: data.isSuperAdmin ?? false,
                    requirePasswordChange: data.requirePasswordChange ?? false
                };
            })
        );
    }

    /**
     * Refresh the access token using the refresh token
     * Prevents multiple simultaneous refresh calls using shareReplay
     */
    refreshAccessToken(): Observable<boolean> {
        // If refresh is already in progress, return the existing observable
        if (this._refreshInProgress$) {
            return this._refreshInProgress$;
        }

        // No refresh token available
        if (!this.refreshToken) {
            this.signOut();
            return of(false);
        }

        // Create new refresh observable and cache it
        this._refreshInProgress$ = this.post<any>('/api/auth/refresh', {
            refreshToken: this.refreshToken
        }).pipe(
            tap(res => {
                // Update access token
                this.accessToken = res.data.accessToken;
                
                // Update refresh token if rotated
                if (res.data.refreshToken) {
                    this.refreshToken = res.data.refreshToken;
                }
                
                // Update user session if user data is returned
                if (res.data.user) {
                    const userData = res.data.user;
                    const fullName = `${userData.firstName || ''} ${userData.lastName || ''}`.trim();
                    const user: AuthUser = {
                        id: userData.id,
                        name: fullName || userData.email,
                        email: userData.email,
                        fullName,
                        roles: res.data.roles ?? userData.roles ?? [],
                        permissions: res.data.permissions ?? userData.permissions ?? [],
                        isSuperAdmin: this.authUser?.isSuperAdmin ?? false,
                        requirePasswordChange: userData.requirePasswordChange ?? false
                    };
                    this.setSession(user);
                } else if (this.authUser) {
                    // If no user data returned, just mark as authenticated
                    this._authenticated$.next(true);
                }
            }),
            map(() => true),
            catchError(err => {
                console.error('Token refresh failed:', err);
                this.signOut();
                return of(false);
            }),
            // Share the result and replay for simultaneous subscribers
            shareReplay(1),
            // Clean up after completion
            tap({
                finalize: () => {
                    this._refreshInProgress$ = null;
                }
            })
        );

        return this._refreshInProgress$;
    }

    signOut(): void {
        localStorage.clear();
        this._authenticated$.next(false);
        this._permissions$.next([]);
        this._requirePasswordChange$.next(false);
        this._userService.user = null;
        this._refreshInProgress$ = null;
    }

    hasPermission(permission: string): boolean {
        return this._permissions$.value.includes(permission);
    }

    hasAnyPermission(permissions: string[]): boolean {
        return permissions.some(p => this.hasPermission(p));
    }

    hasAllPermissions(permissions: string[]): boolean {
        return permissions.every(p => this.hasPermission(p));
    }

    check(): boolean {
        return this._authenticated$.value;
    }

    /* =======================
       PASSWORD CHANGE
    ======================= */
    changePassword(credentials: { 
        currentPassword: string; 
        newPassword: string 
    }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/change-password', {
            currentPassword: credentials.currentPassword,
            newPassword: credentials.newPassword
        }).pipe(
            tap(() => {
                // Update the requirePasswordChange flag
                const currentUser = this.authUser;
                if (currentUser) {
                    currentUser.requirePasswordChange = false;
                    this.authUser = currentUser;
                    this._requirePasswordChange$.next(false);
                }
            })
        );
    }

    /* =======================
       INTERNALS
    ======================= */
    private handleLoginResponse(data: any, isSuperAdmin: boolean): void {
        this.accessToken = data.accessToken;
        this.refreshToken = data.refreshToken;

        const fullName = `${data.user.firstName || ''} ${data.user.lastName || ''}`.trim();

        const user: AuthUser = {
            id: data.user.id,
            name: fullName || data.user.email,
            email: data.user.email,
            fullName,
            roles: data.roles ?? [],
            permissions: data.permissions ?? [],
            isSuperAdmin,
            requirePasswordChange: data.user.requirePasswordChange ?? false
        };

        this.setSession(user);
    }

    private setSession(user: AuthUser): void {
        this.authUser = user;
        this._permissions$.next(user.permissions);
        this._authenticated$.next(true);
        this._requirePasswordChange$.next(user.requirePasswordChange ?? false);
        this._userService.user = user;
    }

    /* =======================
       FETCH WRAPPERS
    ======================= */
    private post<T>(url: string, body: any): Observable<ApiResponse<T>> {
        return this.request<T>(url, 'POST', body);
    }

    private get<T>(url: string): Observable<ApiResponse<T>> {
        return this.request<T>(url, 'GET');
    }

    private request<T>(
        url: string, 
        method: 'GET' | 'POST' | 'PUT' | 'DELETE', 
        body?: any
    ): Observable<ApiResponse<T>> {
        const config: RequestInit = {
            method,
            headers: {
                'Content-Type': 'application/json',
                ...(this.accessToken && {
                    Authorization: `Bearer ${this.accessToken}`
                })
            }
        };

        if (body && method !== 'GET') {
            config.body = JSON.stringify(body);
        }

        return from(
            fetch(`${this._apiBaseUrl}${url}`, config).then(async res => {
                const json = await res.json();
                if (!res.ok) {
                    throw json;
                }
                return json as ApiResponse<T>;
            })
        );
    }
}