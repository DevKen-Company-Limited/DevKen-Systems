import { Component, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';
import { MpesaCallbackStatus, MpesaPaymentService } from 'app/core/DevKenService/payments/MpesaPaymentService';

export interface MpesaPaymentDialogData {
  amount: number;
  accountReference: string;
  description: string;
  phoneNumber?: string;
}

@Component({
  selector: 'app-mpesa-payment-dialog',
  standalone: true,
  templateUrl: './mpesa-payment-dialog.component.html',
  styleUrls: ['./mpesa-payment-dialog.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ]
})
export class MpesaPaymentDialogComponent implements OnDestroy {
  phoneNumber: string = '';
  isProcessing = false;
  paymentStatus: 'idle' | 'initiated' | 'polling' | 'success' | 'failed' | 'cancelled' = 'idle';
  statusMessage = '';
  checkoutRequestId?: string;
  
  private pollingSub?: Subscription;

  constructor(
    private dialogRef: MatDialogRef<MpesaPaymentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: MpesaPaymentDialogData,
    private mpesaService: MpesaPaymentService,
    private snackBar: MatSnackBar
  ) {
    // Pre-fill phone number if provided
    if (data.phoneNumber) {
      this.phoneNumber = data.phoneNumber;
    }
  }

  ngOnDestroy(): void {
    // Clean up subscription
    if (this.pollingSub) {
      this.pollingSub.unsubscribe();
    }
  }

  /**
   * Initiate M-Pesa payment
   */
  initiatePayment(): void {
    // Validate phone number
    if (!this.mpesaService.isValidPhoneNumber(this.phoneNumber)) {
      this.showError('Please enter a valid Kenyan phone number (e.g., 0712345678)');
      return;
    }

    this.isProcessing = true;
    this.paymentStatus = 'initiated';
    this.statusMessage = 'Initiating payment...';

    const formattedPhone = this.mpesaService.formatPhoneNumber(this.phoneNumber);

    this.mpesaService.initiatePayment({
      phoneNumber: formattedPhone,
      amount: this.data.amount,
      accountReference: this.data.accountReference,
      transactionDesc: this.data.description
    }).subscribe({
      next: response => {
        if (response.success && response.checkoutRequestId) {
          this.checkoutRequestId = response.checkoutRequestId;
          this.paymentStatus = 'polling';
          this.statusMessage = response.customerMessage || 
            'Please check your phone and enter your M-Pesa PIN to complete the payment...';
          
          // Start polling for payment status
          this.startPolling(response.checkoutRequestId);
        } else {
          this.paymentStatus = 'failed';
          this.statusMessage = response.message || 'Failed to initiate payment';
          this.isProcessing = false;
          this.showError(this.statusMessage);
        }
      },
      error: error => {
        console.error('Payment initiation error:', error);
        this.paymentStatus = 'failed';
        this.statusMessage = 'Failed to initiate payment. Please try again.';
        this.isProcessing = false;
        this.showError(this.statusMessage);
      }
    });
  }

  /**
   * Start polling for payment status
   */
  private startPolling(checkoutRequestId: string): void {
    this.pollingSub = this.mpesaService.pollPaymentStatus(checkoutRequestId).subscribe({
      next: (status: MpesaCallbackStatus) => {
        // Check if payment is complete (not pending)
        if (status.resultCode !== -1 && status.resultCode !== undefined) {
          this.handlePaymentComplete(status);
        }
      },
      error: error => {
        console.error('Polling error:', error);
        this.paymentStatus = 'failed';
        this.statusMessage = 'Failed to verify payment status';
        this.isProcessing = false;
        this.showError(this.statusMessage);
      },
      complete: () => {
        // If polling completes without a definitive status, it timed out
        if (this.paymentStatus === 'polling') {
          this.paymentStatus = 'failed';
          this.statusMessage = 'Payment verification timed out. Please check your M-Pesa messages.';
          this.isProcessing = false;
          this.showError(this.statusMessage);
        }
      }
    });
  }

  /**
   * Handle payment completion
   */
  private handlePaymentComplete(status: MpesaCallbackStatus): void {
    this.isProcessing = false;

    if (status.resultCode === 0) {
      // Payment successful
      this.paymentStatus = 'success';
      this.statusMessage = `Payment successful! Receipt: ${status.mpesaReceiptNumber || 'N/A'}`;
      this.showSuccess(this.statusMessage);
      
      // Close dialog with success result
      setTimeout(() => {
        this.dialogRef.close({
          success: true,
          mpesaReceiptNumber: status.mpesaReceiptNumber,
          transactionDate: status.transactionDate,
          amount: status.amount
        });
      }, 2000);
    } else if (status.resultCode === 1032) {
      // User cancelled
      this.paymentStatus = 'cancelled';
      this.statusMessage = 'Payment cancelled by user';
      this.showError(this.statusMessage);
    } else {
      // Payment failed
      this.paymentStatus = 'failed';
      this.statusMessage = this.mpesaService.getResultDescription(status.resultCode);
      this.showError(this.statusMessage);
    }
  }

  /**
   * Cancel payment
   */
  cancel(): void {
    // Clean up polling if active
    if (this.pollingSub) {
      this.pollingSub.unsubscribe();
    }

    this.dialogRef.close({ success: false, cancelled: true });
  }

  /**
   * Retry payment
   */
  retry(): void {
    this.paymentStatus = 'idle';
    this.statusMessage = '';
    this.checkoutRequestId = undefined;
    this.isProcessing = false;
    
    // Clean up previous polling
    if (this.pollingSub) {
      this.pollingSub.unsubscribe();
    }
  }

  /**
   * Get status icon
   */
  getStatusIcon(): string {
    switch (this.paymentStatus) {
      case 'success':
        return 'check_circle';
      case 'failed':
      case 'cancelled':
        return 'error';
      case 'polling':
      case 'initiated':
        return 'pending';
      default:
        return 'payment';
    }
  }

  /**
   * Get status color
   */
  getStatusColor(): string {
    switch (this.paymentStatus) {
      case 'success':
        return 'text-green-600';
      case 'failed':
      case 'cancelled':
        return 'text-red-600';
      case 'polling':
      case 'initiated':
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['alert', 'alert-success'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 7000,
      panelClass: ['alert', 'alert-danger'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }
}