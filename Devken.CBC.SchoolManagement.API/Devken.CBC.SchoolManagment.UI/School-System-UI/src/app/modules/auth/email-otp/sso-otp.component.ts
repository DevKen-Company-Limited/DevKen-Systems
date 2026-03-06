import {
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    QueryList,
    ViewChildren,
    ViewEncapsulation,
} from '@angular/core';
import {
    FormsModule,
    ReactiveFormsModule,
    UntypedFormBuilder,
    UntypedFormGroup,
    Validators,
} from '@angular/forms';
import { Router, RouterLink }       from '@angular/router';
import { NgIf }                     from '@angular/common';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { fuseAnimations }           from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService }              from 'app/core/auth/auth.service';
import {
    SsoService,
    SsoSetupRequiredResponse,
    SsoLoginResponse,
} from 'app/core/DevKenService/GoogleService/SsoLoginResponse';

@Component({
    selector     : 'sso-otp',
    templateUrl  : './sso-otp.component.html',
    styleUrls    : ['./sso-otp.component.scss'],
    encapsulation: ViewEncapsulation.None,
    animations   : fuseAnimations,
    standalone   : true,
    imports: [
        FormsModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
        FuseAlertComponent,
    ],
})
export class SsoOtpComponent implements OnInit, OnDestroy {

    @ViewChildren('otpInput') otpInputs: QueryList<ElementRef<HTMLInputElement>>;

    alert: { type: FuseAlertType; message: string } = { type: 'success', message: '' };
    showAlert   = false;
    loading     = false;
    resending   = false;

    maskedEmail  = '';
    firstName    = '';
    countdown    = 0;
    canResend    = false;

    form: UntypedFormGroup;

    private _otpToken    = '';
    private _countdownId : ReturnType<typeof setInterval> | null = null;

    constructor(
        private _fb         : UntypedFormBuilder,
        private _router     : Router,
        private _ssoService : SsoService,
        private _authService: AuthService,
    ) {}

    ngOnInit(): void {
        this._otpToken   = sessionStorage.getItem('sso_otp_token')      ?? '';
        this.maskedEmail = sessionStorage.getItem('sso_otp_email')      ?? '';
        this.firstName   = sessionStorage.getItem('sso_otp_first_name') ?? '';
        this.countdown   = Number(sessionStorage.getItem('sso_otp_expires') ?? '300');

        if (!this._otpToken) {
            this._router.navigate(['/sign-in']);
            return;
        }

        this.form = this._fb.group({
            d0: ['', [Validators.required, Validators.pattern(/^\d$/)]],
            d1: ['', [Validators.required, Validators.pattern(/^\d$/)]],
            d2: ['', [Validators.required, Validators.pattern(/^\d$/)]],
            d3: ['', [Validators.required, Validators.pattern(/^\d$/)]],
            d4: ['', [Validators.required, Validators.pattern(/^\d$/)]],
            d5: ['', [Validators.required, Validators.pattern(/^\d$/)]],
        });

        this._startCountdown();
    }

    ngOnDestroy(): void {
        this._stopCountdown();
    }

    get otpValue(): string {
        return ['d0','d1','d2','d3','d4','d5']
            .map(k => this.form.get(k)?.value ?? '')
            .join('');
    }

    get countdownDisplay(): string {
        const m = Math.floor(this.countdown / 60).toString().padStart(2, '0');
        const s = (this.countdown % 60).toString().padStart(2, '0');
        return `${m}:${s}`;
    }

    onDigitInput(event: Event, index: number): void {
        const input = event.target as HTMLInputElement;
        const val   = input.value.replace(/\D/g, '').slice(-1);

        this.form.get(`d${index}`)?.setValue(val, { emitEvent: false });
        input.value = val;

        if (val && index < 5) {
            const next = this.otpInputs.toArray()[index + 1];
            next?.nativeElement.focus();
        }

        if (index === 5 && this.form.valid) {
            this.submit();
        }
    }

