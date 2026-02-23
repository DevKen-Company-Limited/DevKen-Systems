
// ═══════════════════════════════════════════════════════════════════
// steps/assessment-details-step.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component as DetailsComponent, Input as DetailsInput, Output as DetailsOutput, EventEmitter as DetailsEE, OnInit as DetailsOnInit, OnChanges as DetailsOnChanges, SimpleChanges as DetailsSC } from '@angular/core';
import { CommonModule as DetailsCM } from '@angular/common';
import { FormsModule as DetailsFM }  from '@angular/forms';

@DetailsComponent({
  selector: 'app-assessment-details-step',
  standalone: true,
  imports: [DetailsCM, DetailsFM],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Assessment Details</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Set the type and scoring configuration.</p>
  </div>

  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 sm:p-8 space-y-6">

    <!-- Assessment Type -->
    <div>
      <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
        Assessment Type <span class="text-red-500">*</span>
      </label>
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <button *ngFor="let type of assessmentTypes" type="button"
          (click)="data.assessmentType = type; onChange()"
          class="relative flex flex-col items-center gap-2 px-4 py-5 rounded-xl border-2 transition-all"
          [ngClass]="{
            'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20': data.assessmentType === type,
            'border-gray-200 dark:border-gray-600 hover:border-indigo-300': data.assessmentType !== type
          }">
          <span class="text-sm font-bold"
            [class.text-indigo-700]="data.assessmentType === type"
            [class.text-gray-700]="data.assessmentType !== type"
            [class.dark:text-indigo-300]="data.assessmentType === type"
            [class.dark:text-gray-300]="data.assessmentType !== type">
            {{ type }}
          </span>
          <span class="text-xs text-center"
            [class.text-indigo-500]="data.assessmentType === type"
            [class.text-gray-400]="data.assessmentType !== type">
            <ng-container *ngIf="type === 'Formative'">Ongoing classroom assessments</ng-container>
            <ng-container *ngIf="type === 'Summative'">End-of-period evaluations</ng-container>
            <ng-container *ngIf="type === 'Competency'">CBC skill-based assessment</ng-container>
          </span>
          <div *ngIf="data.assessmentType === type"
            class="absolute top-2 right-2 w-5 h-5 rounded-full bg-indigo-600 flex items-center justify-center">
            <svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
            </svg>
          </div>
        </button>
      </div>
    </div>

    <hr class="border-gray-200 dark:border-gray-700">

    <!-- Maximum Score -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          Maximum Score <span class="text-red-500">*</span>
        </label>
        <input type="number" [(ngModel)]="data.maximumScore" (ngModelChange)="onChange()"
          min="1" placeholder="100"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition"
          [class.border-red-400]="touched && !data.maximumScore" />
        <p *ngIf="touched && !data.maximumScore" class="text-red-500 text-xs mt-1">Maximum score is required</p>
      </div>
    </div>

    <!-- Preview card -->
    <div class="p-4 bg-indigo-50 dark:bg-indigo-900/20 rounded-xl border border-indigo-200 dark:border-indigo-800">
      <p class="text-xs font-semibold text-indigo-600 dark:text-indigo-400 mb-1 uppercase tracking-wide">Summary</p>
      <p class="text-sm text-gray-700 dark:text-gray-300">
        <span class="font-medium">{{ data.assessmentType || '—' }}</span> assessment with a maximum score of
        <span class="font-medium">{{ data.maximumScore || '—' }}</span>.
      </p>
    </div>

  </div>
</div>
  `,
})
export class AssessmentDetailsStepComponent implements DetailsOnInit, DetailsOnChanges {
  @DetailsInput() formData:        any = {};
  @DetailsInput() assessmentTypes: readonly string[] = [];
  @DetailsInput() isEditMode      = false;

  @DetailsOutput() formChanged = new DetailsEE<any>();
  @DetailsOutput() formValid   = new DetailsEE<boolean>();

  data: any = { assessmentType: 'Formative', maximumScore: 100 };
  touched = false;

  ngOnInit():                     void { this.data = { ...this.data, ...this.formData }; this.emitValid(); }
  ngOnChanges(c: DetailsSC):      void { if (c['formData']) { this.data = { ...this.data, ...this.formData }; this.emitValid(); } }

  onChange(): void {
    this.touched = true;
    this.formChanged.emit({ ...this.data });
    this.emitValid();
  }

  private emitValid(): void {
    this.formValid.emit(!!(this.data.assessmentType && this.data.maximumScore));
  }
}

