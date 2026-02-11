// subscription-error-dialog.component.ts

import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

@Component({
  selector: 'app-subscription-error-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="subscription-error-dialog">
      <!-- Header -->
      <div class="dialog-header" [ngClass]="getHeaderClass()">
        <mat-icon class="dialog-icon">{{ getIcon() }}</mat-icon>
        <h2 class="dialog-title">{{ data.title }}</h2>
      </div>

      <!-- Content -->
      <div class="dialog-content">
        <p class="main-message">{{ data.message }}</p>
        
        <div *ngIf="data.details" class="details-card">
          <div *ngIf="data.details.subscriptionStatus" class="detail-item">
            <span class="detail-label">Status:</span>
            <span class="detail-value">{{ data.details.subscriptionStatus }}</span>
          </div>
          
          <div *ngIf="data.details.expiryDate" class="detail-item">
            <span class="detail-label">Expired On:</span>
            <span class="detail-value">{{ data.details.expiryDate | date:'mediumDate' }}</span>
          </div>
          
          <div *ngIf="data.details.daysRemaining !== undefined && data.details.daysRemaining > 0" class="detail-item">
            <span class="detail-label">Days Remaining:</span>
            <span class="detail-value warning">{{ data.details.daysRemaining }} days</span>
          </div>
        </div>

        <div class="info-box" [ngClass]="data.severity">
          <mat-icon>info</mat-icon>
          <p>{{ getInfoMessage() }}</p>
        </div>
      </div>

      <!-- Actions -->
      <div class="dialog-actions">
        <button mat-stroked-button (click)="close()">
          Cancel
        </button>
        
        <button *ngIf="data.canContactSupport" 
                mat-stroked-button 
                color="accent"
                (click)="contactSupport()">
          <mat-icon>support_agent</mat-icon>
          Contact Support
        </button>
        
        <button *ngIf="data.canRenew" 
                mat-flat-button 
                class="primary-action"
                (click)="manageSubscription()">
          <mat-icon>credit_card</mat-icon>
          {{ data.actionText || 'Renew Subscription' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .subscription-error-dialog {
      min-width: 400px;
      max-width: 600px;
    }

    .dialog-header {
      padding: 1.5rem;
      text-align: center;
      border-radius: 12px 12px 0 0;
      margin: -24px -24px 0 -24px;
      background: linear-gradient(135deg, #ef4444, #dc2626);
      color: white;

      &.warning {
        background: linear-gradient(135deg, #f59e0b, #d97706);
      }

      &.info {
        background: linear-gradient(135deg, #6b7280, #4b5563);
      }
    }

    .dialog-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 0.5rem;
    }

    .dialog-title {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 700;
    }

    .dialog-content {
      padding: 2rem 1.5rem;
    }

    .main-message {
      font-size: 1rem;
      color: #374151;
      margin-bottom: 1.5rem;
      line-height: 1.6;
    }

    .details-card {
      background: #f9fafb;
      border: 2px solid #e5e7eb;
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 1.5rem;
    }

    .detail-item {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      border-bottom: 1px solid #e5e7eb;

      &:last-child {
        border-bottom: none;
      }
    }

    .detail-label {
      font-weight: 600;
      color: #6b7280;
      font-size: 0.875rem;
    }

    .detail-value {
      font-weight: 600;
      color: #1f2937;

      &.warning {
        color: #f59e0b;
      }
    }

    .info-box {
      display: flex;
      gap: 0.75rem;
      padding: 1rem;
      border-radius: 8px;
      font-size: 0.875rem;
      line-height: 1.5;

      &.critical {
        background: #fee2e2;
        border: 1px solid #fecaca;
        color: #991b1b;
      }

      &.warning {
        background: #fef3c7;
        border: 1px solid #fde68a;
        color: #92400e;
      }

      &.info {
        background: #e0e7ff;
        border: 1px solid #c7d2fe;
        color: #3730a3;
      }

      mat-icon {
        flex-shrink: 0;
        font-size: 20px;
        width: 20px;
        height: 20px;
      }

      p {
        margin: 0;
      }
    }

    .dialog-actions {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      padding: 1rem 1.5rem;
      border-top: 2px solid #e5e7eb;
      margin: 0 -24px -24px -24px;
      background: #f9fafb;

      button {
        font-weight: 600;

        mat-icon {
          margin-right: 0.5rem;
          font-size: 18px;
          width: 18px;
          height: 18px;
        }
      }

      .primary-action {
        background: linear-gradient(135deg, #10b981, #059669);
        color: white;
        
        &:hover {
          background: linear-gradient(135deg, #059669, #047857);
        }
      }
    }

    @media (max-width: 600px) {
      .subscription-error-dialog {
        min-width: unset;
      }

      .dialog-actions {
        flex-direction: column;

        button {
          width: 100%;
        }
      }
    }
  `]
})
export class SubscriptionErrorDialogComponent {
  constructor(
    private dialogRef: MatDialogRef<SubscriptionErrorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: SubscriptionErrorDialogData,
    private router: Router
  ) {}

  getHeaderClass(): string {
    switch (this.data.severity) {
      case 'critical': return 'critical';
      case 'warning': return 'warning';
      default: return 'info';
    }
  }

  getIcon(): string {
    switch (this.data.severity) {
      case 'critical': return 'error';
      case 'warning': return 'warning';
      default: return 'info';
    }
  }

  getInfoMessage(): string {
    if (this.data.infoMessage) {
      return this.data.infoMessage;
    }

    switch (this.data.severity) {
      case 'critical':
        return 'Your access has been restricted. Please renew your subscription to continue using the platform.';
      case 'warning':
        return 'Your subscription is expiring soon. Renew now to avoid service interruption.';
      default:
        return 'Please check your subscription status or contact support for assistance.';
    }
  }

  manageSubscription(): void {
    this.dialogRef.close({ action: 'manage' });
    this.router.navigate(['/admin/schools'], {
      queryParams: { action: 'manage-subscription' }
    });
  }

  contactSupport(): void {
    this.dialogRef.close({ action: 'support' });
    this.router.navigate(['/support'], {
      queryParams: { reason: 'subscription-issue' }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}

export interface SubscriptionErrorDialogData {
  title: string;
  message: string;
  severity: 'critical' | 'warning' | 'info';
  details?: {
    subscriptionStatus?: string;
    expiryDate?: string;
    daysRemaining?: number;
  };
  canRenew: boolean;
  canContactSupport: boolean;
  actionText?: string;
  infoMessage?: string;
}