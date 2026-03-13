import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BulkInvoiceStudentRow, BulkInvoiceFeeItem } from '../../Types/bulk-invoice.types';


@Component({
  selector: 'app-bulk-invoice-review',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './bulk-invoice-review.component.html',
})
export class BulkInvoiceReviewComponent {
  @Input() scope: any = {};
  @Input() students: BulkInvoiceStudentRow[] = [];
  @Input() feeItems: BulkInvoiceFeeItem[] = [];
  @Input() className = '';
  @Output() editSection = new EventEmitter<number>();

  get selectedStudents(): BulkInvoiceStudentRow[] {
    return this.students.filter(s => s.selected);
  }

  getItemTotal(item: BulkInvoiceFeeItem): number {
    const sub = item.quantity * item.unitPrice - (item.discount || 0);
    return item.isTaxable ? sub + sub * (item.taxRate || 0) / 100 : sub;
  }

  get perStudentTotal(): number {
    return this.feeItems.reduce((s, i) => s + this.getItemTotal(i), 0);
  }

  get grandTotal(): number {
    return this.perStudentTotal * this.selectedStudents.length;
  }

  fmt(val: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', maximumFractionDigits: 0,
    }).format(val);
  }

  formatDate(val: string | Date): string {
    if (!val) return '—';
    const d = new Date(val);
    return isNaN(d.getTime()) ? '—' : d.toLocaleDateString();
  }
}