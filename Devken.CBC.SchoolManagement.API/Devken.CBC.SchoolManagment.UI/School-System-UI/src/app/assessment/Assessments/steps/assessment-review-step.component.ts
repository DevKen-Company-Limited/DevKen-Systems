
// ═══════════════════════════════════════════════════════════════════
// steps/assessment-review-step.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component as ReviewComp, Input as ReviewIn, Output as ReviewOut, EventEmitter as ReviewEE } from '@angular/core';
import { CommonModule as ReviewCM } from '@angular/common';
import { MatIconModule as ReviewMI } from '@angular/material/icon';
import { MatButtonModule as ReviewMB } from '@angular/material/button';

@ReviewComp({
  selector: 'app-assessment-review-step',
  standalone: true,
  imports: [ReviewCM, ReviewMI, ReviewMB],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Review & Save</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Confirm all details before saving.</p>
  </div>

  <div class="space-y-4">

    <!-- Info card -->
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
      <div class="p-6 grid grid-cols-2 gap-4">
        <div><p class="text-xs text-gray-400">Title</p><p class="font-medium text-gray-900 dark:text-white">{{ info.title || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Date</p><p class="font-medium text-gray-900 dark:text-white">{{ info.assessmentDate | date:'mediumDate' }}</p></div>
        <div><p class="text-xs text-gray-400">Class</p><p class="font-medium text-gray-900 dark:text-white">{{ getClassName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Subject</p><p class="font-medium text-gray-900 dark:text-white">{{ getSubjectName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Teacher</p><p class="font-medium text-gray-900 dark:text-white">{{ getTeacherName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Term</p><p class="font-medium text-gray-900 dark:text-white">{{ getTermName() || '—' }}</p></div>
        <div *ngIf="info.description" class="col-span-2">
          <p class="text-xs text-gray-400">Description</p>
          <p class="font-medium text-gray-900 dark:text-white">{{ info.description }}</p>
        </div>
      </div>
    </div>

    <!-- Details card -->
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-full bg-violet-100 dark:bg-violet-900/30 flex items-center justify-center">
            <mat-icon class="text-violet-600 icon-size-4">tune</mat-icon>
          </div>
          <h3 class="font-semibold text-gray-900 dark:text-white">Assessment Details</h3>
        </div>
        <button mat-stroked-button (click)="editSection.emit(1)" class="text-sm">Edit</button>
      </div>
      <div class="p-6 grid grid-cols-2 gap-4">
        <div>
          <p class="text-xs text-gray-400">Type</p>
          <span class="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-bold border mt-1"
            [ngClass]="{
              'bg-indigo-50 text-indigo-700 border-indigo-200': details.assessmentType === 'Formative',
              'bg-violet-50 text-violet-700 border-violet-200': details.assessmentType === 'Summative',
              'bg-teal-50   text-teal-700   border-teal-200':   details.assessmentType === 'Competency'
            }">
            {{ details.assessmentType || '—' }}
          </span>
        </div>
        <div><p class="text-xs text-gray-400">Maximum Score</p><p class="font-medium text-gray-900 dark:text-white text-xl">{{ details.maximumScore || '—' }}</p></div>
      </div>
    </div>

    <!-- Ready indicator -->
    <div class="flex items-center gap-3 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl">
      <mat-icon class="text-green-600">check_circle</mat-icon>
      <p class="text-sm text-green-700 dark:text-green-400 font-medium">All required fields are filled. Ready to save.</p>
    </div>

  </div>
</div>
  `,
})
export class AssessmentReviewStepComponent {
  @ReviewIn() formSections:  Record<string, any> = {};
  @ReviewIn() classes:       any[] = [];
  @ReviewIn() teachers:      any[] = [];
  @ReviewIn() subjects:      any[] = [];
  @ReviewIn() terms:         any[] = [];
  @ReviewIn() academicYears: any[] = [];
  @ReviewIn() steps:         any[] = [];
  @ReviewIn() completedSteps!: Set<number>;

  @ReviewOut() editSection = new ReviewEE<number>();

  get info():    any { return this.formSections['info']    ?? {}; }
  get details(): any { return this.formSections['details'] ?? {}; }

  getClassName():   string { return this.classes.find(c  => c.id  === this.info.classId)?.name ?? ''; }
  getSubjectName(): string { return this.subjects.find(s  => s.id  === this.info.subjectId)?.name ?? ''; }
  getTeacherName(): string { const t = this.teachers.find(t => t.id === this.info.teacherId); return t ? `${t.firstName} ${t.lastName}` : ''; }
  getTermName():    string { return this.terms.find(t    => t.id  === this.info.termId)?.name ?? ''; }
}