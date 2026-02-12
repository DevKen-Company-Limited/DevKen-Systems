import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Alert {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info' | 'confirm';
  title?: string;
  message: string;
  duration?: number;
  dismissible?: boolean;
  onConfirm?: () => void;
  onCancel?: () => void;
  confirmText?: string;
  cancelText?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  private alertsSubject = new BehaviorSubject<Alert[]>([]);
  public alerts$: Observable<Alert[]> = this.alertsSubject.asObservable();

  constructor() {}

  /**
   * Show a success alert
   */
  success(message: string, title?: string, duration: number = 5000): void {
    this.show({
      type: 'success',
      message,
      title: title || 'Success',
      duration,
      dismissible: true
    });
  }

  /**
   * Show an error alert
   */
  error(message: string, title?: string, duration: number = 7000): void {
    this.show({
      type: 'error',
      message,
      title: title || 'Error',
      duration,
      dismissible: true
    });
  }

  /**
   * Show a warning alert
   */
  warning(message: string, title?: string, duration: number = 6000): void {
    this.show({
      type: 'warning',
      message,
      title: title || 'Warning',
      duration,
      dismissible: true
    });
  }

  /**
   * Show an info alert
   */
  info(message: string, title?: string, duration: number = 5000): void {
    this.show({
      type: 'info',
      message,
      title: title || 'Information',
      duration,
      dismissible: true
    });
  }

  /**
   * Show a confirmation dialog
   */
  confirm(
    message: string,
    onConfirm: () => void,
    onCancel?: () => void,
    title?: string,
    confirmText: string = 'Confirm',
    cancelText: string = 'Cancel'
  ): void {
    this.show({
      type: 'confirm',
      message,
      title: title || 'Confirm Action',
      onConfirm,
      onCancel,
      confirmText,
      cancelText,
      dismissible: false
    });
  }

  /**
   * Show a custom alert
   */
  private show(alertConfig: Omit<Alert, 'id'>): void {
    const alert: Alert = {
      ...alertConfig,
      id: this.generateId()
    };

    const currentAlerts = this.alertsSubject.value;
    this.alertsSubject.next([...currentAlerts, alert]);

    // Auto-dismiss if duration is set
    if (alert.duration && alert.duration > 0) {
      setTimeout(() => {
        this.dismiss(alert.id);
      }, alert.duration);
    }
  }

  /**
   * Dismiss an alert by ID
   */
  dismiss(id: string): void {
    const currentAlerts = this.alertsSubject.value;
    this.alertsSubject.next(currentAlerts.filter(alert => alert.id !== id));
  }

  /**
   * Dismiss all alerts
   */
  dismissAll(): void {
    this.alertsSubject.next([]);
  }

  /**
   * Generate a unique ID for alerts
   */
  private generateId(): string {
    return `alert-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}