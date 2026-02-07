import { Route } from '@angular/router';
import { initialDataResolver } from 'app/app.resolvers';
import { AuthGuard } from 'app/core/auth/guards/auth.guard';
import { NoAuthGuard } from 'app/core/auth/guards/noAuth.guard';
import { LayoutComponent } from 'app/layout/layout.component';

// @formatter:off
/* eslint-disable max-len */
/* eslint-disable @typescript-eslint/explicit-function-return-type */
export const appRoutes: Route[] = [

    // Redirect empty path to '/example'
    {path: '', pathMatch : 'full', redirectTo: 'example'},

    // Redirect signed-in user to the '/example'
    //
    // After the user signs in, the sign-in page will redirect the user to the 'signed-in-redirect'
    // path. Below is another redirection for that path to redirect the user to the desired
    // location. This is a small convenience to keep all main routes together here on this file.
    {path: 'signed-in-redirect', pathMatch : 'full', redirectTo: 'example'},

    // Auth routes for guests
    {
        path: '',
        canActivate: [NoAuthGuard],
        canActivateChild: [NoAuthGuard],
        component: LayoutComponent,
        data: {
            layout: 'empty'
        },
        children: [
            {path: 'confirmation-required', loadChildren: () => import('app/modules/auth/confirmation-required/confirmation-required.routes')},
            {path: 'forgot-password', loadChildren: () => import('app/modules/auth/forgot-password/forgot-password.routes')},
            {path: 'reset-password', loadChildren: () => import('app/modules/auth/reset-password/reset-password.routes')},
            {path: 'sign-in', loadChildren: () => import('app/modules/auth/sign-in/sign-in.routes')},
            {path: 'sign-up', loadChildren: () => import('app/modules/auth/sign-up/sign-up.routes')},
             {path: 'change-password', loadChildren: () => import('app/modules/auth/change-password/change-password.component.routes')}
        ]
    },

    // Auth routes for authenticated users
    {
        path: '',
        canActivate: [AuthGuard],
        canActivateChild: [AuthGuard],
        component: LayoutComponent,
        data: {
            layout: 'empty'
        },
        children: [
            {path: 'sign-out', loadChildren: () => import('app/modules/auth/sign-out/sign-out.routes')},
            {path: 'unlock-session', loadChildren: () => import('app/modules/auth/unlock-session/unlock-session.routes')}
        ]
    },

    // Landing routes
    {
        path: '',
        component: LayoutComponent,
        data: {
            layout: 'empty'
        },
        children: [
            {path: 'home', loadChildren: () => import('app/modules/landing/home/home.routes')},
        ]
    },

    // Admin routes
    {
        path: '',
        canActivate: [AuthGuard],
        canActivateChild: [AuthGuard],
        component: LayoutComponent,
        resolve: {
            initialData: initialDataResolver
        },
        children: [
            {path: 'example', loadChildren: () => import('app/modules/admin/example/example.routes')},
        ]
    },




     // ------------------------------------------------------
    // Main Application (Permission-based Navigation)
    // ------------------------------------------------------

    {
        path: '',
        component: LayoutComponent,
        canActivate: [AuthGuard],
        canActivateChild: [AuthGuard],
        resolve: { initialData: initialDataResolver },
        children: [

            // ================= ADMINISTRATION =================
            {
                path: 'administration',
                children: [
                    // { path: 'school', loadChildren: () => import('app/modules/administration/school/school.routes') },
                    // { path: 'users', loadChildren: () => import('app/modules/administration/users/users.routes') },
                    { path: 'roles', loadChildren: () => import('app/RolesAndPermission/role-assignment.component.routes') },
                     { path: 'schools', loadChildren: () => import('app/Tenant/schools-management.routes') },
                     { path: 'users', loadChildren: () => import('app/UserManagement/users-management.component.routes') }
                ]
            },

            // ================= ACADEMIC =================
            {
                path: 'academic',
                children: [
                    // { path: 'students', loadChildren: () => import('app/modules/academic/students/students.routes') },
                    // { path: 'teachers', loadChildren: () => import('app/modules/academic/teachers/teachers.routes') },
                    // { path: 'classes', loadChildren: () => import('app/modules/academic/classes/classes.routes') },
                    // { path: 'subjects', loadChildren: () => import('app/modules/academic/subjects/subjects.routes') },
                    // { path: 'grades', loadChildren: () => import('app/modules/academic/grades/grades.routes') }
                ]
            },

            // ================= ASSESSMENT =================
            {
                path: 'assessment',
                children: [
                    // { path: 'assessments', loadChildren: () => import('app/modules/assessment/assessments/assessments.routes') },
                    // { path: 'reports', loadChildren: () => import('app/modules/assessment/reports/reports.routes') }
                ]
            },

            // ================= FINANCE =================
            {
                path: 'finance',
                children: [
                    // { path: 'fees', loadChildren: () => import('app/modules/finance/fees/fees.routes') },
                    // { path: 'payments', loadChildren: () => import('app/modules/finance/payments/payments.routes') },
                    // { path: 'invoices', loadChildren: () => import('app/modules/finance/invoices/invoices.routes') }
                ]
            },

            // ================= CURRICULUM =================
            {
                path: 'curriculum',
                children: [
                    // { path: 'structure', loadChildren: () => import('app/modules/curriculum/structure/structure.routes') },
                    // { path: 'lesson-plans', loadChildren: () => import('app/modules/curriculum/lesson-plans/lesson-plans.routes') }
                ]
            },

            // ================= SUPER ADMIN =================
            {
                path: 'superadmin',
                children: [
                    // { path: 'settings', loadChildren: () => import('app/modules/superadmin/settings/settings.routes') },
                    // { path: 'logs', loadChildren: () => import('app/modules/superadmin/logs/logs.routes') }
                ]
            }
        ]
    }

];
