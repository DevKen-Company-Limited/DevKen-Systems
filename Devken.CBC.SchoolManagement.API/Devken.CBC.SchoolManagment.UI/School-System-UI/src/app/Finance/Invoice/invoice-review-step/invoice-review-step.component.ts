import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { InvoiceEnrollmentStep } from '../invoice-enrollment/invoice-enrollment.component';

@Component({
  selector: 'app-invoice-review-step',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    FuseAlertComponent,
  ],
  templateUrl: './invoice-review-step.component.html',
})
export class InvoiceReviewStepComponent {
  @Input() formSections: Record<string, any> = {};
  @Input() steps: InvoiceEnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Output() editSection = new EventEmitter<number>();

  get details(): any { return this.formSections['details'] ?? {}; }
  get items(): any[] { return this.formSections['items'] ?? []; }
  get notes(): string { return this.formSections['notes'] ?? ''; }

  isComplete(index: number): boolean {
    return this.completedSteps.has(index);
  }

  completedCount(): number {
    return Array.from(this.completedSteps).filter(i => i < this.steps.length - 1).length;
  }

  allComplete(): boolean {
    return this.completedCount() === this.steps.length - 1;
  }

  getCompletionPct(): number {
    return Math.round((this.completedCount() / (this.steps.length - 1)) * 100);
  }

  getItemTotal(item: any): number {
    const subtotal = item.quantity * item.unitPrice - (item.discount || 0);
    if (item.isTaxable && item.taxRate) {
      return subtotal + (subtotal * item.taxRate / 100);
    }
    return subtotal;
  }

  get grandTotal(): number {
    return this.items.reduce((sum, item) => sum + this.getItemTotal(item), 0);
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES', maximumFractionDigits: 0 }).format(val);
  }

  formatDate(val: string | Date): string {
    if (!val) return '—';
    const d = new Date(val);
    return isNaN(d.getTime()) ? '—' : d.toLocaleDateString();
  }
}