    onKeyDown(event: KeyboardEvent, index: number): void {
        if (event.key === 'Backspace') {
            const current = this.form.get(`d${index}`)?.value;
            if (!current && index > 0) {
                this.form.get(`d${index - 1}`)?.setValue('');
                const prev = this.otpInputs.toArray()[index - 1];
                prev?.nativeElement.focus();
            }
        }
    }

    onPaste(event: ClipboardEvent): void {
        event.preventDefault();
        const text = event.clipboardData?.getData('text') ?? '';
        const digits = text.replace(/\D/g, '').slice(0, 6).split('');
        digits.forEach((d, i) => {
            this.form.get(`d${i}`)?.setValue(d);
            const input = this.otpInputs.toArray()[i];
            if (input) { input.nativeElement.value = d; }
        });
        if (digits.length === 6) { this.submit(); }
    }

    submit(): void {
        if (this.form.invalid || this.loading) { return; }

        this.loading   = true;
        this.showAlert = false;

        this._ssoService.verifyOtp({
            otpToken: this._otpToken,
            otp     : this.otpValue,
        }).subscribe({
            next : (response) => {
                this.loading = false;

                if (!response.success) {
                    this._showError(response.message || 'Invalid code. Please try again.');
                    this._resetInputs();
                    return;
                }

                this._clearOtpSession();

                // OTP verified — check if new user still needs password setup
                if (response.requirePasswordSetup === true) {
                    const su = response as SsoSetupRequiredResponse;
                    sessionStorage.setItem('sso_setup_token',      su.setupToken);
                    sessionStorage.setItem('sso_setup_email',      su.data.email);
                    sessionStorage.setItem('sso_setup_first_name', su.data.firstName);
                    sessionStorage.setItem('sso_setup_last_name',  su.data.lastName);
                    this._router.navigate(['/sign-in/sso/set-password']);
                    return;
                }

                // Fully authenticated
                const login = response as SsoLoginResponse;
                this._authService.handleSsoLoginResponse(login.data);

                if (login.data.user.requirePasswordChange) {
                    this._router.navigate(['/change-password']);
                    return;
                }

                this._router.navigateByUrl('/signed-in-redirect');
            },
            error: (err) => {
                this.loading = false;
                const msg = err?.error?.message || 'Verification failed. Please try again.';
                this._showError(msg);
                this._resetInputs();
            },
        });
    }

    resendOtp(): void {
        if (!this.canResend || this.resending) { return; }

        this.resending = true;
        this.showAlert = false;

        this._ssoService.resendOtp(this._otpToken).subscribe({
            next : (res) => {
                this.resending = false;
                this.canResend = false;
                this.countdown = res.expiresInSeconds;
                this._startCountdown();
                this._resetInputs();
                this.alert     = { type: 'success', message: 'A new code has been sent to your email.' };
                this.showAlert = true;
            },
            error: (err) => {
                this.resending = false;
                this._showError(err?.error?.message || 'Could not resend code. Please try again.');
            },
        });
    }

    cancel(): void {
        this._clearOtpSession();
        this._router.navigate(['/sign-in']);
    }

    private _startCountdown(): void {
        this._stopCountdown();
        this.canResend   = false;
        this._countdownId = setInterval(() => {
            this.countdown--;
            if (this.countdown <= 0) {
                this.countdown = 0;
                this.canResend = true;
                this._stopCountdown();
            }
        }, 1000);
    }

    private _stopCountdown(): void {
        if (this._countdownId) {
            clearInterval(this._countdownId);
            this._countdownId = null;
        }
    }

    private _resetInputs(): void {
        ['d0','d1','d2','d3','d4','d5'].forEach(k => this.form.get(k)?.setValue(''));
        setTimeout(() => this.otpInputs?.first?.nativeElement.focus(), 0);
    }

    private _showError(message: string): void {
        this.alert     = { type: 'error', message };
        this.showAlert = true;
    }

    private _clearOtpSession(): void {
        ['sso_otp_token','sso_otp_email','sso_otp_first_name',
         'sso_otp_last_name','sso_otp_expires'].forEach(k => sessionStorage.removeItem(k));
    }
}