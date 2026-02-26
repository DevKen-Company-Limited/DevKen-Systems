import { Route } from '@angular/router';
import { initialDataResolver } from './app.resolvers';
import { AuthGuard } from './core/auth/guards/auth.guard';
import { NoAuthGuard } from './core/auth/guards/noAuth.guard';
import { LayoutComponent } from './layout/layout.component';
import { changePasswordGuard, passwordChangeRequiredGuard } from './core/auth/guards/password-change-required.guard';

// @formatter:off
/* eslint-disable max-len */
/* eslint-disable @typescript-eslint/explicit-function-return-type */
export const appRoutes: Route[] = [

    // Redirect empty path to '/example'
    { path: '', pathMatch: 'full', redirectTo: 'example' },

    // Redirect signed-in user to the '/example'
    { path: 'signed-in-redirect', pathMatch: 'full', redirectTo: 'example' },

    // Guest Auth routes
    {
        path: '',
        canActivate: [NoAuthGuard],
        canActivateChild: [NoAuthGuard],
        component: LayoutComponent,
        data: { layout: 'empty' },
        children: [
            { path: 'confirmation-required', loadChildren: () => import('./modules/auth/confirmation-required/confirmation-required.routes') },
            { path: 'forgot-password', loadChildren: () => import('./modules/auth/forgot-password/forgot-password.routes') },
            { path: 'reset-password', loadChildren: () => import('./modules/auth/reset-password/reset-password.routes') },
            { path: 'sign-in', loadChildren: () => import('./modules/auth/sign-in/sign-in.routes') },
            { path: 'sign-up', loadChildren: () => import('./modules/auth/sign-up/sign-up.routes') },
        ]
    },

    // Change password route - Only when authenticated & password change required
    {
        path: '',
        canActivate: [AuthGuard],
        component: LayoutComponent,
        data: { layout: 'empty' },
        children: [
            {
                path: 'change-password',
                canActivate: [changePasswordGuard],
                loadChildren: () => import('./modules/auth/change-password/change-password.component.routes')
            }
        ]
    },

    // Auth routes for authenticated users
    {
        path: '',
        canActivate: [AuthGuard],
        canActivateChild: [AuthGuard],
        component: LayoutComponent,
        data: { layout: 'empty' },
        children: [
            { path: 'sign-out', loadChildren: () => import('./modules/auth/sign-out/sign-out.routes') },
            { path: 'unlock-session', loadChildren: () => import('./modules/auth/unlock-session/unlock-session.routes') }
        ]
    },

    // Landing routes
    {
        path: '',
        component: LayoutComponent,
        data: { layout: 'empty' },
        children: [
            { path: 'home', loadChildren: () => import('./modules/landing/home/home.routes') }
        ]
    },

    // Admin routes - Protected with password change check
    {
        path: '',
        canActivate: [AuthGuard, passwordChangeRequiredGuard],
        canActivateChild: [AuthGuard, passwordChangeRequiredGuard],
        component: LayoutComponent,
        resolve: { initialData: initialDataResolver },
        children: [
            { path: 'example', loadChildren: () => import('./modules/admin/example/example.routes') }
        ]
    },

    // Main App routes (Permission-based)
    {
        path: '',
        component: LayoutComponent,
        canActivate: [AuthGuard, passwordChangeRequiredGuard],
        canActivateChild: [AuthGuard, passwordChangeRequiredGuard],
        resolve: { initialData: initialDataResolver },
        children: [

            // UI Design
            { path: 'page-design-v1', loadChildren: () => import('./page-design-version-one/page-design-version-one.routes').then(m => m.default) },

            // Administration
            {
                path: 'administration',
                children: [
                    { path: 'roles', loadChildren: () => import('./RolesAndPermission/role-assignment.component.routes') },
                    { path: 'permissions', loadChildren: () => import('./RolesAndPermission/permission/role-permission-management.component.routes') },
                    { path: 'schools', loadChildren: () => import('./Tenant/schools-management.routes') },
                    { path: 'logs', loadChildren: () => import('./logs/userActivities/user-activity.component.routes') },
                    { path: 'users', loadChildren: () => import('./UserManagement/users-management.component.routes') }
                ]
            },

            // Academic
            {
                path: 'academic',
                children: [
                    { path: 'academic-years', loadChildren: () => import('./Academics/AcademicYear/academic-years.routes') },
                    { path: 'terms', loadChildren: () => import('./Academics/Terms/terms.routes') },
                    { path: 'students', loadChildren: () => import('./administration/students/student.component.routes') },
                    { path: 'subjects', loadChildren: () => import('./Academics/Subject/subjects.routes').then(m => m.default) },
                    { path: 'teachers', loadChildren: () => import('./Academics/Teachers/teachers.component.routes') },
                    { path: 'classes', loadChildren: () => import('./Classes/classes-management.component.routes') },
                    { path: 'grades', loadChildren: () => import('./grades/grades.routes') }
                ]
            },

            // Settings
            {
                path: 'settings',
                children: [
                    { path: 'document-number-series', loadChildren: () => import('./Settings/NumberSeries/document-number-series.component.routes') }
                ]
            },

            // Assessment
            {
                path: 'assessment',
                children: [
                    { path: 'assessments', loadChildren: () => import('./assessment/Assessments/assessments.component.routes') }
                ]
            },

            // Finance
            {
                path: 'finance',
                children: [
                    { path: 'fees', loadChildren: () => import('./finance/fee-item/fee-items.routes').then(m => m.default)  },
                    { path: 'fee-structure', loadChildren: () => import('./finance/fee-structure/fee-structures.component.routes') }
                ]
            },

            // Curriculum
            {
                path: 'curriculum',
                loadChildren: () => import('./curriculum/curriculum.routes')
            },

            // Super Admin
            {
                path: 'superadmin',
                children: [
                    // Add superadmin modules if needed
                ]
            }

        ]
    }

];