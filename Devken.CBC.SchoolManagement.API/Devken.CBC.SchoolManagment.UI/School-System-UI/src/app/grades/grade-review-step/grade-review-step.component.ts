// grade-review-step/grade-review-step.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule }   from '@angular/common';
import { MatCardModule }  from '@angular/material/card';
import { MatIconModule }  from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { GradeEnrollmentStep } from '../grade-enrollment/grade-enrollment.component';
import { getGradeLetterLabel, getGradeTypeLabel } from '../types/GradeEnums';

@Component({
  selector: 'app-grade-review-step',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, FuseAlertComponent],
  templateUrl: './grade-review-step.component.html',
})
export class GradeReviewStepComponent {
  @Input() formSections:  Record<string, any> = {};
  @Input() steps:         GradeEnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Input() isSuperAdmin   = false;
  @Output() editSection   = new EventEmitter<number>();

  getGradeLetterName = getGradeLetterLabel;
  getGradeTypeName   = getGradeTypeLabel;

  get subject():  any { return this.formSections['subject']  ?? {}; }
  get score():    any { return this.formSections['score']    ?? {}; }
  get settings(): any { return this.formSections['settings'] ?? {}; }

  isComplete(index: number): boolean   { return this.completedSteps.has(index); }
  completedCount(): number {
    return Array.from(this.completedSteps).filter(i => i < this.steps.length - 1).length;
  }
  allComplete(): boolean   { return this.completedCount() === this.steps.length - 1; }
  getCompletionPct(): number {
    return Math.round((this.completedCount() / (this.steps.length - 1)) * 100);
  }

  get computedPercentage(): number | null {
    const s = Number(this.score.score);
    const m = Number(this.score.maximumScore);
    if (!s || !m || m === 0) return null;
    return Math.round((s / m) * 10000) / 100;
  }
}