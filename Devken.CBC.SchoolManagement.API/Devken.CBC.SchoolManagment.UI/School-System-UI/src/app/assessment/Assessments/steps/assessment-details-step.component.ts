// ═══════════════════════════════════════════════════════════════════
// steps/assessment-details-step.component.ts
//
// FIXES in this revision
// ──────────────────────
// FIX 1 – duration field
//
//   The C# UpdateAssessmentRequest.Duration is TimeSpan? which
//   ASP.NET Core expects as an "HH:mm:ss" JSON string.  The
//   AssessmentResponse.Duration that comes BACK from the API is
//   int? (total minutes).
//
//   This component now treats duration internally as minutes (a
//   plain number) throughout the form.  The mat-input placeholder
//   and hint are updated to say "minutes".
//
//   Conversion boundary is in AssessmentService._prepareRequest():
//     minutes (number)  →  "HH:mm:ss"  before every POST / PUT.
//
//   When the form is populated from an existing assessment
//   (ngOnChanges / ngOnInit with formData), any duration value that
//   arrives as a number is kept as-is (already minutes from the API).
//   If somehow a "HH:mm:ss" string arrives, timeSpanToMinutes()
//   converts it so the input always shows a plain minute count.
//
// No other logic changed.
// ═══════════════════════════════════════════════════════════════════

import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, OnDestroy, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule }             from '@angular/common';
import { FormsModule }              from '@angular/forms';
import { MatFormFieldModule }       from '@angular/material/form-field';
import { MatInputModule }           from '@angular/material/input';
import { MatSelectModule }          from '@angular/material/select';
import { MatSlideToggleModule }     from '@angular/material/slide-toggle';
import { MatIconModule }            from '@angular/material/icon';
import { MatCardModule }            from '@angular/material/card';
import { MatDividerModule }         from '@angular/material/divider';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent }       from '@fuse/components/alert';
import { Subject }                  from 'rxjs';
import { takeUntil, catchError }    from 'rxjs/operators';
import { of }                       from 'rxjs';

import { AssessmentType }    from 'app/assessment/types/assessments';
import { AssessmentService, timeSpanToMinutes } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';

