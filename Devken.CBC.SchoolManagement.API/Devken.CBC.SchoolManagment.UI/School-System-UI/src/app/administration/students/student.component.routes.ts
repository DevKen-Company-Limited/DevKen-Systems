import { Routes } from "@angular/router";
import { StudentEnrollmentComponent } from "./enrollment/entrollment/student-enrollment.component";
import { StudentsComponent } from "./students.component";

export default[
     {
      path: '',
      component: StudentsComponent,
    },
    {
      path: 'enroll',
      component: StudentEnrollmentComponent,
    },
    {
      path: 'edit/:id',
      component: StudentEnrollmentComponent,
    },
    // {
    //   path: ':id',
    //   component: StudentDetailsComponent, // Optional
    // },
] as Routes;