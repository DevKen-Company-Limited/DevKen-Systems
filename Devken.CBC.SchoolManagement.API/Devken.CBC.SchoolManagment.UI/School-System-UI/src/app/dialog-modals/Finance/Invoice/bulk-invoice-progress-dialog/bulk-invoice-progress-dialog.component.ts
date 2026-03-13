import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BulkInvoiceStudentRow } from 'app/Finance/Invoice/Types/bulk-invoice.types';

export interface BulkProgressDialogData {
  results: BulkInvoiceStudentRow[];
}

@Component({
  selector: 'app-bulk-invoice-progress-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './bulk-invoice-progress-dialog.component.html',
})
export class BulkInvoiceProgressDialogComponent {
  constructor(
    private dialogRef: MatDialogRef<BulkInvoiceProgressDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BulkProgressDialogData,
  ) {
    // Prevent closing while in progress
    dialogRef.disableClose = true;
  }

  get doneCount(): number {
    return this.data.results.filter(r => r.status !== 'pending').length;
  }

  get successCount(): number {
    return this.data.results.filter(r => r.status === 'success').length;
  }

  get errorCount(): number {
    return this.data.results.filter(r => r.status === 'error').length;
  }

  get skippedCount(): number {
    return this.data.results.filter(r => r.status === 'skipped').length;
  }

  get progressPct(): number {
    if (!this.data.results.length) return 0;
    return Math.round((this.doneCount / this.data.results.length) * 100);
  }

  get isComplete(): boolean {
    return this.doneCount === this.data.results.length;
  }

  onDone(): void {
    this.dialogRef.close({
      success: this.successCount,
      errors:  this.errorCount,
    });
  }
}