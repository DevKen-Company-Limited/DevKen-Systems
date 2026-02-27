import { Component, Inject } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

import { MatSnackBar } from '@angular/material/snack-bar';
import { take } from 'rxjs/operators';
import { InvoiceService } from 'app/core/DevKenService/Invoice/Invoice.service ';
import { InvoiceStatus, InvoiceDialogData } from '../Types/Invoice.types';

@Component({
  selector: 'app-invoice-view-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    CurrencyPipe,
    DatePipe,
  ],
  templateUrl: './invoice-view-dialog.component.html',
})
export class InvoiceViewDialogComponent {
  InvoiceStatus = InvoiceStatus;
  isApplyingDiscount = false;
  discountAmount = 0;
  showDiscountForm = false;

  get invoice() { return this.data.invoice!; }

  constructor(
    private dialogRef: MatDialogRef<InvoiceViewDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InvoiceDialogData,
    private invoiceService: InvoiceService,
    private snackBar: MatSnackBar,
  ) {}

  close(): void { this.dialogRef.close(); }

  getStatusClass(status: InvoiceStatus): string {
    const map: Record<InvoiceStatus, string> = {
      [InvoiceStatus.Draft]:          'bg-gray-100 text-gray-600',
      [InvoiceStatus.Issued]:         'bg-blue-100 text-blue-700',
      [InvoiceStatus.PartiallyPaid]:  'bg-amber-100 text-amber-700',
      [InvoiceStatus.Paid]:           'bg-green-100 text-green-700',
      [InvoiceStatus.Overdue]:        'bg-red-100 text-red-700',
      [InvoiceStatus.Cancelled]:      'bg-gray-200 text-gray-500',
    };
    return map[status] ?? 'bg-gray-100 text-gray-600';
  }

  applyDiscount(): void {
    if (!this.discountAmount || this.discountAmount <= 0) return;
    this.isApplyingDiscount = true;
    this.invoiceService.applyDiscount(this.invoice.id, { discountAmount: this.discountAmount })
      .pipe(take(1))
      .subscribe({
        next: (res) => {
          this.isApplyingDiscount = false;
          if (res.success) {
            this.snackBar.open('Discount applied.', 'Close', { duration: 2500 });
            // Update local data
            Object.assign(this.data.invoice!, res.data);
            this.showDiscountForm = false;
            this.discountAmount = 0;
          }
        },
        error: () => { this.isApplyingDiscount = false; },
      });
  }
}