@Component({
  selector: 'app-assessment-details-step',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatSlideToggleModule, MatIconModule, MatCardModule,
    MatDividerModule, MatTooltipModule, MatProgressSpinnerModule,
    FuseAlertComponent,
  ],
  template: `
<div class="max-w-3xl mx-auto">

  <!-- ── Section Header ─────────────────────────────────────────── -->
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Assessment Details</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">
      Set the assessment type, scoring, and advanced configuration.
    </p>
  </div>

  <!-- ── Type Selection ─────────────────────────────────────────── -->
  <mat-card class="shadow-sm mb-6">
    <mat-card-header>
      <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
        <mat-icon class="text-indigo-600 icon-size-4">category</mat-icon>
        Assessment Type
        <span class="ml-auto text-xs font-medium px-2 py-0.5
                     bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded">
          Required
        </span>
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">

        <!-- Formative -->
        <button type="button" (click)="setType(formativeType)"
          class="relative flex flex-col items-start gap-2 px-5 py-5 rounded-xl border-2 transition-all text-left"
          [ngClass]="data.assessmentType === formativeType
            ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 shadow-md'
            : 'border-gray-200 dark:border-gray-600 hover:border-indigo-300 hover:bg-gray-50 dark:hover:bg-gray-700/30'">
          <div class="flex items-center gap-2 w-full">
            <mat-icon class="icon-size-5 flex-shrink-0"
              [class.text-indigo-600]="data.assessmentType === formativeType"
              [class.text-gray-400]="data.assessmentType !== formativeType">edit_note</mat-icon>
            <span class="text-sm font-bold"
              [class.text-indigo-700]="data.assessmentType === formativeType"
              [class.text-gray-700]="data.assessmentType !== formativeType">Formative</span>
            <div *ngIf="data.assessmentType === formativeType"
              class="ml-auto w-5 h-5 rounded-full bg-indigo-600 flex items-center justify-center">
              <mat-icon class="!w-3 !h-3 !text-xs text-white">check</mat-icon>
            </div>
          </div>
          <span class="text-xs pl-7"
            [class.text-indigo-500]="data.assessmentType === formativeType"
            [class.text-gray-400]="data.assessmentType !== formativeType">
            Ongoing classroom assessments
          </span>
        </button>

        <!-- Summative -->
        <button type="button" (click)="setType(summativeType)"
          class="relative flex flex-col items-start gap-2 px-5 py-5 rounded-xl border-2 transition-all text-left"
          [ngClass]="data.assessmentType === summativeType
            ? 'border-violet-500 bg-violet-50 dark:bg-violet-900/20 shadow-md'
            : 'border-gray-200 dark:border-gray-600 hover:border-violet-300 hover:bg-gray-50 dark:hover:bg-gray-700/30'">
          <div class="flex items-center gap-2 w-full">
            <mat-icon class="icon-size-5 flex-shrink-0"
              [class.text-violet-600]="data.assessmentType === summativeType"
              [class.text-gray-400]="data.assessmentType !== summativeType">fact_check</mat-icon>
            <span class="text-sm font-bold"
              [class.text-violet-700]="data.assessmentType === summativeType"
              [class.text-gray-700]="data.assessmentType !== summativeType">Summative</span>
            <div *ngIf="data.assessmentType === summativeType"
              class="ml-auto w-5 h-5 rounded-full bg-violet-600 flex items-center justify-center">
              <mat-icon class="!w-3 !h-3 !text-xs text-white">check</mat-icon>
            </div>
          </div>
          <span class="text-xs pl-7"
            [class.text-violet-500]="data.assessmentType === summativeType"
            [class.text-gray-400]="data.assessmentType !== summativeType">
            End-of-period evaluations
          </span>
        </button>

        <!-- Competency -->
        <button type="button" (click)="setType(competencyType)"
          class="relative flex flex-col items-start gap-2 px-5 py-5 rounded-xl border-2 transition-all text-left"
          [ngClass]="data.assessmentType === competencyType
            ? 'border-teal-500 bg-teal-50 dark:bg-teal-900/20 shadow-md'
            : 'border-gray-200 dark:border-gray-600 hover:border-teal-300 hover:bg-gray-50 dark:hover:bg-gray-700/30'">
          <div class="flex items-center gap-2 w-full">
            <mat-icon class="icon-size-5 flex-shrink-0"
              [class.text-teal-600]="data.assessmentType === competencyType"
              [class.text-gray-400]="data.assessmentType !== competencyType">verified_user</mat-icon>
            <span class="text-sm font-bold"
              [class.text-teal-700]="data.assessmentType === competencyType"
              [class.text-gray-700]="data.assessmentType !== competencyType">Competency</span>
            <div *ngIf="data.assessmentType === competencyType"
              class="ml-auto w-5 h-5 rounded-full bg-teal-600 flex items-center justify-center">
              <mat-icon class="!w-3 !h-3 !text-xs text-white">check</mat-icon>
            </div>
          </div>
          <span class="text-xs pl-7"
            [class.text-teal-500]="data.assessmentType === competencyType"
            [class.text-gray-400]="data.assessmentType !== competencyType">
            CBC skill-based assessment
          </span>
        </button>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Shared Scoring ─────────────────────────────────────────── -->
  <mat-card class="shadow-sm mb-6">
    <mat-card-header>
      <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
        <mat-icon class="text-indigo-600 icon-size-4">calculate</mat-icon>
        Scoring
        <span class="ml-auto text-xs font-medium px-2 py-0.5
                     bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded">
          Required
        </span>
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Maximum Score <span class="text-red-500">*</span></mat-label>
          <input matInput type="number" [(ngModel)]="data.maximumScore"
            (ngModelChange)="onChange()" min="0.01" max="9999.99" step="0.01"
            placeholder="100" />
          <mat-icon matPrefix class="text-gray-400">score</mat-icon>
          <mat-hint>Enter value between 0.01 and 9999.99</mat-hint>
          <mat-error *ngIf="touched && !data.maximumScore">Maximum score is required</mat-error>
        </mat-form-field>
      </div>
    </mat-card-content>
  </mat-card>

  <!-- ══════════════════════════════════════════════════════════════ -->
  <!-- FORMATIVE-SPECIFIC                                            -->
  <!-- ══════════════════════════════════════════════════════════════ -->
  <ng-container *ngIf="data.assessmentType === formativeType">
    <mat-card class="shadow-sm mb-6 border border-indigo-200 dark:border-indigo-800">
      <mat-card-header class="bg-indigo-50 dark:bg-indigo-900/20 !rounded-t-xl">
        <div class="flex items-center gap-2 py-1">
          <div class="w-7 h-7 rounded-lg bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center">
            <mat-icon class="text-indigo-600 dark:text-indigo-400 icon-size-4">edit_note</mat-icon>
          </div>
          <mat-card-title class="!text-sm !font-semibold text-indigo-700 dark:text-indigo-300 !mb-0">
            Formative Options
          </mat-card-title>
        </div>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Formative Type</mat-label>
            <mat-select [(ngModel)]="data.formativeType" (ngModelChange)="onChange()">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="Search…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.formativeType" (ngModelChange)="filterStatic('formativeType')" />
                </div>
              </mat-option>
              <mat-option value="">— Select type —</mat-option>
              <mat-option *ngFor="let o of filtered.formativeTypes" [value]="o.value">{{ o.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">quiz</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Competency Area</mat-label>
            <input matInput [(ngModel)]="data.competencyArea" (ngModelChange)="onChange()"
              placeholder="e.g. Communication" />
            <mat-icon matPrefix class="text-gray-400">hub</mat-icon>
          </mat-form-field>

          <!-- Strand -->
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Strand</mat-label>
            <mat-select [(ngModel)]="data.strandId" (ngModelChange)="onStrandChange($event)">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="Search strands…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.strand" (ngModelChange)="filterLookup('strand', _strands, 'strands')" />
                </div>
              </mat-option>
              <mat-option value="">— Select strand —</mat-option>
              <ng-container *ngIf="loadingStrands">
                <mat-option disabled>
                  <div class="flex items-center gap-2 text-gray-400 text-xs">
                    <mat-progress-spinner mode="indeterminate" diameter="14" strokeWidth="2"></mat-progress-spinner>
                    Loading…
                  </div>
                </mat-option>
              </ng-container>
              <mat-option *ngFor="let s of filtered.strands" [value]="s.id">{{ s.name }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">account_tree</mat-icon>
          </mat-form-field>

          <!-- Sub-Strand -->
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Sub-Strand</mat-label>
            <mat-select [(ngModel)]="data.subStrandId" (ngModelChange)="onSubStrandChange($event)"
              [disabled]="!data.strandId">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="Search sub-strands…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.subStrand" (ngModelChange)="filterLookup('subStrand', _subStrands, 'subStrands')" />
                </div>
              </mat-option>
              <mat-option value="">— Select sub-strand —</mat-option>
              <ng-container *ngIf="loadingSubStrands">
                <mat-option disabled>
                  <div class="flex items-center gap-2 text-gray-400 text-xs">
                    <mat-progress-spinner mode="indeterminate" diameter="14" strokeWidth="2"></mat-progress-spinner>
                    Loading…
                  </div>
                </mat-option>
              </ng-container>
              <mat-option *ngFor="let s of filtered.subStrands" [value]="s.id">{{ s.name }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">device_hub</mat-icon>
            <mat-hint *ngIf="!data.strandId" class="text-amber-500">Select a strand first</mat-hint>
          </mat-form-field>

          <!-- Learning Outcome -->
          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Learning Outcome</mat-label>
            <mat-select
              [(ngModel)]="data.learningOutcomeId"
              (ngModelChange)="onChange()"
              [disabled]="!data.subStrandId">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input
                    class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="Search learning outcomes…"
                    (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.learningOutcome"
                    (ngModelChange)="filterLookup('learningOutcome', _learningOutcomes, 'learningOutcomes')" />
                </div>
              </mat-option>
              <mat-option value="">— Select learning outcome —</mat-option>
              <ng-container *ngIf="loadingOutcomes">
                <mat-option disabled>
                  <div class="flex items-center gap-2 text-gray-400 text-xs">
                    <mat-progress-spinner mode="indeterminate" diameter="14" strokeWidth="2"></mat-progress-spinner>
                    Loading…
                  </div>
                </mat-option>
              </ng-container>
              <mat-option *ngFor="let l of filtered.learningOutcomes" [value]="l.id">
                <div class="flex flex-col">
                  <span class="font-medium">{{ l.code }} - {{ l.outcome }}</span>
                  <span class="text-xs text-gray-500">
                    Level {{ l.level }}
                    <span *ngIf="l.isCore" class="text-indigo-600 font-medium">• Core</span>
                  </span>
                </div>
              </mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">lightbulb</mat-icon>
            <mat-hint *ngIf="!data.subStrandId" class="text-amber-500">Select a sub-strand first</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Assessment Criteria</mat-label>
            <textarea matInput [(ngModel)]="data.criteria" (ngModelChange)="onChange()"
              rows="2" placeholder="Describe the assessment criteria…"></textarea>
            <mat-icon matPrefix class="text-gray-400">checklist</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Feedback Template</mat-label>
            <textarea matInput [(ngModel)]="data.feedbackTemplate" (ngModelChange)="onChange()"
              rows="2" placeholder="Template for student feedback…"></textarea>
            <mat-icon matPrefix class="text-gray-400">rate_review</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Weight (%)</mat-label>
            <input matInput type="number" [(ngModel)]="data.assessmentWeight"
              (ngModelChange)="onChange()" min="0" max="100" placeholder="100" />
            <mat-icon matPrefix class="text-gray-400">percent</mat-icon>
          </mat-form-field>

          <div class="flex items-center gap-3 self-center pt-2">
            <mat-slide-toggle color="primary" [(ngModel)]="data.requiresRubric"
              (ngModelChange)="onChange()">Requires Rubric</mat-slide-toggle>
          </div>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Instructions</mat-label>
            <textarea matInput [(ngModel)]="data.formativeInstructions" (ngModelChange)="onChange()"
              rows="3" placeholder="Assessment instructions for teachers / students…"></textarea>
            <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ══════════════════════════════════════════════════════════════ -->
  <!-- SUMMATIVE-SPECIFIC                                            -->
  <!-- ══════════════════════════════════════════════════════════════ -->
  <ng-container *ngIf="data.assessmentType === summativeType">
    <mat-card class="shadow-sm mb-6 border border-violet-200 dark:border-violet-800">
      <mat-card-header class="bg-violet-50 dark:bg-violet-900/20 !rounded-t-xl">
        <div class="flex items-center gap-2 py-1">
          <div class="w-7 h-7 rounded-lg bg-violet-100 dark:bg-violet-900/40 flex items-center justify-center">
            <mat-icon class="text-violet-600 dark:text-violet-400 icon-size-4">fact_check</mat-icon>
          </div>
          <mat-card-title class="!text-sm !font-semibold text-violet-700 dark:text-violet-300 !mb-0">
            Summative Options
          </mat-card-title>
        </div>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Exam Type</mat-label>
            <mat-select [(ngModel)]="data.examType" (ngModelChange)="onChange()">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-violet-500"
                    placeholder="Search…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.examType" (ngModelChange)="filterStatic('examType')" />
                </div>
              </mat-option>
              <mat-option value="">— Select exam type —</mat-option>
              <mat-option *ngFor="let o of filtered.examTypes" [value]="o.value">{{ o.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">assignment</mat-icon>
          </mat-form-field>

          <!--
            FIX 1 – Duration input
            ─────────────────────
            The field is now a plain NUMBER input (minutes).
            • The form stores: data.duration = 150  (minutes)
            • AssessmentService._prepareRequest() converts → "02:30:00" before POST/PUT
            • On edit, AssessmentResponse.Duration arrives as int (minutes) and is
              assigned directly — no conversion needed at this layer.
          -->
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Duration (minutes)</mat-label>
            <input matInput type="number"
              [(ngModel)]="data.duration"
              (ngModelChange)="onChange()"
              min="0"
              placeholder="e.g. 90" />
            <mat-icon matPrefix class="text-gray-400">timer</mat-icon>
            <mat-hint>Enter total minutes (e.g. 90 = 1 hr 30 min)</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Number of Questions</mat-label>
            <input matInput type="number" [(ngModel)]="data.numberOfQuestions"
              (ngModelChange)="onChange()" min="0" placeholder="50" />
            <mat-icon matPrefix class="text-gray-400">help_outline</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Pass Mark (%)</mat-label>
            <input matInput type="number" [(ngModel)]="data.passMark"
              (ngModelChange)="onChange()" min="0" max="100" placeholder="50" />
            <mat-icon matPrefix class="text-gray-400">check_circle_outline</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Theory Weight (%)</mat-label>
            <input matInput type="number" [(ngModel)]="data.theoryWeight"
              (ngModelChange)="onChange()" min="0" max="100" placeholder="100" />
            <mat-icon matPrefix class="text-gray-400">menu_book</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Practical Weight (%)</mat-label>
            <input matInput type="number" [(ngModel)]="data.practicalWeight"
              (ngModelChange)="onChange()" min="0" max="100" placeholder="0"
              [disabled]="!data.hasPracticalComponent" />
            <mat-icon matPrefix class="text-gray-400">science</mat-icon>
          </mat-form-field>

          <div class="sm:col-span-2 flex items-center gap-3 pt-1">
            <mat-slide-toggle color="primary" [(ngModel)]="data.hasPracticalComponent"
              (ngModelChange)="onChange()">Has Practical Component</mat-slide-toggle>
          </div>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Instructions</mat-label>
            <textarea matInput [(ngModel)]="data.summativeInstructions" (ngModelChange)="onChange()"
              rows="3" placeholder="Exam instructions for students…"></textarea>
            <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ══════════════════════════════════════════════════════════════ -->
  <!-- COMPETENCY-SPECIFIC                                           -->
  <!-- ══════════════════════════════════════════════════════════════ -->
  <ng-container *ngIf="data.assessmentType === competencyType">
    <mat-card class="shadow-sm mb-6 border border-teal-200 dark:border-teal-800">
      <mat-card-header class="bg-teal-50 dark:bg-teal-900/20 !rounded-t-xl">
        <div class="flex items-center gap-2 py-1">
          <div class="w-7 h-7 rounded-lg bg-teal-100 dark:bg-teal-900/40 flex items-center justify-center">
            <mat-icon class="text-teal-600 dark:text-teal-400 icon-size-4">verified_user</mat-icon>
          </div>
          <mat-card-title class="!text-sm !font-semibold text-teal-700 dark:text-teal-300 !mb-0">
            Competency Options
          </mat-card-title>
        </div>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Competency Name <span class="text-red-500">*</span></mat-label>
            <input matInput [(ngModel)]="data.competencyName" (ngModelChange)="onChange()"
              placeholder="e.g. Numeracy and Mathematical Thinking" />
            <mat-icon matPrefix class="text-gray-400">verified_user</mat-icon>
            <mat-error *ngIf="touched && data.assessmentType === competencyType && !data.competencyName">
              Competency name is required
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Strand</mat-label>
            <input matInput [(ngModel)]="data.competencyStrand" (ngModelChange)="onChange()"
              placeholder="e.g. Number" />
            <mat-icon matPrefix class="text-gray-400">account_tree</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Sub-Strand</mat-label>
            <input matInput [(ngModel)]="data.competencySubStrand" (ngModelChange)="onChange()"
              placeholder="e.g. Counting" />
            <mat-icon matPrefix class="text-gray-400">device_hub</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>CBC Level</mat-label>
            <mat-select [(ngModel)]="data.targetLevel" (ngModelChange)="onChange()">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-teal-500"
                    placeholder="Search levels…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.targetLevel" (ngModelChange)="filterStatic('targetLevel')" />
                </div>
              </mat-option>
              <mat-option [value]="null">— Select level —</mat-option>
              <mat-option *ngFor="let o of filtered.targetLevels" [value]="o.value">{{ o.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">stairs</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Assessment Method</mat-label>
            <mat-select [(ngModel)]="data.assessmentMethod" (ngModelChange)="onChange()">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-teal-500"
                    placeholder="Search methods…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.assessmentMethod" (ngModelChange)="filterStatic('assessmentMethod')" />
                </div>
              </mat-option>
              <mat-option [value]="null">— Select method —</mat-option>
              <mat-option *ngFor="let o of filtered.assessmentMethods" [value]="o.value">{{ o.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">psychology</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Rating Scale</mat-label>
            <mat-select [(ngModel)]="data.ratingScale" (ngModelChange)="onChange()">
              <mat-option disabled class="!h-auto !px-0 !py-0">
                <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                  <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-teal-500"
                    placeholder="Search scales…" (keydown.Space)="$event.stopPropagation()"
                    [(ngModel)]="filters.ratingScale" (ngModelChange)="filterStatic('ratingScale')" />
                </div>
              </mat-option>
              <mat-option value="">— Select scale —</mat-option>
              <mat-option *ngFor="let o of filtered.ratingScales" [value]="o.value">{{ o.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">star_rate</mat-icon>
          </mat-form-field>

          <div class="flex items-center gap-3 self-center pt-1">
            <mat-slide-toggle color="primary" [(ngModel)]="data.isObservationBased"
              (ngModelChange)="onChange()">Observation Based</mat-slide-toggle>
          </div>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Performance Indicators</mat-label>
            <textarea matInput [(ngModel)]="data.performanceIndicators" (ngModelChange)="onChange()"
              rows="2" placeholder="Key indicators for this competency…"></textarea>
            <mat-icon matPrefix class="text-gray-400">show_chart</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Tools Required</mat-label>
            <input matInput [(ngModel)]="data.toolsRequired" (ngModelChange)="onChange()"
              placeholder="e.g. Ruler, Calculator, Protractor" />
            <mat-icon matPrefix class="text-gray-400">construction</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Specific Learning Outcome</mat-label>
            <textarea matInput [(ngModel)]="data.specificLearningOutcome" (ngModelChange)="onChange()"
              rows="2" placeholder="Describe the specific learning outcome…"></textarea>
            <mat-icon matPrefix class="text-gray-400">school</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Instructions</mat-label>
            <textarea matInput [(ngModel)]="data.competencyInstructions" (ngModelChange)="onChange()"
              rows="3" placeholder="Assessment instructions…"></textarea>
            <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ── Summary Preview ────────────────────────────────────────── -->
  <fuse-alert [type]="typeBadge.alertType" appearance="soft" [showIcon]="true">
    <span fuseAlertTitle>{{ typeBadge.label }} Assessment Summary</span>
    Max score: <strong>{{ data.maximumScore || '—' }}</strong>
    <ng-container *ngIf="data.assessmentType === summativeType && data.passMark">
      · Pass mark: <strong>{{ data.passMark }}%</strong>
    </ng-container>
    <ng-container *ngIf="data.assessmentType === summativeType && data.duration">
      · Duration: <strong>{{ data.duration }} min</strong>
    </ng-container>
    <ng-container *ngIf="data.assessmentType === competencyType && data.competencyName">
      · Competency: <strong>{{ data.competencyName }}</strong>
    </ng-container>
    <ng-container *ngIf="data.assessmentType === formativeType && data.strandId">
      · Strand: <strong>{{ getStrandName(data.strandId) }}</strong>
    </ng-container>
  </fuse-alert>

</div>
  `,
})
export class AssessmentDetailsStepComponent implements OnInit, OnChanges, OnDestroy {

