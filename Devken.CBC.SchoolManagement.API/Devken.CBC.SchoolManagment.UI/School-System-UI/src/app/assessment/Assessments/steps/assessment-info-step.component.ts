// ═══════════════════════════════════════════════════════════════════
// steps/assessment-info-step.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';

@Component({
  selector: 'app-assessment-info-step',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Basic Information</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Enter core details for this assessment.</p>
  </div>

  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 sm:p-8 space-y-6">

    <!-- Title -->
    <div>
      <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
        Title <span class="text-red-500">*</span>
      </label>
      <input type="text" [(ngModel)]="data.title" (ngModelChange)="onChange()"
        placeholder="e.g. End of Term Mathematics Examination"
        class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
               bg-white dark:bg-gray-700 text-gray-900 dark:text-white
               focus:ring-2 focus:ring-indigo-500 outline-none transition"
        [class.border-red-400]="touched && !data.title?.trim()" />
      <p *ngIf="touched && !data.title?.trim()" class="text-red-500 text-xs mt-1">Title is required</p>
    </div>

    <!-- Description -->
    <div>
      <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Description</label>
      <textarea [(ngModel)]="data.description" (ngModelChange)="onChange()"
        rows="3" placeholder="Brief description of this assessment..."
        class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
               bg-white dark:bg-gray-700 text-gray-900 dark:text-white
               focus:ring-2 focus:ring-indigo-500 outline-none transition resize-none">
      </textarea>
    </div>

    <!-- Date -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          Assessment Date <span class="text-red-500">*</span>
        </label>
        <input type="date" [(ngModel)]="data.assessmentDate" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition"
          [class.border-red-400]="touched && !data.assessmentDate" />
        <p *ngIf="touched && !data.assessmentDate" class="text-red-500 text-xs mt-1">Date is required</p>
      </div>
    </div>

    <hr class="border-gray-200 dark:border-gray-700">

    <!-- Class + Subject -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Class</label>
        <select [(ngModel)]="data.classId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select class</option>
          <option *ngFor="let c of classes" [value]="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Subject</label>
        <select [(ngModel)]="data.subjectId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select subject</option>
          <option *ngFor="let s of subjects" [value]="s.id">{{ s.name }}</option>
        </select>
      </div>
    </div>

    <!-- Teacher + Term -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Teacher</label>
        <select [(ngModel)]="data.teacherId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select teacher</option>
          <option *ngFor="let t of teachers" [value]="t.id">{{ t.firstName }} {{ t.lastName }}</option>
        </select>
      </div>
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Term</label>
        <select [(ngModel)]="data.termId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select term</option>
          <option *ngFor="let t of terms" [value]="t.id">{{ t.name }}</option>
        </select>
      </div>
    </div>

    <!-- Academic Year + School (SuperAdmin) -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Academic Year</label>
        <select [(ngModel)]="data.academicYearId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select academic year</option>
          <option *ngFor="let y of academicYears" [value]="y.id">{{ y.name }}</option>
        </select>
      </div>
      <div *ngIf="isSuperAdmin">
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          School <span class="text-red-500">*</span>
        </label>
        <select [(ngModel)]="data.schoolId" (ngModelChange)="onChange()"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition">
          <option value="">Select school</option>
          <option *ngFor="let s of schools" [value]="s.id">{{ s.name }}</option>
        </select>
      </div>
    </div>

  </div>
</div>
  `,
})
export class AssessmentInfoStepComponent implements OnInit, OnChanges {
  @Input() formData:      any = {};
  @Input() classes:       any[] = [];
  @Input() teachers:      any[] = [];
  @Input() subjects:      any[] = [];
  @Input() terms:         any[] = [];
  @Input() academicYears: any[] = [];
  @Input() schools:       any[] = [];
  @Input() isEditMode   = false;
  @Input() isSuperAdmin = false;

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  data: any = {};
  touched = false;

  ngOnInit():                    void { this.data = { ...this.formData }; this.emitValid(); }
  ngOnChanges(c: SimpleChanges): void { if (c['formData']) { this.data = { ...this.formData }; this.emitValid(); } }

  onChange(): void {
    this.touched = true;
    this.formChanged.emit({ ...this.data });
    this.emitValid();
  }

  private emitValid(): void {
    const ok = !!(this.data.title?.trim() && this.data.assessmentDate &&
                  (!this.isSuperAdmin || this.data.schoolId));
    this.formValid.emit(ok);
  }
}

