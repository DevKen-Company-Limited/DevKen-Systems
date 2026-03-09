import { Routes } from '@angular/router';
import { InvoiceItemsComponent } from './invoice-items.component';

export default [
    // Accessed from nav menu — no invoiceId in path
    {
        path: '',
        component: InvoiceItemsComponent,
    },
    // Navigated from invoice detail page with path param
    // Component already reads paramMap.get('invoiceId') first, so this just works
    // {
    //     path: ':invoiceId',
    //     component: InvoiceItemsComponent,
    // },
] as Routes;