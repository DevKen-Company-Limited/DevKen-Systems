import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, ReplaySubject, tap } from 'rxjs';
import { FuseNavigationItem } from '@fuse/components/navigation';
import { environment } from 'environments/environment';

export interface NavigationResponse {
    default: FuseNavigationItem[];
    compact: FuseNavigationItem[];
    futuristic: FuseNavigationItem[];
    horizontal: FuseNavigationItem[];
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
    private _navigation: ReplaySubject<NavigationResponse> =
        new ReplaySubject<NavigationResponse>(1);

    constructor(private _httpClient: HttpClient) {}

    get navigation$(): Observable<NavigationResponse> {
        return this._navigation.asObservable();
    }

    get(): Observable<NavigationResponse> {
        return this._httpClient
            .get<NavigationResponse>(`${environment.apiUrl}/navigation`)
            .pipe(
                tap((navigation) => {
                    this._navigation.next(navigation);
                })
            );
    }

    getUserPermissions(): Observable<{ permissions: string[] }> {
        return this._httpClient.get<{ permissions: string[] }>(
            `${environment.apiUrl}/navigation/permissions`
        );
    }

    checkPermission(
        permissionKey: string
    ): Observable<{ permissionKey: string; hasPermission: boolean }> {
        return this._httpClient.get<{
            permissionKey: string;
            hasPermission: boolean;
        }>(
            `${environment.apiUrl}/navigation/check-permission/${permissionKey}`
        );
    }
}
