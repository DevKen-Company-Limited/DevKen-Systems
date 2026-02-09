// subscription-status.interceptor.ts

import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable()
export class SubscriptionStatusInterceptor implements HttpInterceptor {
  constructor(
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // Check if error is subscription-related
        if (error.status === 402 || error.status === 403) {
          const errorMessage = error.error?.message || 'Subscription required';
          
          // Show subscription alert
          this.snackBar.open(errorMessage, 'Manage Subscription', {
            duration: 10000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['warn-snackbar']
          }).onAction().subscribe(() => {
            // Navigate to subscription management
            this.router.navigate(['/admin/subscription']);
          });
          
          // Optionally redirect to subscription page
          if (error.status === 402) {
            // 402 Payment Required - Subscription expired
            this.router.navigate(['/subscription/expired']);
          }
        }
        
        return throwError(() => error);
      })
    );
  }
}