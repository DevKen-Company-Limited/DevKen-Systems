import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from, of } from 'rxjs';
import { catchError, map, shareReplay, tap } from 'rxjs/operators';
import { AuthUtils }    from 'app/core/auth/auth.utils';
import { UserService }  from 'app/core/user/user.service';
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
    id                  : string;
    name                : string;
    email               : string;
    fullName            : string;
    roles               : string[];
    permissions         : string[];
    isSuperAdmin        : boolean;
    requirePasswordChange?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {

    private _apiBaseUrl  = inject(API_BASE_URL);
    private _userService = inject(UserService);

    private _authenticated$          = new BehaviorSubject<boolean>(false);
    readonly authenticated$          = this._authenticated$.asObservable();

    private _permissions$            = new BehaviorSubject<string[]>([]);
    readonly permissions$            = this._permissions$.asObservable();

    private _requirePasswordChange$  = new BehaviorSubject<boolean>(false);
    readonly requirePasswordChange$  = this._requirePasswordChange$.asObservable();

    // Prevent multiple simultaneous refresh calls
    private _refreshInProgress$: Observable<boolean> | null = null;

    private readonly ACCESS_TOKEN  = 'accessToken';
    private readonly REFRESH_TOKEN = 'refreshToken';
    private readonly USER          = 'authUser';

    // ── Token / User storage ───────────────────────────────────────────────────

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
        const raw = localStorage.getItem(this.USER);
        return raw ? JSON.parse(raw) : null;
    }
    set authUser(user: AuthUser | null) {
        user
            ? localStorage.setItem(this.USER, JSON.stringify(user))
            : localStorage.removeItem(this.USER);
    }

    get requiresPasswordChange(): boolean {
        return this.authUser?.requirePasswordChange ?? false;
    }

    // ── Email / password login ─────────────────────────────────────────────────

    signIn(credentials: { email: string; password: string }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/login', credentials).pipe(
            tap(res => this._handleLoginResponse(res.data, false))
        );
    }

    superAdminSignIn(credentials: { email: string; password: string }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/super-admin/login', credentials).pipe(
            tap(res => this._handleLoginResponse(res.data, true))
        );
    }

    // ── Google SSO ─────────────────────────────────────────────────────────────

    /**
     * Called by sign-in.component after SsoService.exchangeGoogleToken() succeeds.
     *
     * The backend (SsoController) returns the same LoginResponseDto shape used
     * by the normal login endpoint, so we reuse _handleLoginResponse.
     *
     * @param data  The `response.data` object from POST /api/auth/sso/google
     */
    handleSsoLoginResponse(data: any): void {
        // isSuperAdmin is always false for SSO school users.
        // The role check inside _handleLoginResponse will catch it if they somehow
        // have a SuperAdmin role assigned.
        this._handleLoginResponse(data, false);
    }

    /**
     * Convenience method: store tokens + set session from an SSO response.
     *
     * Use this when you already have the full `data` object and want a
     * one-liner in sign-in.component:
     *
     *   this._authService.storeTokens(response.data.accessToken,
     *                                 response.data.refreshToken);
     *   this._authService.handleSsoLoginResponse(response.data);
     *
     * OR just call handleSsoLoginResponse(response.data) alone — it sets
     * both tokens and the session in one step (preferred).
     */
    storeTokens(accessToken: string, refreshToken: string): void {
        this.accessToken  = accessToken;
        this.refreshToken = refreshToken;
    }

    // ── App startup / session restore ──────────────────────────────────────────

    checkAuthOnStartup(): Observable<boolean> {
        if (!this.accessToken) {
            return of(false);
        }

        if (AuthUtils.isTokenExpired(this.accessToken)) {
            return this.refreshAccessToken();
        }

        const user = this.authUser;
        if (user) {
            this.setSession(user);
            return of(true);
        }

        // Edge case: valid token but no stored user — fetch from API
        return this._fetchCurrentUser().pipe(
            tap(user => this.setSession(user)),
            map(() => true),
            catchError(err => {
                console.error('[AuthService] Failed to fetch current user:', err);
                this.signOut();
                return of(false);
            })
        );
    }

    refreshAccessToken(): Observable<boolean> {
        if (this._refreshInProgress$) {
            return this._refreshInProgress$;
        }

        if (!this.refreshToken) {
            this.signOut();
            return of(false);
        }

        this._refreshInProgress$ = this.post<any>('/api/auth/refresh', {
            refreshToken: this.refreshToken,
        }).pipe(
            tap(res => {
                this.accessToken = res.data.accessToken;

                if (res.data.refreshToken) {
                    this.refreshToken = res.data.refreshToken;
                }

                if (res.data.user) {
                    const userData   = res.data.user;
                    const fullName   = this._buildFullName(userData);
                    const isSuperAdm = this._detectSuperAdmin(userData, res.data, this.authUser?.isSuperAdmin);

                    const user: AuthUser = {
                        id                   : userData.id,
                        name                 : fullName || userData.email,
                        email                : userData.email,
                        fullName,
                        roles                : res.data.roles ?? userData.roleNames ?? [],
                        permissions          : res.data.permissions ?? userData.permissions ?? [],
                        isSuperAdmin         : isSuperAdm,
                        requirePasswordChange: userData.requirePasswordChange ?? false,
                    };
                    this.setSession(user);
                } else if (this.authUser) {
                    this._authenticated$.next(true);
                }
            }),
            map(() => true),
            catchError(err => {
                console.error('[AuthService] Token refresh failed:', err);
                this.signOut();
                return of(false);
            }),
            shareReplay(1),
            tap({ finalize: () => { this._refreshInProgress$ = null; } })
        );

        return this._refreshInProgress$;
    }

    signOut(): void {
        localStorage.clear();
        this._authenticated$.next(false);
        this._permissions$.next([]);
        this._requirePasswordChange$.next(false);
        this._userService.user    = null;
        this._refreshInProgress$  = null;
    }

    // ── Permission helpers ─────────────────────────────────────────────────────

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

    // ── Password change ────────────────────────────────────────────────────────

    changePassword(credentials: {
        currentPassword: string;
        newPassword    : string;
    }): Observable<ApiResponse<any>> {
        return this.post<any>('/api/auth/change-password', {
            currentPassword: credentials.currentPassword,
            newPassword    : credentials.newPassword,
        }).pipe(
            tap(() => {
                const currentUser = this.authUser;
                if (currentUser) {
                    currentUser.requirePasswordChange = false;
                    this.authUser = currentUser;
                    this._requirePasswordChange$.next(false);
                }
            })
        );
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /**
     * Single entry point for ALL successful login responses
     * (email/password, super-admin, AND Google SSO).
     *
     * The backend always returns the same LoginResponseDto shape, so
     * this method works for every auth path.
     */
    private _handleLoginResponse(data: any, isSuperAdmin: boolean): void {
        // Persist tokens
        this.accessToken  = data.accessToken;
        this.refreshToken = data.refreshToken;

        const fullName   = this._buildFullName(data.user);
        const isSuperAdm = this._detectSuperAdmin(data.user, data, isSuperAdmin);

        const user: AuthUser = {
            id                   : data.user.id,
            name                 : fullName || data.user.email,
            email                : data.user.email,
            fullName,
            roles                : data.roles ?? data.user.roleNames ?? [],
            permissions          : data.permissions ?? data.user.permissions ?? [],
            isSuperAdmin         : isSuperAdm,
            requirePasswordChange: data.user.requirePasswordChange ?? false,
        };

        this.setSession(user);
    }

    /**
     * Builds a trimmed full name from firstName / lastName fields.
     * Handles cases where one or both may be absent (e.g. first Google login).
     */
    private _buildFullName(userObj: any): string {
        return `${userObj?.firstName ?? ''} ${userObj?.lastName ?? ''}`.trim();
    }

    /**
     * Detects SuperAdmin from all the places the backend might signal it:
     *  - roleNames array on the user object
     *  - top-level roles array
     *  - the isSuperAdmin flag in the login response
     *  - the boolean passed in by the caller (e.g. superAdminSignIn path)
     */
    private _detectSuperAdmin(
        userObj    : any,
        responseData: any,
        fallback   : boolean | undefined,
    ): boolean {
        return (
            userObj?.roleNames?.includes('SuperAdmin')    ||
            responseData?.roles?.includes('SuperAdmin')   ||
            responseData?.user?.isSuperAdmin              ||
            fallback                                       ||
            false
        );
    }

    private fetchCurrentUser = () => this._fetchCurrentUser(); // public alias kept for compatibility

    private _fetchCurrentUser(): Observable<AuthUser> {
        return this.get<any>('/api/auth/me').pipe(
            map(res => {
                const data     = res.data;
                const fullName = this._buildFullName(data);
                const isSuperAdm = this._detectSuperAdmin(data, data, false);

                return {
                    id                   : data.id,
                    name                 : fullName || data.email,
                    email                : data.email,
                    fullName,
                    roles                : data.roleNames ?? data.roles ?? [],
                    permissions          : data.permissions ?? [],
                    isSuperAdmin         : isSuperAdm,
                    requirePasswordChange: data.requirePasswordChange ?? false,
                };
            })
        );
    }

    private setSession(user: AuthUser): void {
        this.authUser = user;
        this._permissions$.next(user.permissions);
        this._authenticated$.next(true);
        this._requirePasswordChange$.next(user.requirePasswordChange ?? false);
        this._userService.user = user;
    }

    // ── HTTP helpers ───────────────────────────────────────────────────────────

    private post<T>(url: string, body: any): Observable<ApiResponse<T>> {
        return this.request<T>(url, 'POST', body);
    }

    private get<T>(url: string): Observable<ApiResponse<T>> {
        return this.request<T>(url, 'GET');
    }

    private request<T>(
        url   : string,
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        body? : any,
    ): Observable<ApiResponse<T>> {
        const config: RequestInit = {
            method,
            headers: {
                'Content-Type': 'application/json',
                ...(this.accessToken && { Authorization: `Bearer ${this.accessToken}` }),
            },
        };

        if (body && method !== 'GET') {
            config.body = JSON.stringify(body);
        }

        return from(
            fetch(`${this._apiBaseUrl}${url}`, config).then(async res => {
                const json = await res.json();
                if (!res.ok) { throw json; }
                return json as ApiResponse<T>;
            })
        );
    }
}