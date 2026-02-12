import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, transition, style, animate, state } from '@angular/animations';
import { Subject, takeUntil } from 'rxjs';
import { Alert, AlertService } from 'app/core/DevKenService/Alert/AlertService';

@Component({
  selector: 'app-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alert.component.html',
  styleUrls: ['./alert.component.scss'],
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ transform: 'translateX(100%)', opacity: 0 }),
        animate('300ms ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('250ms ease-in', style({ transform: 'translateX(100%)', opacity: 0 }))
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class AlertComponent implements OnInit, OnDestroy {
  alerts: Alert[] = [];
  private destroy$ = new Subject<void>();

  constructor(private alertService: AlertService) {}

  ngOnInit(): void {
    this.alertService.alerts$
      .pipe(takeUntil(this.destroy$))
      .subscribe(alerts => {
        this.alerts = alerts;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Get the icon class based on alert type
   */
  getIconClass(type: string): string {
    const icons = {
      success: 'heroicons_solid:check-circle',
      error: 'heroicons_solid:x-circle',
      warning: 'heroicons_solid:exclamation-triangle',
      info: 'heroicons_solid:information-circle',
      confirm: 'heroicons_solid:question-mark-circle'
    };
    return icons[type] || icons.info;
  }
get hasConfirmAlert(): boolean {
  return this.alerts?.some(a => a.type === 'confirm') ?? false;
}

  /**
   * Dismiss an alert
   */
  dismiss(id: string): void {
    this.alertService.dismiss(id);
  }

  /**
   * Handle confirm action
   */
  onConfirm(alert: Alert): void {
    if (alert.onConfirm) {
      alert.onConfirm();
    }
    this.dismiss(alert.id);
  }

  /**
   * Handle cancel action
   */
  onCancel(alert: Alert): void {
    if (alert.onCancel) {
      alert.onCancel();
    }
    this.dismiss(alert.id);
  }

  /**
   * Track alerts by ID for performance
   */
  trackByFn(index: number, item: Alert): string {
    return item.id;
  }
}