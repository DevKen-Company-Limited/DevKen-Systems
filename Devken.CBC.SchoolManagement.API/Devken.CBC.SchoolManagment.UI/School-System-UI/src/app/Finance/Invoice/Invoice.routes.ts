import { Routes } from "@angular/router";
import { InvoicesListComponent } from "./invoices-list.component";
import { InvoiceEnrollmentComponent } from "./invoice-enrollment/invoice-enrollment.component";


export default [
    { path: '', component: InvoicesListComponent },
    { path: 'create', component: InvoiceEnrollmentComponent },
    { path: 'edit/:id', component: InvoiceEnrollmentComponent },
] as Routes;
