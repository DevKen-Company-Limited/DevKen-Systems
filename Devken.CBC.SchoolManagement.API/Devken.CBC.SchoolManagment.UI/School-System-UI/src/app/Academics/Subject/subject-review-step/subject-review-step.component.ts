// subject-review-step/subject-review-step.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { SubjectEnrollmentStep } from '../subject-enrollment/subject-enrollment.component';
import { getCBCLevelLabel, getSubjectTypeLabel } from '../Types/SubjectEnums';

@Component({
  selector: 'app-subject-review-step',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    FuseAlertComponent,
  ],
  templateUrl: './subject-review-step.component.html',
})
export class SubjectReviewStepComponent {

  @Input() formSections: Record<string, any> = {};
  @Input() steps: SubjectEnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Input() isSuperAdmin = false;
  @Output() editSection = new EventEmitter<number>();

  getSubjectTypeName  = getSubjectTypeLabel;
  getCBCLevelName     = getCBCLevelLabel;

  get identity():   any { return this.formSections['identity']   ?? {}; }
  get curriculum(): any { return this.formSections['curriculum'] ?? {}; }
  get settings():   any { return this.formSections['settings']   ?? {}; }

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
}