import { Injectable, NgZone }    from '@angular/core';
import { HttpClient }             from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { environment }            from 'environments/environment';

declare global {
    interface Window {
        google: {
            accounts: {
                id: {
                    initialize       : (cfg: GoogleIdConfig)                        => void;
                    renderButton     : (el: HTMLElement, opts: GoogleButtonOptions) => void;
                    prompt           : ()                                            => void;
                    disableAutoSelect: ()                                            => void;
                    cancel           : ()                                            => void;
                };
            };
        };
    }
}

interface GoogleIdConfig {
    client_id             : string;
    callback              : (resp: { credential: string }) => void;
    auto_select?          : boolean;
    cancel_on_tap_outside?: boolean;
    ux_mode?              : 'popup' | 'redirect';
    use_fedcm_for_prompt? : boolean;
}

interface GoogleButtonOptions {
    type?          : 'standard' | 'icon';
    theme?         : 'outline' | 'filled_blue' | 'filled_black';
    size?          : 'large' | 'medium' | 'small';
    text?          : 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
    shape?         : 'rectangular' | 'pill' | 'circle' | 'square';
    width?         : number;
    logo_alignment?: 'left' | 'center';
}

// ─── Response shapes ──────────────────────────────────────────────────────────
// Every interface carries ALL four discriminant fields so TypeScript can
// narrow the SsoGoogleResponse union completely without reaching `never`.
// Rule: only ONE interface sets a discriminant to `true`; all others set it
// to `false | undefined`.

export interface SsoLoginResponse {
    success              : boolean;
    requirePasswordSetup : false | undefined;
    requireEmailVerify   : false | undefined;
    requireOtp           : false | undefined;
    message              : string;
    data: {
        accessToken          : string;
        expiresInSeconds     : number;
        refreshToken         : string;
        user: {
            id                   : string;
            email                : string;
            firstName            : string;
            lastName             : string;
            requirePasswordChange: boolean;
            roleNames            : string[];
            permissions          : string[];
        };
    };
}

export interface SsoSetupRequiredResponse {
    success              : boolean;
    requirePasswordSetup : true;
    requireEmailVerify   : false | undefined;
    requireOtp           : false | undefined;
    setupToken           : string;
    message              : string;
    data: {
        email    : string;
        firstName: string;
        lastName : string;
    };
}

export interface SsoEmailVerificationResponse {
    success              : boolean;
    requirePasswordSetup : false | undefined;
    requireEmailVerify   : true;
    requireOtp           : false | undefined;
    message              : string;
    data: {
        email    : string;
        reason   : 'not_verified' | 'domain_not_allowed' | 'no_email';
        firstName: string;
        lastName : string;
    };
}

export interface SsoOtpRequiredResponse {
    success              : boolean;
    requirePasswordSetup : false | undefined;
    requireEmailVerify   : false | undefined;
    requireOtp           : true;
    otpToken             : string;
    message              : string;
    data: {
        email           : string;
        firstName       : string;
        lastName        : string;
        expiresInSeconds: number;
    };
}

export type SsoGoogleResponse =
    | SsoLoginResponse
    | SsoSetupRequiredResponse
    | SsoEmailVerificationResponse
    | SsoOtpRequiredResponse;

/**
 * What POST /api/auth/sso/verify-otp can return:
 *   SsoOtpVerifiedResponse  — OTP accepted, full session issued
 *   SsoSetupRequiredResponse — OTP accepted, new user still needs a password
 *
 * Using a dedicated union (not SsoGoogleResponse) keeps narrowing clean
 * in SsoOtpComponent and avoids the `never` problem.
 */
export type SsoOtpVerifyResponse = SsoLoginResponse | SsoSetupRequiredResponse;

export interface SsoSetPasswordRequest {
    setupToken     : string;
    newPassword    : string;
    confirmPassword: string;
}

export interface SsoVerifyOtpRequest {
    otpToken: string;
    otp     : string;
}

// ─── Service ──────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class SsoService {

    private readonly _apiUrl = environment.apiUrl;

    constructor(
        private _http  : HttpClient,
        private _ngZone: NgZone,
    ) {}

    initGoogle(onToken: (idToken: string) => void): void {
        if (!window.google?.accounts?.id) {
            console.warn('[SsoService] initGoogle() called before GIS script was ready.');
            return;
        }

        window.google.accounts.id.initialize({
            client_id            : environment.sso.google.clientId,
            auto_select          : false,
            cancel_on_tap_outside: true,
            use_fedcm_for_prompt : false,
            callback             : (response) => {
                const token = response?.credential;

                if (!token || typeof token !== 'string' || token.split('.').length !== 3) {
                    console.error('[SsoService] GIS callback: invalid credential shape.', token);
                    return;
                }

                this._ngZone.run(() => onToken(token));
            },
        });
    }

    renderGoogleButton(hostElement: HTMLElement): void {
        if (!window.google?.accounts?.id) { return; }

        window.google.accounts.id.renderButton(hostElement, {
            type          : 'standard',
            theme         : 'outline',
            size          : 'large',
            text          : 'continue_with',
            shape         : 'rectangular',
            logo_alignment: 'left',
            width         : hostElement.offsetWidth || 280,
        });
    }

    promptOneTap(): void {
        if (!window.google?.accounts?.id) {
            console.warn('[SsoService] promptOneTap() called before GIS script was ready.');
            return;
        }
        window.google.accounts.id.prompt();
    }

    cancelOneTap(): void {
        window.google?.accounts?.id?.cancel();
    }

    exchangeGoogleToken(idToken: string): Observable<SsoGoogleResponse> {
        if (!idToken || typeof idToken !== 'string' || idToken.split('.').length !== 3) {
            console.error('[SsoService] exchangeGoogleToken: invalid token.', idToken);
            return throwError(() => new Error('Invalid Google id_token: not a JWT string.'));
        }

        return this._http.post<SsoGoogleResponse>(
            `${this._apiUrl}/api/auth/sso/google`,
            { idToken },
        );
    }

    verifyOtp(payload: SsoVerifyOtpRequest): Observable<SsoOtpVerifyResponse> {
        return this._http.post<SsoOtpVerifyResponse>(
            `${this._apiUrl}/api/auth/sso/verify-otp`,
            payload,
        );
    }

    resendOtp(otpToken: string): Observable<{ success: boolean; message: string; expiresInSeconds: number }> {
        return this._http.post<{ success: boolean; message: string; expiresInSeconds: number }>(
            `${this._apiUrl}/api/auth/sso/resend-otp`,
            { otpToken },
        );
    }

    setPasswordAfterSso(payload: SsoSetPasswordRequest): Observable<SsoLoginResponse> {
        return this._http.post<SsoLoginResponse>(
            `${this._apiUrl}/api/auth/sso/set-password`,
            payload,
        );
    }
}