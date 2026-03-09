import {
    Component,
    OnInit,
    ViewEncapsulation,
} from '@angular/core';
import { Router, RouterLink }   from '@angular/router';
import { NgIf }                 from '@angular/common';
import { MatButtonModule }      from '@angular/material/button';
import { MatIconModule }        from '@angular/material/icon';
import { fuseAnimations }       from '@fuse/animations';
import { FuseAlertComponent }   from '@fuse/components/alert';

type VerifyReason = 'not_verified' | 'domain_not_allowed' | 'no_email';

@Component({
    selector     : 'sso-verify-email',
    templateUrl  : './sso-verify-email.component.html',
    styleUrls    : ['./sso-verify-email.component.scss'],
    encapsulation: ViewEncapsulation.None,
    animations   : fuseAnimations,
    standalone   : true,
    imports: [
        RouterLink,
   
        MatButtonModule,
        MatIconModule,
   
    ],
})
export class SsoVerifyEmailComponent implements OnInit {

    email     = '';
    firstName = '';
    reason    : VerifyReason = 'not_verified';

    constructor(private _router: Router) {}

    ngOnInit(): void {
        this.email     = sessionStorage.getItem('sso_verify_email')      ?? '';
        this.firstName = sessionStorage.getItem('sso_verify_first_name') ?? '';
        this.reason    = (sessionStorage.getItem('sso_verify_reason') ?? 'not_verified') as VerifyReason;

        // Guard: if no context exists the user navigated here directly.
        if (!this.email && !this.reason) {
            this._router.navigate(['/sign-in']);
        }
    }

    // ── Computed helpers for template ─────────────────────────────────────────

    get headingText(): string {
        switch (this.reason) {
            case 'not_verified'     : return 'Verify your email';
            case 'domain_not_allowed': return 'Email domain not allowed';
            case 'no_email'         : return 'No email on your Google account';
            default                 : return 'Sign-in issue';
        }
    }

    get bodyText(): string {
        switch (this.reason) {
            case 'not_verified':
                return `Your Google account email ${this.email ? '(' + this.email + ')' : ''} has not been verified by Google yet. Please verify it in your Google account settings, then try signing in again.`;
            case 'domain_not_allowed':
                return `The email address ${this.email ? '(' + this.email + ')' : ''} is not from an allowed school domain. Please sign in with your school-issued email address, or contact your administrator.`;
            case 'no_email':
                return 'Your Google account does not have an email address associated with it. Please add an email to your Google account and try again.';
            default:
                return 'There was a problem with your Google account. Please try again or contact your administrator.';
        }
    }

    get iconName(): string {
        switch (this.reason) {
            case 'not_verified'      : return 'heroicons_solid:envelope';
            case 'domain_not_allowed': return 'heroicons_solid:shield-exclamation';
            case 'no_email'          : return 'heroicons_solid:exclamation-circle';
            default                  : return 'heroicons_solid:exclamation-circle';
        }
    }

    get showGoogleLink(): boolean {
        return this.reason === 'not_verified' || this.reason === 'no_email';
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    tryAgain(): void {
        this._clearSession();
        this._router.navigate(['/sign-in']);
    }

    openGoogleAccount(): void {
        window.open('https://myaccount.google.com/email', '_blank', 'noopener,noreferrer');
    }

    private _clearSession(): void {
        sessionStorage.removeItem('sso_verify_email');
        sessionStorage.removeItem('sso_verify_reason');
        sessionStorage.removeItem('sso_verify_first_name');
        sessionStorage.removeItem('sso_verify_last_name');
    }
}