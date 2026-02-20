import { Routes } from "@angular/router";
import { AssessmentsComponent } from "./assessments.component";
import { AssessmentFormComponent } from "./form/assessment-form.component";
import { AssessmentDetailsStepComponent } from "./steps/assessment-details-step.component";


export default [
  {
    path: '',
    component: AssessmentsComponent
  },
  {
    path: 'create',
    component: AssessmentFormComponent,
  },
  {
    path: 'edit/:id',
    component: AssessmentFormComponent, // reuse create component for edit
  },
  {
    path: 'details/:id',
    component: AssessmentDetailsStepComponent,
    data: {
      title: 'Assessment Details',
      breadcrumb: 'Details'
    }
  },
  {
    path: 'grades/:id',
    component: AssessmentsComponent,
    data: {
      title: 'Assessment Grades',
      breadcrumb: 'Grades'
    }
  }
] as Routes;