  private _service  = inject(AssessmentService);
  private _destroy$ = new Subject<void>();

  readonly formativeType  = AssessmentType.Formative;
  readonly summativeType  = AssessmentType.Summative;
  readonly competencyType = AssessmentType.Competency;

  @Input() schoolId?: string;
  @Input() formData:  any  = {};
  @Input() isEditMode      = false;

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  data: any = {
    assessmentType:     AssessmentType.Formative,
    maximumScore:       100,
    assessmentWeight:   100,
    theoryWeight:       100,
    passMark:           50,
    isObservationBased: true,
  };

  touched = false;

  loadingStrands    = false;
  loadingSubStrands = false;
  loadingOutcomes   = false;

  _strands:          any[] = [];
  _subStrands:       any[] = [];
  _learningOutcomes: any[] = [];

  private readonly ALL_FORMATIVE_TYPES = [
    { value: 'Quiz',           label: 'Quiz'            },
    { value: 'Homework',       label: 'Homework'        },
    { value: 'ClassActivity',  label: 'Class Activity'  },
    { value: 'Project',        label: 'Project'         },
    { value: 'Observation',    label: 'Observation'     },
    { value: 'Portfolio',      label: 'Portfolio'       },
    { value: 'PeerAssessment', label: 'Peer Assessment' },
  ];

