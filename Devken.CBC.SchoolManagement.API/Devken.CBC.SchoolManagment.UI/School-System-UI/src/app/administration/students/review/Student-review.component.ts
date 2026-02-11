// steps/student-review/student-review.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EnrollmentStep } from '../types/EnrollmentStep';


@Component({
  selector: 'app-student-review',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-review.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss', './student-review.component.scss'],
})
export class StudentReviewComponent {
  @Input() formSections: Record<string, any> = {};
  @Input() steps: EnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Output() editSection = new EventEmitter<number>();

  get fullName(): string {
    const p = this.formSections['personal'];
    if (!p) return '—';
    return [p.firstName, p.middleName, p.lastName].filter(Boolean).join(' ') || '—';
  }

  isComplete(index: number): boolean {
    return this.completedSteps.has(index);
  }

  completedCount(): number {
    return Array.from(this.completedSteps).filter(i => i < this.steps.length - 1).length;
  }
  formatDate(value: any): string {
  if (!value) return '—';
  return new Date(value).toLocaleDateString();
}


  allComplete(): boolean {
    return this.completedCount() === this.steps.length - 1;
  }

  getCompletionPct(): number {
    return Math.round((this.completedCount() / (this.steps.length - 1)) * 100);
  }
}