import { Routes } from "@angular/router";
import { ParentsListComponent } from "./parents-list.component";
import { ParentEnrollmentComponent } from "./parent-enrollment/parent-enrollment.component";


export default [
    { path: '', component: ParentsListComponent },
    { path: 'create', component: ParentEnrollmentComponent },
    { path: 'edit/:id', component: ParentEnrollmentComponent },
] as Routes;
