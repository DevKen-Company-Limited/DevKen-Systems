import { Routes } from '@angular/router';
import { AuthSignInComponent } from 'app/modules/auth/sign-in/sign-in.component';
import { SsoSetPasswordComponent } from '../set-up-password/sso-set-password.component';
import { SsoVerifyEmailComponent } from '../email-verification/sso-verify-email.component';
import { SsoOtpComponent } from '../email-otp/sso-otp.component';

export default [
    {
        path: '',
        component: AuthSignInComponent,
    },

    {
        path: 'sso/set-password',
        component: SsoSetPasswordComponent,
    },
    {
        path: 'sso/verify-email',
        component: SsoVerifyEmailComponent,
    },
        {
        path: 'sso/otp',
        component: SsoOtpComponent,
    },
] as Routes;


