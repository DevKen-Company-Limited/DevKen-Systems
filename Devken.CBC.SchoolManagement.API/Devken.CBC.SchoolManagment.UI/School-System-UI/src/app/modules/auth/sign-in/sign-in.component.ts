import {
    AfterViewInit,
    Component,
    ElementRef,
    NgZone,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
} from '@angular/core';
import {
    FormsModule,
    NgForm,
    ReactiveFormsModule,
    UntypedFormBuilder,
    UntypedFormGroup,
    Validators,
} from '@angular/forms';
import { MatButtonModule }          from '@angular/material/button';
import { MatCheckboxModule }         from '@angular/material/checkbox';
import { MatFormFieldModule }        from '@angular/material/form-field';
import { MatIconModule }             from '@angular/material/icon';
import { MatInputModule }            from '@angular/material/input';
import { MatProgressSpinnerModule }  from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
<<<<<<< HEAD
import { NgStyle }                   from '@angular/common';
import { fuseAnimations }            from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService }               from 'app/core/auth/auth.service';

// ─── Interfaces ───────────────────────────────────────────────────────────────
=======
import { NgIf, NgStyle }             from '@angular/common';
import { fuseAnimations }            from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService }               from 'app/core/auth/auth.service';
import {
    SsoGoogleResponse,
    SsoEmailVerificationResponse,
    SsoOtpRequiredResponse,
    SsoSetupRequiredResponse,
    SsoService,
    SsoLoginResponse,
} from 'app/core/DevKenService/GoogleService/SsoLoginResponse';
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481

export interface RolePreset {
    label   : string;
    email   : string;
    password: string;
}

export interface Avatar {
    initials: string;
    bg      : string;
}

// ─── Component ────────────────────────────────────────────────────────────────

@Component({
    selector     : 'auth-sign-in',
    templateUrl  : './sign-in.component.html',
    styleUrls    : ['./sign-in.component.scss'],
    encapsulation: ViewEncapsulation.None,
    animations   : fuseAnimations,
    standalone   : true,
    imports: [
        RouterLink,
        NgIf,
        NgStyle,
        FuseAlertComponent,
        FormsModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatCheckboxModule,
        MatProgressSpinnerModule,
    ],
})
export class AuthSignInComponent implements OnInit, AfterViewInit, OnDestroy {

    @ViewChild('signInNgForm')  signInNgForm : NgForm;
    @ViewChild('roleSwitcher')  roleSwitcher : ElementRef<HTMLDivElement>;
    @ViewChild('googleBtnHost') googleBtnHost: ElementRef<HTMLDivElement>;

    alert: { type: FuseAlertType; message: string } = { type: 'success', message: '' };
    showAlert          = false;
    showPassword       = false;
    googleLoading      = false;
    googleScriptFailed = false;

    activeRole = 'Super Admin';

    roles: RolePreset[] = [
<<<<<<< HEAD
        { label: 'Super Admin', email: 'superadmin@devken.com', password: 'SuperAdmin@123' },
        { label: 'Principal',   email: 'principal@school.com',  password: 'Principal@123'  },
        { label: 'Teacher',     email: 'teacher@school.com',    password: 'Teacher@123'    },
        { label: 'Parent',      email: 'parent@school.com',     password: 'Parent@123'     },
    ];

    // ── Right panel data ───────────────────────────────────────────────────────
=======
        { label: 'Super Admin',     email: 'superadmin@devken.com',        password: 'SuperAdmin@123'  },
        { label: 'School Admin',    email: 'admin@defaultschool.com',       password: 'Admin@123'       },
        { label: 'Head Teacher',    email: 'headteacher@defaultschool.com', password: 'HeadTeacher@123' },
        { label: 'Teacher',         email: 'teacher@defaultschool.com',     password: 'Teacher@123'     },
        { label: 'Registrar',       email: 'registrar@defaultschool.com',   password: 'Registrar@123'   },
        { label: 'Finance Officer', email: 'finance@defaultschool.com',     password: 'Finance@123'     },
        { label: 'Accountant',      email: 'accountant@defaultschool.com',  password: 'Accountant@123'  },
        { label: 'Cashier',         email: 'cashier@defaultschool.com',     password: 'Cashier@123'     },
        { label: 'Librarian',       email: 'librarian@defaultschool.com',   password: 'Librarian@123'   },
        { label: 'Parent',          email: 'parent@defaultschool.com',      password: 'Parent@123'      },
    ];

