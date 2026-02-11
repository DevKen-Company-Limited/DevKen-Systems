import { inject } from '@angular/core';
import {
  HttpInterceptorFn,
  HttpRequest,
  HttpErrorResponse
} from '@angular/common/http';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import {
  SubscriptionErrorDialogComponent,
  SubscriptionErrorDialogData
} from 'app/dialog-modals/Tenant/subscription-error-dialog.component';

/**
 * Enhanced functional interceptor that handles subscription-related HTTP errors
 * (Angular standalone / provideHttpClient compatible)
 */

// ------------------------------------------------------
// Module-level state (shared across requests)
// ------------------------------------------------------
let lastNotificationTime = 0;
const NOTIFICATION_THROTTLE_MS = 5000;
const handledErrors = new Set<string>();
let dialogOpen = false;

// ------------------------------------------------------
// Interceptor
// ------------------------------------------------------
export const subscriptionStatusInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);
  const dialog = inject(MatDialog);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (isSubscriptionError(error)) {
        handleSubscriptionError(error, req, router, snackBar, dialog);
      }

      return throwError(() => error);
    })
  );
};

// ------------------------------------------------------
// Helpers
// ------------------------------------------------------
function isSubscriptionError(error: HttpErrorResponse): boolean {
  if (error.status !== 402 && error.status !== 403) {
    return false;
  }

  const errorMessage = error.error?.message?.toLowerCase() || '';
  const errorCode = error.error?.code?.toLowerCase() || '';

  return (
    errorMessage.includes('subscription') ||
    errorMessage.includes('expired') ||
    errorMessage.includes('inactive') ||
    errorMessage.includes('payment') ||
    errorCode === 'subscription_expired' ||
    errorCode === 'subscription_inactive' ||
    errorCode === 'subscription_required' ||
    errorCode === 'payment_required'
  );
}

function handleSubscriptionError(
  error: HttpErrorResponse,
  request: HttpRequest<any>,
  router: Router,
  snackBar: MatSnackBar,
  dialog: MatDialog
): void {
  const now = Date.now();
  const errorKey = `${error.status}-${error.error?.code || 'subscription'}`;

  // Throttle notifications
  if (now - lastNotificationTime < NOTIFICATION_THROTTLE_MS) {
    return;
  }

  // Prevent duplicate handling
  if (handledErrors.has(errorKey)) {
    return;
  }

  lastNotificationTime = now;
  handledErrors.add(errorKey);

  setTimeout(() => handledErrors.delete(errorKey), NOTIFICATION_THROTTLE_MS);

  const errorData = parseErrorData(error);
  const shouldUseDialog = shouldShowDialog(errorData, request);

  if (shouldUseDialog) {
    showErrorDialog(errorData, dialog);
  } else {
    showSnackbar(errorData, router, snackBar);
  }
}

function parseErrorData(error: HttpErrorResponse): SubscriptionErrorData {
  const body = error.error || {};

  return {
    status: error.status,
    message: body.message || 'Subscription issue detected',
    code: body.code || 'SUBSCRIPTION_ERROR',
    subscriptionStatus: body.subscriptionStatus,
    daysRemaining: body.daysRemaining,
    expiryDate: body.expiryDate,
    canRenew: body.canRenew !== false,
    severity: getErrorSeverity(error.status, body)
  };
}

function getErrorSeverity(
  status: number,
  body: any
): 'critical' | 'warning' | 'info' {
  if (status === 402) return 'critical';
  if (body.daysRemaining !== undefined && body.daysRemaining <= 0) return 'critical';
  if (body.daysRemaining !== undefined && body.daysRemaining <= 7) return 'warning';
  return 'info';
}

function shouldShowDialog(
  errorData: SubscriptionErrorData,
  request: HttpRequest<any>
): boolean {
  if (errorData.severity === 'critical') return true;

  if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(request.method)) {
    return true;
  }

  return false;
}

function showErrorDialog(
  errorData: SubscriptionErrorData,
  dialog: MatDialog
): void {
  if (dialogOpen) return;

  dialogOpen = true;

  const dialogData: SubscriptionErrorDialogData = {
    title: getDialogTitle(errorData),
    message: errorData.message,
    severity: errorData.severity,
    details: {
      subscriptionStatus: errorData.subscriptionStatus,
      expiryDate: errorData.expiryDate,
      daysRemaining: errorData.daysRemaining
    },
    canRenew: errorData.canRenew,
    canContactSupport: true,
    actionText: errorData.status === 402 ? 'Renew Now' : 'Upgrade Plan'
  };

  const dialogRef = dialog.open(SubscriptionErrorDialogComponent, {
    width: '90vw',
    maxWidth: '600px',
    disableClose: errorData.severity === 'critical',
    panelClass: 'subscription-error-dialog-container',
    data: dialogData
  });

  dialogRef.afterClosed().subscribe(() => {
    dialogOpen = false;
  });
}

function showSnackbar(
  errorData: SubscriptionErrorData,
  router: Router,
  snackBar: MatSnackBar
): void {
  const message = errorData.message || 'Subscription verification failed';
  const action = errorData.canRenew ? 'Manage' : 'Learn More';

  const snackBarRef = snackBar.open(message, action, {
    duration: errorData.severity === 'critical' ? 10000 : 7000,
    horizontalPosition: 'end',
    verticalPosition: 'top',
    panelClass: [getSnackbarClass(errorData.severity)]
  });

  snackBarRef.onAction().subscribe(() => {
    if (errorData.canRenew) {
      router.navigate(['/admin/schools'], {
        queryParams: { action: 'manage-subscription' }
      });
    } else {
      router.navigate(['/support'], {
        queryParams: { reason: 'subscription-issue' }
      });
    }
  });
}

function getDialogTitle(errorData: SubscriptionErrorData): string {
  switch (errorData.severity) {
    case 'critical':
      return errorData.status === 402
        ? 'Subscription Expired'
        : 'Access Restricted';
    case 'warning':
      return 'Subscription Expiring Soon';
    default:
      return 'Subscription Notice';
  }
}

function getSnackbarClass(
  severity: 'critical' | 'warning' | 'info'
): string {
  switch (severity) {
    case 'critical':
      return 'subscription-critical-snackbar';
    case 'warning':
      return 'subscription-warning-snackbar';
    default:
      return 'subscription-info-snackbar';
  }
}

// ------------------------------------------------------
// Types
// ------------------------------------------------------
interface SubscriptionErrorData {
  status: number;
  message: string;
  code: string;
  subscriptionStatus?: string;
  daysRemaining?: number;
  expiryDate?: string;
  canRenew: boolean;
  severity: 'critical' | 'warning' | 'info';
}
