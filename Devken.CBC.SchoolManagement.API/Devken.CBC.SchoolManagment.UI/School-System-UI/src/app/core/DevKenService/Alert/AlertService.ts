import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { v4 as uuidv4 } from 'uuid';

export interface Alert {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info' | 'confirm';
  title?: string;
  message: string;
  dismissible?: boolean;
  confirmText?: string;
  cancelText?: string;
  onConfirm?: () => void;
  onCancel?: () => void;
}

@Injectable({ providedIn: 'root' })
export class AlertService {

  private _alerts = new BehaviorSubject<Alert[]>([]);
  alerts$ = this._alerts.asObservable();

  private get alerts(): Alert[] {
    return this._alerts.value;
  }

  private set alerts(val: Alert[]) {
    this._alerts.next(val);
  }

  private add(alert: Alert): void {
    this.alerts = [...this.alerts, alert];
  }

  dismiss(id: string): void {
    this.alerts = this.alerts.filter(a => a.id !== id);
  }

  // 🔥 SIMPLE METHODS

  success(message: string, title?: string) {
    this.add({
      id: uuidv4(),
      type: 'success',
      message,
      title,
      dismissible: true
    });
  }

  error(message: string, title?: string) {
    this.add({
      id: uuidv4(),
      type: 'error',
      message,
      title,
      dismissible: true
    });
  }

  warning(message: string, title?: string) {
    this.add({
      id: uuidv4(),
      type: 'warning',
      message,
      title,
      dismissible: true
    });
  }

  info(message: string, title?: string) {
    this.add({
      id: uuidv4(),
      type: 'info',
      message,
      title,
      dismissible: true
    });
  }

  confirm(options: {
    message: string;
    title?: string;
    confirmText?: string;
    cancelText?: string;
    onConfirm: () => void;
    onCancel?: () => void;
  }) {
    this.add({
      id: uuidv4(),
      type: 'confirm',
      message: options.message,
      title: options.title,
      confirmText: options.confirmText ?? 'Confirm',
      cancelText: options.cancelText ?? 'Cancel',
      onConfirm: options.onConfirm,
      onCancel: options.onCancel,
      dismissible: false
    });
  }
}