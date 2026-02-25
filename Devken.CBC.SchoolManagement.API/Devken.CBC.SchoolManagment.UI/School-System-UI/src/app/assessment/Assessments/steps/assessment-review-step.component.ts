// ═══════════════════════════════════════════════════════════════════
// steps/assessment-review-step.component.ts
// Shows a full summary of ALL entered fields before submission
// ═══════════════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-assessment-review-step',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Review & Save</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Confirm all details before saving.</p>
  </div>

  <div class="space-y-4">

    <!-- ── Basic Information Card ─────────────────────────────── -->
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-full bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center">
            <mat-icon class="text-indigo-600 icon-size-4">info</mat-icon>
          </div>
          <h3 class="font-semibold text-gray-900 dark:text-white">Basic Information</h3>
        </div>
        <button mat-stroked-button (click)="editSection.emit(0)" class="text-sm">Edit</button>
      </div>
      <div class="p-6 grid grid-cols-2 gap-x-6 gap-y-4">
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Title</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ info.title || '—' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Date</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ info.assessmentDate | date:'mediumDate' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Class</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ getClassName() || '—' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Subject</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ getSubjectName() || '—' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Teacher</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ getTeacherName() || '—' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Term</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ getTermName() || '—' }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 mb-0.5">Academic Year</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ getAcademicYearName() || '—' }}</p>
        </div>
        <div *ngIf="info.description" class="col-span-2">
          <p class="text-xs text-gray-400 mb-0.5">Description</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ info.description }}</p>
        </div>
      </div>
    </div>

    <!-- ── Assessment Details Card ────────────────────────────── -->
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-full flex items-center justify-center"
            [ngClass]="{
              'bg-indigo-100 dark:bg-indigo-900/30': details.assessmentType === 'Formative',
              'bg-violet-100 dark:bg-violet-900/30': details.assessmentType === 'Summative',
              'bg-teal-100   dark:bg-teal-900/30':   details.assessmentType === 'Competency'
            }">
            <mat-icon class="icon-size-4"
              [ngClass]="{
                'text-indigo-600': details.assessmentType === 'Formative',
                'text-violet-600': details.assessmentType === 'Summative',
                'text-teal-600':   details.assessmentType === 'Competency'
              }">tune</mat-icon>
          </div>
          <h3 class="font-semibold text-gray-900 dark:text-white">Assessment Details</h3>
        </div>
        <button mat-stroked-button (click)="editSection.emit(1)" class="text-sm">Edit</button>
      </div>
      <div class="p-6">
        <!-- Shared fields -->
        <div class="grid grid-cols-2 gap-x-6 gap-y-4 mb-4">
          <div>
            <p class="text-xs text-gray-400 mb-0.5">Type</p>
            <span class="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-bold border"
              [ngClass]="{
                'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-900/30 dark:text-indigo-300': details.assessmentType === 'Formative',
                'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-900/30 dark:text-violet-300': details.assessmentType === 'Summative',
                'bg-teal-50   text-teal-700   border-teal-200   dark:bg-teal-900/30   dark:text-teal-300':   details.assessmentType === 'Competency'
              }">
              {{ details.assessmentType || '—' }}
            </span>
          </div>
          <div>
            <p class="text-xs text-gray-400 mb-0.5">Maximum Score</p>
            <p class="font-bold text-gray-900 dark:text-white text-xl">{{ details.maximumScore || '—' }}</p>
          </div>
        </div>

        <!-- Formative fields summary -->
        <ng-container *ngIf="details.assessmentType === 'Formative'">
          <div class="border-t border-gray-100 dark:border-gray-700 pt-4 grid grid-cols-2 gap-x-6 gap-y-3">
            <div *ngIf="details.formativeType">
              <p class="text-xs text-gray-400 mb-0.5">Formative Type</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.formativeType }}</p>
            </div>
            <div *ngIf="details.assessmentWeight">
              <p class="text-xs text-gray-400 mb-0.5">Weight (%)</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.assessmentWeight }}%</p>
            </div>
            <div *ngIf="details.competencyArea">
              <p class="text-xs text-gray-400 mb-0.5">Competency Area</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyArea }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-400 mb-0.5">Requires Rubric</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.requiresRubric ? 'Yes' : 'No' }}</p>
            </div>
            <div *ngIf="details.criteria" class="col-span-2">
              <p class="text-xs text-gray-400 mb-0.5">Criteria</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.criteria }}</p>
            </div>
          </div>
        </ng-container>

        <!-- Summative fields summary -->
        <ng-container *ngIf="details.assessmentType === 'Summative'">
          <div class="border-t border-gray-100 dark:border-gray-700 pt-4 grid grid-cols-2 gap-x-6 gap-y-3">
            <div *ngIf="details.examType">
              <p class="text-xs text-gray-400 mb-0.5">Exam Type</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.examType }}</p>
            </div>
            <div *ngIf="details.passMark">
              <p class="text-xs text-gray-400 mb-0.5">Pass Mark</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.passMark }}%</p>
            </div>
            <div *ngIf="details.duration">
              <p class="text-xs text-gray-400 mb-0.5">Duration</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.duration }}</p>
            </div>
            <div *ngIf="details.numberOfQuestions">
              <p class="text-xs text-gray-400 mb-0.5">Questions</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.numberOfQuestions }}</p>
            </div>
            <div *ngIf="details.theoryWeight">
              <p class="text-xs text-gray-400 mb-0.5">Theory Weight</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.theoryWeight }}%</p>
            </div>
            <div *ngIf="details.hasPracticalComponent">
              <p class="text-xs text-gray-400 mb-0.5">Practical Weight</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.practicalWeight || 0 }}%</p>
            </div>
          </div>
        </ng-container>

        <!-- Competency fields summary -->
        <ng-container *ngIf="details.assessmentType === 'Competency'">
          <div class="border-t border-gray-100 dark:border-gray-700 pt-4 grid grid-cols-2 gap-x-6 gap-y-3">
            <div class="col-span-2" *ngIf="details.competencyName">
              <p class="text-xs text-gray-400 mb-0.5">Competency Name</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyName }}</p>
            </div>
            <div *ngIf="details.competencyStrand">
              <p class="text-xs text-gray-400 mb-0.5">Strand</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyStrand }}</p>
            </div>
            <div *ngIf="details.competencySubStrand">
              <p class="text-xs text-gray-400 mb-0.5">Sub-Strand</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencySubStrand }}</p>
            </div>
            <div *ngIf="details.ratingScale">
              <p class="text-xs text-gray-400 mb-0.5">Rating Scale</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.ratingScale }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-400 mb-0.5">Observation Based</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.isObservationBased ? 'Yes' : 'No' }}</p>
            </div>
          </div>
        </ng-container>
      </div>
    </div>

    <!-- ── Validation / Ready Indicator ──────────────────────── -->
    <div *ngIf="allValid()" class="flex items-center gap-3 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl">
      <mat-icon class="text-green-600 flex-shrink-0">check_circle</mat-icon>
      <p class="text-sm text-green-700 dark:text-green-400 font-medium">All required fields are filled. Ready to save.</p>
    </div>
    <div *ngIf="!allValid()" class="flex items-center gap-3 p-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl">
      <mat-icon class="text-amber-600 flex-shrink-0">warning</mat-icon>
      <p class="text-sm text-amber-700 dark:text-amber-400 font-medium">
        Some required fields are missing. Please go back and complete all required fields.
      </p>
    </div>

  </div>
