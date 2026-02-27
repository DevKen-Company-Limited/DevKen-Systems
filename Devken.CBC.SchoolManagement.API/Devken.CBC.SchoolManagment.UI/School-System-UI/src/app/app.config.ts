import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
    ApplicationConfig,
    inject,
    isDevMode,
    provideAppInitializer,
    InjectionToken
} from '@angular/core';
import { LuxonDateAdapter } from '@angular/material-luxon-adapter';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideFuse } from '@fuse';
import { TranslocoService, provideTransloco } from '@jsverse/transloco';
import { importProvidersFrom } from '@angular/core';

import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';

import { appRoutes } from 'app/app.routes';
import { provideAuth } from 'app/core/auth/auth.provider';
import { provideIcons } from 'app/core/icons/icons.provider';

import { firstValueFrom, catchError, of, switchMap } from 'rxjs';
import { TranslocoHttpLoader } from './core/transloco/transloco.http-loader';

import { mockApiInterceptor } from '@fuse/lib/mock-api/mock-api.interceptor';
import { authInterceptor } from './core/auth/auth.interceptor';
import { subscriptionStatusInterceptor } from './core/auth/SubscriptionStatusInterceptor';

import { MockApiService } from './mock-api';
import { NavigationService } from '@fuse/lib/mock-api/NavigationService';
import { AuthService } from './core/auth/auth.service';
import { environment } from '../environments/environment';

// API Base URL Injection Token
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

export const appConfig: ApplicationConfig = {
    providers: [
        provideAnimations(),

        // HttpClient with interceptors
        provideHttpClient(
            withInterceptors([
                mockApiInterceptor,
                authInterceptor,
                subscriptionStatusInterceptor
            ])
        ),

        // Router setup
        provideRouter(
            appRoutes,
            withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })
        ),

        // Material providers
        importProvidersFrom(MatSnackBarModule, MatDialogModule),

        // API Base URL from environment
        { provide: API_BASE_URL, useValue: environment.apiUrl },

        // Date adapter
        { provide: DateAdapter, useClass: LuxonDateAdapter },
        {
            provide: MAT_DATE_FORMATS,
            useValue: {
                parse: { dateInput: 'D' },
                display: {
                    dateInput: 'DDD',
                    monthYearLabel: 'LLL yyyy',
                    dateA11yLabel: 'DD',
                    monthYearA11yLabel: 'LLLL yyyy',
                }
            }
        },

        // Transloco setup
        provideTransloco({
            config: {
                availableLangs: [
                    { id: 'en', label: 'English' },
                    { id: 'tr', label: 'Turkish' }
                ],
                defaultLang: 'en',
                fallbackLang: 'en',
                reRenderOnLangChange: true,
                prodMode: !isDevMode(),
            },
            loader: TranslocoHttpLoader
        }),

        // Initialize default language
        provideAppInitializer(() => {
            const translocoService = inject(TranslocoService);
            const defaultLang = translocoService.getDefaultLang();
            translocoService.setActiveLang(defaultLang);
            return firstValueFrom(translocoService.load(defaultLang));
        }),

        provideAuth(),
        provideIcons(),

        provideFuse({
            mockApi: { delay: 0, service: MockApiService },
            fuse: {
                layout: 'classy',
                scheme: 'light',
                screens: { sm: '600px', md: '960px', lg: '1280px', xl: '1440px' },
                theme: 'theme-default',
                themes: [
                    { id: 'theme-default', name: 'Default' },
                    { id: 'theme-brand', name: 'Brand' },
                    { id: 'theme-teal', name: 'Teal' },
                    { id: 'theme-rose', name: 'Rose' },
                    { id: 'theme-purple', name: 'Purple' },
                    { id: 'theme-amber', name: 'Amber' }
                ]
            }
        }),

        // Auth and navigation initializer
        provideAppInitializer(() => {
            const authService = inject(AuthService);
            const navigationService = inject(NavigationService);

            return firstValueFrom(
                authService.checkAuthOnStartup().pipe(
                    switchMap(isAuth =>
                        isAuth ? navigationService.loadNavigation() : of(null)
                    ),
                    catchError(err => {
                        console.error('Auth init failed', err);
                        return of(null);
                    })
                )
            );
        })
    ]
};