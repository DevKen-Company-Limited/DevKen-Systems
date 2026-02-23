// assessment-review-step/assessment-review-step.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { AssessmentType, getAssessmentTypeLabel, getAssessmentTypeColor, getAssessmentMethodLabel, AssessmentTypeOptions } from '../types/AssessmentDtos';


export interface AssessmentEnrollmentStep {
  label: string;
  icon: string;
  sectionKey: string;
}

@Component({
  selector: 'app-assessment-review-step',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, FuseAlertComponent],
  templateUrl: './assessment-review-step.component.html',
})
export class AssessmentReviewStepComponent {

  @Input() formSections: Record<string, any>  = {};
  @Input() steps: AssessmentEnrollmentStep[]  = [];
  @Input() completedSteps = new Set<number>();
  @Input() isSuperAdmin = false;
  @Output() editSection = new EventEmitter<number>();

  readonly AssessmentType = AssessmentType;

  getTypeName   = getAssessmentTypeLabel;
  getTypeColor  = getAssessmentTypeColor;
  getMethodName = getAssessmentMethodLabel;

  get typeStep():     any { return this.formSections['type']     ?? {}; }
  get identity():     any { return this.formSections['identity'] ?? {}; }
  get details():      any { return this.formSections['details']  ?? {}; }

  get assessmentType(): AssessmentType {
    return this.typeStep.assessmentType ?? AssessmentType.Formative;
  }

  get typeOption() {
    return AssessmentTypeOptions.find(o => o.value === this.assessmentType);
  }

  isComplete(index: number): boolean { return this.completedSteps.has(index); }

  completedCount(): number {
    return Array.from(this.completedSteps).filter(i => i < this.steps.length - 1).length;
  }

  allComplete(): boolean { return this.completedCount() === this.steps.length - 1; }

  getCompletionPct(): number {
    return Math.round((this.completedCount() / (this.steps.length - 1)) * 100);
  }

  formatDate(val: any): string {
    if (!val) return '—';
    try {
      const d = typeof val === 'string' ? new Date(val) : val;
      return isNaN(d.getTime()) ? '—' :
        d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
    } catch { return '—'; }
  }

  field(obj: any, key: string, fallback = '—'): string {
    const v = obj?.[key];
    return v !== null && v !== undefined && v !== '' ? String(v) : fallback;
  }
}