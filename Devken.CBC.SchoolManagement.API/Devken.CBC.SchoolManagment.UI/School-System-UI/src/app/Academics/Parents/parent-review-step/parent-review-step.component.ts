import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import { ParentEnrollmentStep } from '../parent-enrollment/parent-enrollment.component';
import { ParentRelationship } from 'app/Academics/Parents/Types/Parent.types';

@Component({
  selector: 'app-parent-review-step',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    FuseAlertComponent,
  ],
  templateUrl: './parent-review-step.component.html',
})
export class ParentReviewStepComponent {
  @Input() formSections: Record<string, any> = {};
  @Input() steps: ParentEnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Output() editSection = new EventEmitter<number>();

  get basic():      any { return this.formSections['basic']      ?? {}; }
  get contact():    any { return this.formSections['contact']    ?? {}; }
  get identity():   any { return this.formSections['identity']   ?? {}; }
  get employment(): any { return this.formSections['employment'] ?? {}; }
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

  getRelationshipLabel(value: any): string {
    const map: Record<number, string> = {
      [ParentRelationship.Father]:      'Father',
      [ParentRelationship.Mother]:      'Mother',
      [ParentRelationship.Guardian]:    'Guardian',
      [ParentRelationship.Sponsor]:     'Sponsor',
      [ParentRelationship.Grandparent]: 'Grandparent',
      [ParentRelationship.Other]:       'Other',
    };
    return map[Number(value)] ?? '—';
  }
}