  private readonly ALL_EXAM_TYPES = [
    { value: 'MidTerm', label: 'Mid-Term' },
    { value: 'EndTerm', label: 'End-Term' },
    { value: 'Mock',    label: 'Mock'     },
    { value: 'CAT',     label: 'CAT'      },
    { value: 'Final',   label: 'Final'    },
  ];

  private readonly ALL_ASSESSMENT_METHODS = [
    { value: 0, label: 'Observation'     },
    { value: 1, label: 'Written'         },
    { value: 2, label: 'Oral'            },
    { value: 3, label: 'Practical'       },
    { value: 4, label: 'Portfolio'       },
    { value: 5, label: 'Peer Assessment' },
  ];

  private readonly ALL_TARGET_LEVELS = [
    { value: 1,  label: 'PP1'            },
    { value: 2,  label: 'PP2'            },
    { value: 3,  label: 'Grade 1'        },
    { value: 4,  label: 'Grade 2'        },
    { value: 5,  label: 'Grade 3'        },
    { value: 6,  label: 'Grade 4'        },
    { value: 7,  label: 'Grade 5'        },
    { value: 8,  label: 'Grade 6'        },
    { value: 9,  label: 'Grade 7 (JSS1)' },
    { value: 10, label: 'Grade 8 (JSS2)' },
    { value: 11, label: 'Grade 9 (JSS3)' },
  ];

