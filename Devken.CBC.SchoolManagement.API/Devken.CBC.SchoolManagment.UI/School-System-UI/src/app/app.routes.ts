import { Route } from '@angular/router';
import { initialDataResolver } from 'app/app.resolvers';
import { AuthGuard } from 'app/core/auth/guards/auth.guard';
import { NoAuthGuard } from 'app/core/auth/guards/noAuth.guard';
import { LayoutComponent } from 'app/layout/layout.component';
import { changePasswordGuard, passwordChangeRequiredGuard } from './core/auth/guards/password-change-required.guard';

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
            {path: 'sign-up', loadChildren: () => import('app/modules/auth/sign-up/sign-up.routes')}
        ]
    },

    // Change password route - Only accessible when authenticated AND password change is required
    {
        path: '',
        canActivate: [AuthGuard],
        component: LayoutComponent,
        data: {
            layout: 'empty'
        },
        children: [
            {
                path: 'change-password', 
                canActivate: [changePasswordGuard],
                loadChildren: () => import('app/modules/auth/change-password/change-password.component.routes')
            }
        ]
    },

    // Auth routes for authenticated users (sign-out, unlock-session)
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

    // Admin routes - Protected with password change check
    {
        path: '',
        canActivate: [AuthGuard, passwordChangeRequiredGuard],
        canActivateChild: [AuthGuard, passwordChangeRequiredGuard],
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
    // Protected with password change check
    // ------------------------------------------------------
    {
        path: '',
        component: LayoutComponent,
        canActivate: [AuthGuard, passwordChangeRequiredGuard],
        canActivateChild: [AuthGuard, passwordChangeRequiredGuard],
        resolve: { initialData: initialDataResolver },
        children: [

            //UI Design
            {
                path: 'page-design-v1',
                loadChildren: () =>
                    import('app/page-design-version-one/page-design-version-one.routes')
                        .then(m => m.default)
            },

            // ================= ADMINISTRATION =================
            {
                path: 'administration',
                children: [
                    { path: 'roles', loadChildren: () => import('app/RolesAndPermission/role-assignment.component.routes') },
                     { path: 'permissions', loadChildren: () => import('app/RolesAndPermission/permission/role-permission-management.component.routes') },
                    { path: 'schools', loadChildren: () => import('app/Tenant/schools-management.routes') },
                     { path: 'logs', loadChildren: () => import('app/logs/userActivities/user-activity.component.routes') },
                    { path: 'users', loadChildren: () => import('app/UserManagement/users-management.component.routes') }
                ]
            },

            // ================= ACADEMIC =================
            {
                path: 'academic',
                children: [
                    { path: 'academic-years', loadChildren: () => import('app/Academics/AcademicYear/academic-years.routes') },
                    { path: 'terms', loadChildren: () => import('app/Academics/Terms/terms.routes') },
                    // { path: 'students', loadChildren: () => import('app/modules/academic/students/students.routes') },
                    { path: 'students', loadChildren: () => import('app/administration/students/student.component.routes') },
                    { path: 'subjects',       loadChildren: () => import('app/Academics/Subject/subjects.routes').then(m => m.default) },  // ← ADD THIS
                    { path: 'subjects',       loadChildren: () => import('app/Subject/subjects.routes').then(m => m.default) },  // ← ADD THIS
                    { path: 'teachers', loadChildren: () => import('app/Academics/Teachers/teachers.component.routes') },
                    { path: 'classes', loadChildren: () => import('app/Classes/classes-management.component.routes') },
                    { path: 'grades', loadChildren: () => import('app/grades/grades.routes') }
                  //  { path: 'details', loadChildren: () => import('app/administration/students/details/student-details.component.routes') },
                    // { path: 'grades', loadChildren: () => import('app/modules/academic/grades/grades.routes') }
                ]
            },

            {
                path: 'settings',
                children: [
                    { path: 'document-number-series', loadChildren: () => import('app/Settings/NumberSeries/document-number-series.component.routes') },

                ]
            },           

            // ================= ASSESSMENT =================
            {
                path: 'assessment',
                children: [
                    { path: 'assessments', loadChildren: () => import('app/assessment/assessments.routes') },
              
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
                loadChildren: () => import('./curriculum/curriculum.routes'),
                // children: [
                    // { path: 'learning-areas', loadChildren: () => import('app/curriculum/curriculum.routes') },
                    // { path: 'strands', loadChildren: () => import('app/curriculum/curriculum.routes') },
                    // { path: 'sub-strands', loadChildren: () => import('app/curriculum/curriculum.routes') },
                    // { path: 'learning-outcomes', loadChildren: () => import('app/curriculum/curriculum.routes') },
                    
                    // { path: 'learning-area', loadChildren: () => import('app/curriculum/curriculum.routes') },
                    // { path: 'strand': () => import('app/curriculum/curriculum.routes') },
                    // { path: 'substrand', loadChildren: () => import(app/curriculum/curriculum.routes') },
                    // { path: 'learning-outcome', loadChildren: () => import('app/curriculum/curriculum.routes'}

                    // { path: 'structure', loadChildren: () => import('app/modules/curriculum/structure/structure.routes') },
                    // { path: 'lesson-plans', loadChildren: () => import('app/modules/curriculum/lesson-plans/lesson-plans.routes') }
                // ]
            },

            // ================= SUPER ADMIN =================
            {
                path: 'superadmin',
                children: [
                    // { path: 'settings', loadChildren: () => import('app/modules/superadmin/settings/settings.routes') },
                    // { path: 'logs', loadChildren: () => import('app/modules/auth/logs/logs.routes') }
                ]
            }
        ]
    }
];