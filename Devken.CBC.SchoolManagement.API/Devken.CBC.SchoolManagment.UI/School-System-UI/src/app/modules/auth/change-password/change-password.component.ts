import { Component, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import {
    AbstractControl,
    FormsModule,
    NgForm,
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
import { fuseAnimations } from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService } from 'app/core/auth/auth.service';

@Component({
    selector: 'auth-change-password',
    templateUrl: './change-password.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: fuseAnimations,
    standalone: true,
    imports: [
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
export class AuthChangePasswordComponent implements OnInit {
    @ViewChild('changePasswordNgForm') changePasswordNgForm: NgForm;

    alert: { type: FuseAlertType; message: string } = {
        type: 'success',
        message: '',
    };
    changePasswordForm: UntypedFormGroup;
    showAlert: boolean = false;

    constructor(
        private _authService: AuthService,
        private _formBuilder: UntypedFormBuilder,
        private _router: Router
    ) {}

    ngOnInit(): void {
        // Create the form with password validators
        this.changePasswordForm = this._formBuilder.group({
            currentPassword: ['', Validators.required],
            newPassword: [
                '',
                [
                    Validators.required,
                    Validators.minLength(8),
                    Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/),
                ],
            ],
            confirmPassword: ['', Validators.required],
        }, {
            validators: this.passwordMatchValidator()
        });
    }

    /**
     * Custom validator to check if passwords match
     */
    passwordMatchValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const newPassword = control.get('newPassword');
            const confirmPassword = control.get('confirmPassword');

            if (!newPassword || !confirmPassword) {
                return null;
            }

            return newPassword.value === confirmPassword.value
                ? null
                : { passwordMismatch: true };
        };
    }

    /**
     * Change password
     */
    changePassword(): void {
        if (this.changePasswordForm.invalid) {
            return;
        }

        this.changePasswordForm.disable();
        this.showAlert = false;

        this._authService
            .changePassword({
                currentPassword: this.changePasswordForm.get('currentPassword').value,
                newPassword: this.changePasswordForm.get('newPassword').value,
            })
            .subscribe({
                next: () => {
                    this.alert = {
                        type: 'success',
                        message: 'Password changed successfully. Please sign in with your new password.',
                    };
                    this.showAlert = true;

                    // Sign out and redirect to sign-in page after 2 seconds
                    setTimeout(() => {
                        this._authService.signOut();
                        this._router.navigate(['/sign-in']);
                    }, 2000);
                },
                error: (response) => {
                    this.changePasswordForm.enable();
                    this.changePasswordNgForm.resetForm(this.changePasswordForm.value);

                    this.alert = {
                        type: 'error',
                        message: response.message || 'Failed to change password. Please try again.',
                    };

                    this.showAlert = true;
                },
            });
    }

    /**
     * Logout
     */
    logout(): void {
        this._authService.signOut();
        this._router.navigate(['/sign-in']);
    }
}