  private readonly ALL_RATING_SCALES = [
    { value: 'EE-ME-AE-BE', label: 'EE / ME / AE / BE (CBC Standard)' },
    { value: '1-4',          label: '1 – 4'                            },
    { value: '1-5',          label: '1 – 5'                            },
    { value: 'Pass/Fail',    label: 'Pass / Fail'                      },
  ];

  filters: Record<string, string> = {
    formativeType: '', examType: '', assessmentMethod: '',
    targetLevel:   '', ratingScale: '',
    strand:        '', subStrand: '', learningOutcome: '',
  };

  filtered: Record<string, any[]> = {
    formativeTypes:    [...this.ALL_FORMATIVE_TYPES],
    examTypes:         [...this.ALL_EXAM_TYPES],
    assessmentMethods: [...this.ALL_ASSESSMENT_METHODS],
    targetLevels:      [...this.ALL_TARGET_LEVELS],
    ratingScales:      [...this.ALL_RATING_SCALES],
    strands:           [],
    subStrands:        [],
    learningOutcomes:  [],
  };

  ngOnInit(): void {
    // FIX 1: normalise duration when loading existing data into the form
    this.data = { ...this.data, ...this._normaliseDuration(this.formData) };
    this._loadStrands();
    if (this.data.strandId)    this._loadSubStrands(this.data.strandId);
    if (this.data.subStrandId) this._loadLearningOutcomes(this.data.subStrandId);
    this.emitValid();
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && !c['formData'].firstChange) {
      // FIX 1: normalise duration on every formData update
      this.data = { ...this.data, ...this._normaliseDuration(this.formData) };
      this.emitValid();
    }
    if (c['schoolId'] && !c['schoolId'].firstChange) {
      this._strands = []; this._subStrands = []; this._learningOutcomes = [];
      this.filtered.strands = []; this.filtered.subStrands = []; this.filtered.learningOutcomes = [];
      this.data.strandId = ''; this.data.subStrandId = ''; this.data.learningOutcomeId = '';
      this._loadStrands();
      this.onChange();
    }
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  /**
   * FIX 1 helper: ensure duration is always stored as plain minutes (number)
   * regardless of whether the source is:
   *   - int (from AssessmentResponse)  → kept as-is
   *   - "HH:mm:ss" string (edge case)  → converted via timeSpanToMinutes()
   *   - null / undefined               → left alone
   */
  private _normaliseDuration(source: any): any {
    if (!source || source.duration == null) return source;
    const minutes = timeSpanToMinutes(source.duration);
    return { ...source, duration: minutes };
  }

