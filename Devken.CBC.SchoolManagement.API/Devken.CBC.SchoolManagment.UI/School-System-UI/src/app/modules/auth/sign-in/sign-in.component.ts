import { Component, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import {
    FormsModule,
    NgForm,
    ReactiveFormsModule,
    UntypedFormBuilder,
    UntypedFormGroup,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgIf, NgStyle } from '@angular/common';
import { fuseAnimations } from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { AuthService } from 'app/core/auth/auth.service';

export interface RolePreset {
    label: string;
    email: string;
    password: string;
}

export interface Avatar {
    initials: string;
    bg: string;
}

@Component({
    selector     : 'auth-sign-in',
    templateUrl  : './sign-in.component.html',
    styleUrls    : ['./sign-in.component.scss'],
    encapsulation: ViewEncapsulation.None,
    animations   : fuseAnimations,
    standalone   : true,
    imports      : [
        RouterLink,
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
export class AuthSignInComponent implements OnInit
{
    @ViewChild('signInNgForm') signInNgForm: NgForm;

    // ── Alert ──────────────────────────────────────────────────────────────────
    alert: { type: FuseAlertType; message: string } = {
        type   : 'success',
        message: '',
    };
    showAlert = false;

    // ── UI state ───────────────────────────────────────────────────────────────
    showPassword = false;
    showIcon     = true;

    // ── Role switcher ──────────────────────────────────────────────────────────
    activeRole = 'Super Admin';

    roles: RolePreset[] = [
        { label: 'Super Admin', email: 'superadmin@devken.com', password: 'SuperAdmin@123'  },
        { label: 'Principal',   email: 'principal@school.com', password: 'Principal@123'   },
        { label: 'Teacher',     email: 'teacher@school.com',   password: 'Teacher@123'     },
        { label: 'Parent',      email: 'parent@school.com',    password: 'Parent@123'      },
    ];

    // ── Right panel ────────────────────────────────────────────────────────────
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

    // ── Form ───────────────────────────────────────────────────────────────────
    signInForm: UntypedFormGroup;

    // ── Constructor ────────────────────────────────────────────────────────────
    constructor(
        private _activatedRoute: ActivatedRoute,
        private _authService   : AuthService,
        private _formBuilder   : UntypedFormBuilder,
        private _router        : Router,
    ) {}

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    ngOnInit(): void
    {
        this.signInForm = this._formBuilder.group({
            email     : ['superadmin@devken.com', [Validators.required, Validators.email]],
            password  : ['SuperAdmin@123',         Validators.required],
            rememberMe: [''],
        });
    }

    // ── Public methods ─────────────────────────────────────────────────────────

    /** Pre-fill credentials for the selected role */
    setRole(role: RolePreset): void
    {
        this.activeRole = role.label;
        this.signInForm.patchValue({ email: role.email, password: role.password });
        this.showAlert = false;
    }

    /** Submit sign-in */
    signIn(): void
    {
        if (this.signInForm.invalid) { return; }

        this.signInForm.disable();
        this.showAlert  = false;
        this.showIcon   = false;

        const email        = this.signInForm.get('email').value as string;
        const isSuperAdmin = email.toLowerCase().includes('superadmin');

        const authCall = isSuperAdmin
            ? this._authService.superAdminSignIn(this.signInForm.value)
            : this._authService.signIn(this.signInForm.value);

        authCall.subscribe({
            next: (response) =>
            {
                if (response.data.user.requirePasswordChange)
                {
                    this._router.navigate(['/change-password']);
                    return;
                }

                const redirectURL =
                    this._activatedRoute.snapshot.queryParamMap.get('redirectURL') ||
                    '/signed-in-redirect';

                this._router.navigateByUrl(redirectURL);
            },
            error: (response) =>
            {
                this.signInForm.enable();
                this.signInNgForm.resetForm();
                this.showIcon = true;

                this.alert = {
                    type   : 'error',
                    message: response.message || 'Wrong email or password',
                };
                this.showAlert = true;
            },
        });
    }

    /** Cancel — force password change flow logout */
    logout(): void
    {
        const confirmed = confirm(
            'Are you sure you want to cancel? You must change your password to access the system.',
        );
        if (confirmed)
        {
            this._authService.signOut();
            this._router.navigate(['/sign-in']);
        }
    }
}