>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481
    features: string[] = [
        'Competency-based assessment tracking (EE · ME · AE · BE)',
        'Full CBC grade support: PP1, PP2, Grade 1–6, JHS 1–3',
        'Lesson plans, strands & learning area management',
        'Progress reports, fee collection & parent portal',
    ];

    avatars: Avatar[] = [
        { initials: 'PM', bg: 'linear-gradient(135deg,#3b82f6,#8b5cf6)' },
        { initials: 'AK', bg: 'linear-gradient(135deg,#10b981,#3b82f6)' },
        { initials: 'NW', bg: 'linear-gradient(135deg,#f59e0b,#ef4444)' },
        { initials: 'JO', bg: 'linear-gradient(135deg,#8b5cf6,#ec4899)' },
    ];

    signInForm: UntypedFormGroup;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _authService   : AuthService,
        private _ssoService    : SsoService,
        private _formBuilder   : UntypedFormBuilder,
        private _router        : Router,
        private _ngZone        : NgZone,
    ) {}

    ngOnInit(): void {
        this.signInForm = this._formBuilder.group({
            email     : ['superadmin@devken.com', [Validators.required, Validators.email]],
            password  : ['SuperAdmin@123',          Validators.required],
            rememberMe: [false],
        });
    }

    ngAfterViewInit(): void {
        this._waitForGoogleScript(() => {
            this._ssoService.initGoogle((idToken) => this._onGoogleToken(idToken));
            if (this.googleBtnHost?.nativeElement) {
                this._ssoService.renderGoogleButton(this.googleBtnHost.nativeElement);
            }
        });
    }

