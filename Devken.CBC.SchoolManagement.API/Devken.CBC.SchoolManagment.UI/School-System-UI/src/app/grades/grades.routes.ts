// grades.routes.ts
import { Routes }                from '@angular/router';
import { GradeDetailsComponent } from './details/grade-details.component';
import { GradeEnrollmentComponent } from './grade-enrollment/grade-enrollment.component';
import { GradesComponent } from './list/grades.component';
export default [
  {
    path: '',
    component: GradesComponent,
  },
  {
    path: 'create',
    component: GradeEnrollmentComponent,
    data: { title: 'Record Grade' },
  },
  {
    path: 'edit/:id',
    component: GradeEnrollmentComponent,
    data: { title: 'Edit Grade' },
  },
  {
    path: 'details/:id',
    component: GradeDetailsComponent,
    data: { title: 'Grade Details' },
  },
] as Routes;