  private _loadStrands(): void {
    this.loadingStrands = true;
    this._service.getStrands(undefined, this.schoolId)
      .pipe(takeUntil(this._destroy$), catchError(() => of([])))
      .subscribe(data => {
        this._strands         = data;
        this.filtered.strands = [...data];
        this.loadingStrands   = false;
        if (this.data.strandId && data.length > 0) {
          if (!data.some((s: any) => s.id === this.data.strandId)) {
            this.data.strandId = ''; this.data.subStrandId = ''; this.data.learningOutcomeId = '';
          }
        }
      });
  }

  private _loadSubStrands(strandId: string): void {
    if (!strandId) { this._subStrands = []; this.filtered.subStrands = []; return; }
    this.loadingSubStrands = true;
    this._service.getSubStrands(strandId, this.schoolId)
      .pipe(takeUntil(this._destroy$), catchError(() => of([])))
      .subscribe(data => {
        this._subStrands = data; this.filtered.subStrands = [...data]; this.loadingSubStrands = false;
      });
  }

  private _loadLearningOutcomes(subStrandId: string): void {
    if (!subStrandId) { this._learningOutcomes = []; this.filtered.learningOutcomes = []; return; }
    this.loadingOutcomes = true;
    this._service.getLearningOutcomes(subStrandId, this.schoolId)
      .pipe(takeUntil(this._destroy$), catchError(() => of([])))
      .subscribe(data => {
        this._learningOutcomes = data; this.filtered.learningOutcomes = [...data]; this.loadingOutcomes = false;
      });
  }