<<<<<<< HEAD
    /**
     * Pre-fill credentials when a role tab is clicked.
     */
    setRole(role: RolePreset): void
    {
=======
    ngOnDestroy(): void {
        this._ssoService.cancelOneTap();
    }

    setRole(role: RolePreset): void {
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481
        this.activeRole = role.label;
        this.signInForm.patchValue({ email: role.email, password: role.password });
        this.showAlert = false;
        setTimeout(() => this._scrollActivePillIntoView(), 0);
    }

<<<<<<< HEAD
    /**
     * Submit the sign-in form.
     * Calls superAdminSignIn for super-admin emails, signIn for everyone else.
     */
    signIn(): void
    {
=======
    signIn(): void {
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481
        if (this.signInForm.invalid) { return; }

        this.signInForm.disable();
        this.showAlert = false;
<<<<<<< HEAD
        this.showIcon  = false;
=======
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481

        const email        = this.signInForm.get('email')!.value as string;
        const isSuperAdmin = email.toLowerCase().includes('superadmin');

        const authCall = isSuperAdmin
            ? this._authService.superAdminSignIn(this.signInForm.value)
            : this._authService.signIn(this.signInForm.value);

        authCall.subscribe({
<<<<<<< HEAD
            next: (response) =>
            {
                // Redirect to password-change flow if required
                if (response.data.user.requirePasswordChange)
                {
=======
            next : (response) => {
                if (response.data.user.requirePasswordChange) {
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481
                    this._router.navigate(['/change-password']);
                    return;
                }
                const redirectURL =
                    this._activatedRoute.snapshot.queryParamMap.get('redirectURL') ||
                    '/signed-in-redirect';
                this._router.navigateByUrl(redirectURL);
            },
            error: (response) => {
                this.signInForm.enable();
                this.signInNgForm.resetForm();
                this.alert = {
                    type   : 'error',
                    message: response.message || 'Wrong email or password',
                };
                this.showAlert = true;
            },
        });
    }

<<<<<<< HEAD
    /**
     * Cancel during a forced password-change flow.
     * Signs the user out and returns them to the sign-in page.
     */
    logout(): void
    {
=======
    loginWithGoogle(): void {
        if (this.googleLoading || this.googleScriptFailed) { return; }
        this._ssoService.promptOneTap();
    }

    logout(): void {
>>>>>>> eed5cf6203fce924377e02c550155d7fe07d1481
        const confirmed = confirm(
            'Are you sure you want to cancel? You must change your password to access the system.',
        );
        if (confirmed) {
            this._authService.signOut();
            this._router.navigate(['/sign-in']);
        }
    }

private _onGoogleToken(idToken: string): void {
    this.googleLoading = true;
    this.showAlert     = false;

    this._ssoService.exchangeGoogleToken(idToken).subscribe({
        next : (response: SsoGoogleResponse) => {
            this.googleLoading = false;

            // STEP 1 — email not verified or domain not allowed
            if (response.requireEmailVerify === true) {
                const ev = response as SsoEmailVerificationResponse;
                sessionStorage.setItem('sso_verify_email',      ev.data.email);
                sessionStorage.setItem('sso_verify_reason',     ev.data.reason);
                sessionStorage.setItem('sso_verify_first_name', ev.data.firstName);
                sessionStorage.setItem('sso_verify_last_name',  ev.data.lastName);
                this._router.navigate(['/sign-in/sso/verify-email']);
                return;
            }

            // STEP 2 — OTP required (mandatory for every Google sign-in)
            if (response.requireOtp === true) {
                const otp = response as SsoOtpRequiredResponse;
                sessionStorage.setItem('sso_otp_token',      otp.otpToken);
                sessionStorage.setItem('sso_otp_email',      otp.data.email);
                sessionStorage.setItem('sso_otp_first_name', otp.data.firstName);
                sessionStorage.setItem('sso_otp_last_name',  otp.data.lastName);
                sessionStorage.setItem('sso_otp_expires',    String(otp.data.expiresInSeconds));
                this._router.navigate(['/sign-in/sso/otp']);
                return;
            }

            // STEP 3 — new user needs a password
            if (response.requirePasswordSetup === true) {
                const su = response as SsoSetupRequiredResponse;
                sessionStorage.setItem('sso_setup_token',      su.setupToken);
                sessionStorage.setItem('sso_setup_email',      su.data.email);
                sessionStorage.setItem('sso_setup_first_name', su.data.firstName);
                sessionStorage.setItem('sso_setup_last_name',  su.data.lastName);
                this._router.navigate(['/sign-in/sso/set-password']);
                return;
            }

            // STEP 4 — TypeScript now knows this is SsoLoginResponse.
            // Cast explicitly — after three `=== true` checks + returns,
            // the compiler narrows to `never` without the cast because all
            // four union members have been eliminated by their discriminants.
            const login = response as SsoLoginResponse;

            if (!login.success) {
                this._showSsoError(login.message || 'Google sign-in failed.');
                return;
            }

            this._authService.handleSsoLoginResponse(login.data);

            if (login.data.user.requirePasswordChange) {
                this._router.navigate(['/change-password']);
                return;
            }

            const redirectURL =
                this._activatedRoute.snapshot.queryParamMap.get('redirectURL') ||
                '/signed-in-redirect';
            this._router.navigateByUrl(redirectURL);
        },
        error: (err) => {
            this.googleLoading = false;
            const msg = err?.error?.message || 'Google sign-in failed. Please try again.';
            this._showSsoError(msg);
        },
    });
}

    private _showSsoError(message: string): void {
        this.alert     = { type: 'error', message };
        this.showAlert = true;
    }

    private _waitForGoogleScript(callback: () => void, attempts = 0): void {
        if (window.google?.accounts?.id) {
            this._ngZone.run(() => callback());
            return;
        }
        if (attempts >= 100) {
            console.warn('[SignIn] Google GIS script did not load within 10 s.');
            this._ngZone.run(() => this.googleScriptFailed = true);
            return;
        }
        setTimeout(() => this._waitForGoogleScript(callback, attempts + 1), 100);
    }

    private _scrollActivePillIntoView(): void {
        const track = this.roleSwitcher?.nativeElement;
        if (!track) { return; }
        const activeBtn = track.querySelector<HTMLButtonElement>('.cbc-role-btn.active');
        if (!activeBtn) { return; }
        track.scrollTo({
            left    : activeBtn.offsetLeft - (track.clientWidth / 2) + (activeBtn.offsetWidth / 2),
            behavior: 'smooth',
        });
    }
}