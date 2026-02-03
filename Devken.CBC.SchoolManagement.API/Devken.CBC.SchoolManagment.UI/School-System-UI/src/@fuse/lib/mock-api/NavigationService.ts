import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, catchError, of } from 'rxjs';
import { FuseNavigationItem } from '@fuse/components/navigation';
import { API_BASE_URL } from 'app/app.config';

export interface NavigationData {
    default: FuseNavigationItem[];
    compact: FuseNavigationItem[];
    futuristic: FuseNavigationItem[];
    horizontal: FuseNavigationItem[];
}

export interface NavigationResponse {
    success: boolean;
    message: string;
    data: NavigationData;
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
    private readonly _http = inject(HttpClient);
    private readonly _apiBaseUrl = inject(API_BASE_URL);
    private _navigation$ = new BehaviorSubject<NavigationData | null>(null);

    /**
     * Getter for navigation observable
     */
    get navigation$(): Observable<NavigationData | null> {
        return this._navigation$.asObservable();
    }

    /**
     * Load navigation from API
     */
    loadNavigation(): Observable<NavigationResponse> {
        return this._http
            .get<NavigationResponse>(
                `${this._apiBaseUrl}/api/navigation`
            )
            .pipe(
                tap((response) => {
                    if (response.success && response.data) {
                        this._navigation$.next(response.data);
                    }
                }),
                catchError((error) => {
                    console.error('Failed to load navigation:', error);
                    // Return empty navigation structure on error
                    const emptyResponse: NavigationResponse = {
                        success: false,
                        message: 'Failed to load navigation',
                        data: {
                            default: [],
                            compact: [],
                            futuristic: [],
                            horizontal: [],
                        },
                    };
                    this._navigation$.next(emptyResponse.data);
                    return of(emptyResponse);
                })
            );
    }

    /**
     * Get current navigation value (synchronous)
     */
    getNavigation(): NavigationData | null {
        return this._navigation$.value;
    }

    /**
     * Get specific navigation layout
     */
    getNavigationByLayout(layout: 'default' | 'compact' | 'futuristic' | 'horizontal'): FuseNavigationItem[] {
        const navigation = this._navigation$.value;
        return navigation ? navigation[layout] : [];
    }

    /**
     * Clear navigation cache
     */
    clearNavigation(): void {
        this._navigation$.next(null);
    }

    /**
     * Reload navigation from API
     */
    reloadNavigation(): Observable<NavigationResponse> {
        this.clearNavigation();
        return this.loadNavigation();
    }
}