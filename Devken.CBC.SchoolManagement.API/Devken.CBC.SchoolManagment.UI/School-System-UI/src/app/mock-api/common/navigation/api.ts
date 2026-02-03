import { Injectable, inject } from '@angular/core';
import { FuseMockApiService } from '@fuse/lib/mock-api';

import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { NavigationService } from '@fuse/lib/mock-api/NavigationService';

@Injectable({ providedIn: 'root' })
export class NavigationMockApi {
    private readonly _fuseMockApiService = inject(FuseMockApiService);
    private readonly _navigationService = inject(NavigationService);

    constructor() {
        this.registerHandlers();
    }

    registerHandlers(): void {
        // Register the handler for navigation
        this._fuseMockApiService
            .onGet('api/common/navigation')
            .reply(() => {
                // Use the NavigationService to fetch from real API
                return this._navigationService.loadNavigation().pipe(
                    map((response) => {
                        // Return in the format expected by mock API [statusCode, data]
                        if (response.success) {
                            return [200, response.data];
                        }
                        return [500, {
                            compact: [],
                            default: [],
                            futuristic: [],
                            horizontal: [],
                        }];
                    }),
                    catchError((error) => {
                        console.error('Error fetching navigation:', error);
                        return of([
                            500,
                            {
                                compact: [],
                                default: [],
                                futuristic: [],
                                horizontal: [],
                            },
                        ]);
                    })
                );
            });
    }
}