</div>
  `,
})
export class AssessmentReviewStepComponent {
  @Input() formSections:    Record<string, any> = {};
  @Input() classes:         any[] = [];
  @Input() teachers:        any[] = [];
  @Input() subjects:        any[] = [];
  @Input() terms:           any[] = [];
  @Input() academicYears:   any[] = [];
  @Input() steps:           any[] = [];
  @Input() completedSteps!: Set<number>;

  @Output() editSection = new EventEmitter<number>();

  get info():    any { return this.formSections['info']    ?? {}; }
  get details(): any { return this.formSections['details'] ?? {}; }

  getClassName():       string { return this.classes.find(c  => c.id === this.info.classId)?.name       ?? ''; }
  getSubjectName():     string { return this.subjects.find(s => s.id === this.info.subjectId)?.name     ?? ''; }
  getTeacherName():     string { const t = this.teachers.find(t => t.id === this.info.teacherId); return t ? `${t.firstName} ${t.lastName}` : ''; }
  getTermName():        string { return this.terms.find(t => t.id === this.info.termId)?.name           ?? ''; }
  getAcademicYearName(): string { return this.academicYears.find(y => y.id === this.info.academicYearId)?.name ?? ''; }

  allValid(): boolean {
    const infoOk    = !!(this.info.title?.trim() && this.info.assessmentDate);
    const detailsOk = !!(this.details.assessmentType && this.details.maximumScore);
    const compOk    = this.details.assessmentType !== 'Competency' || !!this.details.competencyName;
    return infoOk && detailsOk && compOk;
  }
}