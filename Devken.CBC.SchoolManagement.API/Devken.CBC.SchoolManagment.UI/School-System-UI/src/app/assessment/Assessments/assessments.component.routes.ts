import { Routes } from "@angular/router";
import { AssessmentsComponent } from "./assessments.component";
import { AssessmentFormComponent } from "./form/assessment-form.component";
import { AssessmentDetailComponent } from "./detail/assessment-detail.component";
import { AssessmentGradesComponent } from "./grades/assessment-grades.component";

export default [
  {
    path: '',
    component: AssessmentsComponent,
    data: { title: 'Assessments', breadcrumb: 'Assessments' }
  },
  {
    path: 'create',
    component: AssessmentFormComponent,
    data: { title: 'Create Assessment', breadcrumb: 'Create' }
  },
  {
    path: 'edit/:id',
    component: AssessmentFormComponent,
    data: { title: 'Edit Assessment', breadcrumb: 'Edit' }
  },
  {
    path: 'details/:id',
    component: AssessmentDetailComponent,
    data: { title: 'Assessment Details', breadcrumb: 'Details' }
  },
  {
    path: 'grades/:id',
    component: AssessmentGradesComponent,
    data: { title: 'Assessment Grades', breadcrumb: 'Grades' }
  }
] as Routes;