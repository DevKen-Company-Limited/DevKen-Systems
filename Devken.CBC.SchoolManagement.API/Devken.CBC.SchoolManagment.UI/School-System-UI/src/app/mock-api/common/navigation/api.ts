import { Injectable, inject } from '@angular/core';
import { FuseMockApiService } from '@fuse/lib/mock-api';
import { map, catchError, switchMap } from 'rxjs/operators';
import { of, forkJoin } from 'rxjs';
import { NavigationService } from '@fuse/lib/mock-api/NavigationService';
import { DashboardService } from 'app/core/DevKenService/dashboard/dashboard.service';

@Injectable({ providedIn: 'root' })
export class NavigationMockApi {
    private readonly _fuseMockApiService = inject(FuseMockApiService);
    private readonly _navigationService  = inject(NavigationService);
    private readonly _dashboardService   = inject(DashboardService);

    constructor() {
        this.registerHandlers();
    }

    registerHandlers(): void {
        this._fuseMockApiService
            .onGet('api/common/navigation')
            .reply(() => {
                return forkJoin([
                    this._navigationService.loadNavigation().pipe(catchError(() => of(null))),
                    this._dashboardService.getDashboard({ level: 'All Levels' }).pipe(catchError(() => of(null))),
                ]).pipe(
                    map(([navResponse, dashboardResponse]) => {
                        if (navResponse?.success) {
                            return [200, {
                                ...navResponse.data,
                                dashboard: dashboardResponse,
                            }];
                        }
                        return [500, {
                            compact    : [],
                            default    : [],
                            futuristic : [],
                            horizontal : [],
                            dashboard  : null,
                        }];
                    }),
                    catchError((error) => {
                        console.error('Error fetching navigation or dashboard:', error);
                        return of([500, {
                            compact    : [],
                            default    : [],
                            futuristic : [],
                            horizontal : [],
                            dashboard  : null,
                        }]);
                    })
                );
            });
    }
}