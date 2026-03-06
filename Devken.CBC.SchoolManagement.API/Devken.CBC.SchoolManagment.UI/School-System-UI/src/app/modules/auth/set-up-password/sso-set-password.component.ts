import {
    Component,
    OnInit,
    ViewEncapsulation,
} from '@angular/core';
import {
    AbstractControl,
    FormsModule,
    ReactiveFormsModule,
    UntypedFormBuilder,
    UntypedFormGroup,
    ValidationErrors,
    ValidatorFn,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { fuseAnimations } from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService } from 'app/core/auth/auth.service';
import { SsoService } from 'app/core/DevKenService/GoogleService/SsoLoginResponse';

// ── Password strength validator ───────────────────────────────────────────────
// Mirrors the backend IsStrongPassword() check exactly.
const strongPasswordValidator: ValidatorFn = (ctrl: AbstractControl): ValidationErrors | null => {
    const v: string = ctrl.value ?? '';
    if (v.length < 8) return { tooShort: true };
    if (!/[A-Z]/.test(v)) return { noUppercase: true };
    if (!/[a-z]/.test(v)) return { noLowercase: true };
    if (!/[0-9]/.test(v)) return { noDigit: true };
    if (!/[^a-zA-Z0-9]/.test(v)) return { noSpecial: true };
    return null;
};

// ── Passwords-match cross-field validator ─────────────────────────────────────
const passwordsMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
    const pw = group.get('newPassword')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw && cpw && pw !== cpw ? { passwordsMismatch: true } : null;
};

@Component({
    selector: 'sso-set-password',
    templateUrl: './sso-set-password.component.html',
    styleUrls: ['./sso-set-password.component.scss'],
    encapsulation: ViewEncapsulation.None,
    animations: fuseAnimations,
    standalone: true,
    imports: [
        NgIf,
        FuseAlertComponent,
        FormsModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
    ],
})
export class SsoSetPasswordComponent implements OnInit {

    // ── Alert ──────────────────────────────────────────────────────────────────
    alert: { type: FuseAlertType; message: string } = { type: 'success', message: '' };
    showAlert = false;

    // ── UI state ───────────────────────────────────────────────────────────────
    showNewPassword = false;
    showConfirmPassword = false;
    loading = false;

    // ── Pre-filled from sessionStorage ────────────────────────────────────────
    userEmail = '';
    userFirstName = '';

    // ── Form ───────────────────────────────────────────────────────────────────
    form: UntypedFormGroup;

    // ── Private ────────────────────────────────────────────────────────────────
    private _setupToken = '';

    constructor(
        private _fb: UntypedFormBuilder,
        private _router: Router,
        private _ssoService: SsoService,
        private _authService: AuthService,
    ) { }

    ngOnInit(): void {
        // Read setup token written by sign-in component after Google callback.
        this._setupToken = sessionStorage.getItem('sso_setup_token') ?? '';
        this.userEmail = sessionStorage.getItem('sso_setup_email') ?? '';
        this.userFirstName = sessionStorage.getItem('sso_setup_first_name') ?? '';

        // Guard: if no setup token exists the user navigated here directly — send back.
        if (!this._setupToken) {
            this._router.navigate(['/sign-in']);
            return;
        }

        this.form = this._fb.group(
            {
                newPassword: ['', [Validators.required, strongPasswordValidator]],
                confirmPassword: ['', Validators.required],
            },
            { validators: passwordsMatchValidator },
        );
    }

    // ── Password strength level (0–4) — drives the strength bar in the template ──
    get _strengthLevel(): number {
        const v: string = this.pw?.value ?? '';
        if (!v) return 0;
        let score = 0;
        if (v.length >= 8) score++;
        if (/[A-Z]/.test(v)) score++;
        if (/[0-9]/.test(v)) score++;
        if (/[^a-zA-Z0-9]/.test(v)) score++;
        return score;
    }
    // ── Convenience getters for template ──────────────────────────────────────
    get pw() { return this.form.get('newPassword')!; }
    get cpw() { return this.form.get('confirmPassword')!; }

    // ── Submit ─────────────────────────────────────────────────────────────────
    submit(): void {
        if (this.form.invalid || this.loading) { return; }

        this.loading = true;
        this.showAlert = false;

        this._ssoService.setPasswordAfterSso({
            setupToken: this._setupToken,
            newPassword: this.pw.value,
            confirmPassword: this.cpw.value,
        }).subscribe({
            next: (response) => {
                this.loading = false;

                if (!response.success) {
                    this._showError(response.message || 'Could not set password. Please try again.');
                    return;
                }

                // Clear sensitive session data immediately after success.
                this._clearSessionStorage();

                // Hand off to AuthService — identical path as normal login.
                this._authService.handleSsoLoginResponse(response.data);

                this._router.navigateByUrl('/signed-in-redirect');
            },
            error: (err) => {
                this.loading = false;
                const msg = err?.error?.message || 'Something went wrong. Please try again.';
                this._showError(msg);
            },
        });
    }

    // ── Cancel / back ──────────────────────────────────────────────────────────
    cancel(): void {
        this._clearSessionStorage();
        this._router.navigate(['/sign-in']);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private _showError(message: string): void {
        this.alert = { type: 'error', message };
        this.showAlert = true;
    }

    private _clearSessionStorage(): void {
        sessionStorage.removeItem('sso_setup_token');
        sessionStorage.removeItem('sso_setup_email');
        sessionStorage.removeItem('sso_setup_first_name');
        sessionStorage.removeItem('sso_setup_last_name');
    }
}