import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { EnrollmentStep } from '../../types/EnrollmentStep';


@Component({
  selector: 'app-student-review',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    FuseAlertComponent,
  ],
  templateUrl: './student-review.component.html',
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