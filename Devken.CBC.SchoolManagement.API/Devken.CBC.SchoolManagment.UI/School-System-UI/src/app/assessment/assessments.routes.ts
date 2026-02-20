import { Routes } from '@angular/router';

export default[

     {
    path: '',
    loadComponent: () =>
      import('./list/assessments.component').then(m => m.AssessmentsComponent),
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./assessment-enrollment/assessment-enrollment.component').then(
        m => m.AssessmentEnrollmentComponent
      ),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./assessment-enrollment/assessment-enrollment.component').then(
        m => m.AssessmentEnrollmentComponent
      ),
  },
  {
    path: 'details/:id',
    loadComponent: () =>
      import('./details/assessment-details.component').then(
        m => m.AssessmentDetailsComponent
      ),
  },

] as Routes;