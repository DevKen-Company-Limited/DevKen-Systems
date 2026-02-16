import { Routes } from "@angular/router";
import { StudentDetailsComponent } from "./student-details.component";

export default[

    {
      path: ':id',
      component: StudentDetailsComponent, // Optional
    },
] as Routes;