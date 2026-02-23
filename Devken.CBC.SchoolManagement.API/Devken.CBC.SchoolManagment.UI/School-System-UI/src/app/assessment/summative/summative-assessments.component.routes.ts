import { Routes } from "@angular/router";
import { SummativeAssessmentsComponent } from "./summative-assessments.component";
import { SummativeAssessmentFormComponent } from "./forms/summative-assessment-form.component";
import { SummativeAssessmentInfoComponent } from "./steps/summative-assessment-info.component";
import { SummativeAssessmentGradeComponent } from "./grades/summative-assessment-grade.component";


export default [
  {
    path: '',
    component: SummativeAssessmentsComponent
  },
  {
    path: 'create',
    component: SummativeAssessmentFormComponent,
  },
  {
    path: 'edit/:id',
    component: SummativeAssessmentFormComponent, // reuse for edit
  },
  {
    path: 'details/:id',
    component: SummativeAssessmentInfoComponent,
    data: {
      title: 'Summative Assessment Details',
      breadcrumb: 'Details'
    }
  },
  {
    path: 'grade/:id',
    component: SummativeAssessmentGradeComponent,
    data: {
      title: 'Grade Students',
      breadcrumb: 'Grading'
    }
  }
] as Routes;
