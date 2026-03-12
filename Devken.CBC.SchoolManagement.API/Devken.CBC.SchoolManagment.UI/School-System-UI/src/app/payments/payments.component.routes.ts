import { Routes } from '@angular/router';
import { PaymentBulkFormComponent } from './bulk/payment-bulk-form.component';
import { PaymentDetailsComponent } from './details/payment-details.component';
import { PaymentFormComponent } from './form/payment-form.component';
import { PaymentsComponent } from './payments.component';
import { PaymentReverseComponent } from './reverse/payment-reverse.component';

export default [
    { path: '',            component: PaymentsComponent        },
    { path: 'create',      component: PaymentFormComponent     },
    { path: 'edit/:id',    component: PaymentFormComponent     },
    { path: 'details/:id', component: PaymentDetailsComponent  },
    { path: 'reverse/:id', component: PaymentReverseComponent  },
    { path: 'bulk',        component: PaymentBulkFormComponent },
] as Routes;