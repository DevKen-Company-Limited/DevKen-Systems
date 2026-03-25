// book-borrow/book-borrows.routes.ts
import { Routes } from '@angular/router';
import { BookBorrowsComponent } from './book-borrows.component';

export default [
  { path: '', component: BookBorrowsComponent },
  // Specific Returns view
  { 
    path: 'book-returns', 
    component: BookBorrowsComponent, 
    data: { mode: 'returns' } 
  },
] as Routes;