import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, ReplaySubject, tap } from 'rxjs';
import { environment } from 'environments/environment';

export interface AuthUser {
    id: string;
    email: string;
    name: string;
    schoolId: string;
    permissions: string[];
}

export interface LoginResponse {
    accessToken: string;
    accessTokenExpiresInSeconds: number;
    user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
    private _authenticated: boolean = false;
    private _user: ReplaySubject<AuthUser> = new ReplaySubject<AuthUser>(1);
    private _permissions: string[] = [];

    constructor(private _httpClient: HttpClient) {
        this.loadPermissionsFromToken();
    }

    set user(value: AuthUser) {
        this._user.next(value);
        this._permissions = value.permissions || [];
    }

    get user$(): Observable<AuthUser> {
        return this._user.asObservable();
    }

    signIn(credentials: {
        email: string;
        password: string;
        schoolSlug: string;
    }): Observable<LoginResponse> {
        if (this._authenticated) {
            throw new Error('User is already logged in.');
        }

        return this._httpClient
            .post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials)
            .pipe(
                tap((response) => {
                    this.accessToken = response.accessToken;
                    this._authenticated = true;
                    this.user = response.user;
                })
            );
    }

    signOut(): Observable<any> {
        localStorage.removeItem('accessToken');
        this._authenticated = false;
        this._permissions = [];
        return this._httpClient.post(`${environment.apiUrl}/auth/logout`, {});
    }

    check(): Observable<boolean> {
        if (this._authenticated) {
            return new Observable<boolean>((observer) => {
                observer.next(true);
                observer.complete();
            });
        }

        if (!this.accessToken) {
            return new Observable<boolean>((observer) => {
                observer.next(false);
                observer.complete();
            });
        }

        this._authenticated = true;

        return new Observable<boolean>((observer) => {
            observer.next(true);
            observer.complete();
        });
    }

    getUserPermissions(): string[] {
        return this._permissions;
    }

    hasPermission(permission: string): boolean {
        return this._permissions.includes(permission);
    }

    hasAllPermissions(permissions: string[]): boolean {
        return permissions.every((permission) =>
            this._permissions.includes(permission)
        );
    }

    hasAnyPermission(permissions: string[]): boolean {
        return permissions.some((permission) =>
            this._permissions.includes(permission)
        );
    }

    private loadPermissionsFromToken(): void {
        const token = this.accessToken;
        if (token) {
            try {
                const payload = this.parseJwt(token);
                this._permissions = payload.permissions || [];
            } catch {
                this._permissions = [];
            }
        }
    }

    private parseJwt(token: string): any {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split('')
                .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );
        return JSON.parse(jsonPayload);
    }

    get accessToken(): string {
        return localStorage.getItem('accessToken') ?? '';
    }

    set accessToken(token: string) {
        localStorage.setItem('accessToken', token);
        this.loadPermissionsFromToken();
    }
}