  onStrandChange(strandId: string): void {
    this.data.subStrandId = ''; this.data.learningOutcomeId = '';
    this._subStrands = []; this._learningOutcomes = [];
    this.filtered.subStrands = []; this.filtered.learningOutcomes = [];
    this.filters.subStrand = ''; this.filters.learningOutcome = '';
    if (strandId) this._loadSubStrands(strandId);
    this.onChange();
  }

  onSubStrandChange(subStrandId: string): void {
    this.data.learningOutcomeId = '';
    this._learningOutcomes = []; this.filtered.learningOutcomes = [];
    this.filters.learningOutcome = '';
    if (subStrandId) this._loadLearningOutcomes(subStrandId);
    this.onChange();
  }

  setType(type: AssessmentType): void { this.data.assessmentType = type; this.onChange(); }

  filterStatic(key: string): void {
    const q = (this.filters[key] || '').toLowerCase();
    const map: Record<string, { src: any[], dest: string }> = {
      formativeType:    { src: this.ALL_FORMATIVE_TYPES,    dest: 'formativeTypes'    },
      examType:         { src: this.ALL_EXAM_TYPES,         dest: 'examTypes'         },
      assessmentMethod: { src: this.ALL_ASSESSMENT_METHODS, dest: 'assessmentMethods' },
      targetLevel:      { src: this.ALL_TARGET_LEVELS,      dest: 'targetLevels'      },
      ratingScale:      { src: this.ALL_RATING_SCALES,      dest: 'ratingScales'      },
    };
    const m = map[key]; if (!m) return;
    this.filtered[m.dest] = q ? m.src.filter(o => o.label.toLowerCase().includes(q)) : [...m.src];
  }

  filterLookup(filterKey: string, src: any[], destKey: string): void {
    const q = (this.filters[filterKey] || '').toLowerCase();
    this.filtered[destKey] = q ? src.filter(o => (o.name || '').toLowerCase().includes(q)) : [...src];
  }

  getStrandName(id: string): string { return this._strands.find(s => s.id === id)?.name ?? id; }

  get typeBadge(): { label: string; alertType: 'info' | 'success' | 'primary' } {
    switch (this.data.assessmentType) {
      case AssessmentType.Formative:  return { label: 'Formative',  alertType: 'primary' };
      case AssessmentType.Summative:  return { label: 'Summative',  alertType: 'info'    };
      case AssessmentType.Competency: return { label: 'Competency', alertType: 'success' };
      default: return { label: '—', alertType: 'info' };
    }
  }

  onChange(): void {
    this.touched = true;
    this.formChanged.emit({ ...this.data });
    this.emitValid();
  }

  private emitValid(): void {
    const hasBase      = !!(this.data.assessmentType && this.data.maximumScore);
    const competencyOk = this.data.assessmentType !== AssessmentType.Competency || !!this.data.competencyName;
    this.formValid.emit(hasBase && competencyOk);
  }
}