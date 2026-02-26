// ═══════════════════════════════════════════════════════════════════
// grades/assessment-grades.component.ts
//
// TypeScript errors fixed:
//
//  FIX 1 – TS2339 getStudentsByAssessment does not exist:
//   Removed the non-existent method call. Students are now loaded in
//   a two-step approach using switchMap: first fetch the assessment
//   (which carries classId), then fetch students scoped to that
//   classId via the existing getStudents(classId) overload.
//
//  FIX 2 – TS2322 AssessmentResponse not assignable to AssessmentListItem:
//   The assessment field is now typed as AssessmentResponse | null
//   (the full DTO) instead of AssessmentListItem (the lightweight
//   list DTO whose classId was mistakenly typed as a method).
//   The template only reads shared fields present on both types so
//   no template changes are required.
//
//  FIX 3 – TS1117 duplicate 'assessmentType' key in object literal:
//   saveScore() now spreads scoreForm first, then overrides the three
//   authoritative fields (assessmentId, assessmentType, studentId)
//   in one clean assignment — no property appears twice.
//
//  Also fixed in types/assessments.ts (separate file):
//   AssessmentListItem.classId was `classId(classId: any): unknown`
//   (a method signature) — corrected to `classId?: string`.
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, inject,
  ViewChild, TemplateRef, AfterViewInit,
} from '@angular/core';
import { CommonModule }             from '@angular/common';
import { FormsModule }              from '@angular/forms';
import { ActivatedRoute, Router }   from '@angular/router';
import { MatIconModule }            from '@angular/material/icon';
import { MatButtonModule }          from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { MatFormFieldModule }       from '@angular/material/form-field';
import { MatInputModule }           from '@angular/material/input';
import { MatSelectModule }          from '@angular/material/select';
import { MatSlideToggleModule }     from '@angular/material/slide-toggle';
import { MatCardModule }            from '@angular/material/card';
import { MatDividerModule }         from '@angular/material/divider';
import { MatChipsModule }           from '@angular/material/chips';
import { FuseAlertComponent }       from '@fuse/components/alert';
import { Subject, forkJoin, of }    from 'rxjs';
import { switchMap, takeUntil, catchError, finalize } from 'rxjs/operators';

import { AssessmentService }       from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AlertService }            from 'app/core/DevKenService/Alert/AlertService';
import { AssessmentReportService } from 'app/core/DevKenService/assessments/Assessments/AssessmentReportService';
import { PageHeaderComponent, Breadcrumb }
  from 'app/shared/Page-Header/page-header.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState }
  from 'app/shared/data-table/data-table.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import {
  AssessmentType,
  AssessmentResponse,       // FIX 2: full DTO — classId is always string
  AssessmentScoreResponse,
  UpsertScoreRequest,
  getAssessmentTypeLabel,
} from 'app/assessment/types/assessments';

@Component({
  selector: 'app-assessment-grades',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatSlideToggleModule, MatCardModule, MatDividerModule, MatChipsModule,
    FuseAlertComponent,
    PageHeaderComponent, DataTableComponent, PaginationComponent,
  ],
  template: `
<div class="absolute inset-0 flex min-w-0 flex-col overflow-y-auto">

  <!-- ── Page Header ──────────────────────────────────────────── -->
  <app-page-header
    [title]="assessmentTitle"
    [description]="'Manage grades for ' + typeLabel + ' assessment'"
    icon="grading"
    [breadcrumbs]="breadcrumbs"
    [actionTemplate]="headerActions">
  </app-page-header>

  <ng-template #headerActions>
    <button mat-stroked-button [disabled]="isDownloading" (click)="downloadPdf()"
      class="mr-3 border-green-200 text-green-700 hover:bg-green-50">
      <ng-container *ngIf="isDownloading; else dlIcon">
        <mat-progress-spinner mode="indeterminate" diameter="18" strokeWidth="2"
          class="inline-block mr-2"></mat-progress-spinner>
      </ng-container>
      <ng-template #dlIcon><mat-icon class="icon-size-5">download</mat-icon></ng-template>
      <span class="ml-2">{{ isDownloading ? 'Generating…' : 'Export PDF' }}</span>
    </button>
    <button mat-flat-button class="bg-white text-indigo-700 hover:bg-indigo-50 shadow-lg font-bold"
      (click)="openScoreModal(null)">
      <mat-icon class="icon-size-5">add</mat-icon>
      <span class="ml-2">Add Score</span>
    </button>
  </ng-template>

  <!-- ── Main Content ─────────────────────────────────────────── -->
  <div class="bg-card -mt-10 flex-auto rounded-t-2xl p-6 shadow sm:p-10">

    <!-- Assessment summary strip -->
    <mat-card *ngIf="assessment" class="mb-6 shadow-sm" [ngClass]="{
      'border border-indigo-200 dark:border-indigo-800': assessmentType === formativeType,
      'border border-violet-200 dark:border-violet-800': assessmentType === summativeType,
      'border border-teal-200   dark:border-teal-800':   assessmentType === competencyType
    }">
      <mat-card-content class="!py-4">
        <div class="flex flex-wrap items-center gap-4">
          <div class="flex items-center gap-2">
            <mat-icon class="icon-size-5"
              [ngClass]="{
                'text-indigo-600': assessmentType === formativeType,
                'text-violet-600': assessmentType === summativeType,
                'text-teal-600':   assessmentType === competencyType
              }">assignment</mat-icon>
            <span class="font-semibold text-gray-900 dark:text-white">{{ assessment.title }}</span>
          </div>
          <span class="text-sm text-gray-500">
            {{ assessment.className }} · {{ assessment.termName }}
          </span>
          <mat-chip-listbox class="pointer-events-none">
            <mat-chip [highlighted]="true"
              [color]="assessmentType === formativeType ? 'primary'
                     : assessmentType === summativeType ? 'accent' : 'warn'">
              {{ typeLabel }}
            </mat-chip>
          </mat-chip-listbox>
          <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
            Max: <strong>{{ assessment.maximumScore }}</strong>
          </span>
          <span class="text-sm text-gray-500">{{ scores.length }} submitted</span>
          <span class="ml-auto px-3 py-1 rounded-full text-xs font-semibold"
            [ngClass]="assessment.isPublished
              ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
              : 'bg-gray-100  text-gray-600  dark:bg-gray-800     dark:text-gray-400'">
            {{ assessment.isPublished ? 'Published' : 'Draft' }}
          </span>
        </div>
      </mat-card-content>
    </mat-card>

    <!-- Stats row -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6" *ngIf="scores.length">
      <mat-card class="shadow-sm text-center">
        <mat-card-content class="!py-4">
          <p class="text-2xl font-bold text-indigo-600">{{ scores.length }}</p>
          <p class="text-xs text-gray-500 mt-1">Total Scores</p>
        </mat-card-content>
      </mat-card>
      <mat-card class="shadow-sm text-center" *ngIf="assessmentType === formativeType">
        <mat-card-content class="!py-4">
          <p class="text-2xl font-bold text-green-600">{{ submittedCount }}</p>
          <p class="text-xs text-gray-500 mt-1">Submitted</p>
        </mat-card-content>
      </mat-card>
      <mat-card class="shadow-sm text-center" *ngIf="assessmentType === summativeType">
        <mat-card-content class="!py-4">
          <p class="text-2xl font-bold text-green-600">{{ passedCount }}</p>
          <p class="text-xs text-gray-500 mt-1">Passed</p>
        </mat-card-content>
      </mat-card>
      <mat-card class="shadow-sm text-center" *ngIf="assessmentType === competencyType">
        <mat-card-content class="!py-4">
          <p class="text-2xl font-bold text-teal-600">{{ finalizedCount }}</p>
          <p class="text-xs text-gray-500 mt-1">Finalized</p>
        </mat-card-content>
      </mat-card>
      <mat-card class="shadow-sm text-center" *ngIf="avgScore != null">
        <mat-card-content class="!py-4">
          <p class="text-2xl font-bold text-violet-600">{{ avgScore | number:'1.1-1' }}</p>
          <p class="text-xs text-gray-500 mt-1">Avg Score</p>
        </mat-card-content>
      </mat-card>
    </div>

    <!-- Table -->
    <app-data-table
      [columns]="tableColumns"
      [data]="paginatedScores"
      [actions]="tableActions"
      [loading]="isLoading"
      [header]="tableHeader"
      [emptyState]="tableEmptyState"
      [cellTemplates]="cellTemplates">
    </app-data-table>

    <app-pagination
      *ngIf="!isLoading"
      [currentPage]="currentPage"
      [totalItems]="scores.length"
      [itemsPerPage]="itemsPerPage"
      [itemLabel]="'scores'"
      [showItemsPerPageSelector]="true"
      [showPageNumbers]="true"
      (pageChange)="currentPage = $event"
      (itemsPerPageChange)="itemsPerPage = $event; currentPage = 1">
    </app-pagination>

  </div>
</div>

<!-- ══════════════════════════════════════════════════════════════ -->
<!-- Cell Templates                                                -->
<!-- ══════════════════════════════════════════════════════════════ -->

<ng-template #studentCellRef let-row>
  <div class="flex items-center gap-3">
    <div class="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500
                flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
      {{ getInitials(row.studentName) }}
    </div>
    <div>
      <p class="text-sm font-semibold text-gray-900 dark:text-white">{{ row.studentName }}</p>
      <p class="text-xs text-gray-500">{{ row.studentAdmissionNo }}</p>
    </div>
  </div>
</ng-template>

<ng-template #scoreCellRef let-row>
  <ng-container [ngSwitch]="assessmentType">
    <ng-container *ngSwitchCase="formativeType">
      <div class="flex flex-col">
        <span class="font-bold text-indigo-600 dark:text-indigo-400">
          {{ row.score ?? '—' }} / {{ row.maximumScore ?? assessment?.maximumScore }}
        </span>
        <span *ngIf="row.percentage != null" class="text-xs text-gray-500">
          {{ row.percentage | number:'1.0-1' }}%
        </span>
      </div>
    </ng-container>
    <ng-container *ngSwitchCase="summativeType">
      <div class="flex flex-col">
        <span class="font-bold text-violet-600 dark:text-violet-400">
          {{ row.totalScore ?? '—' }} / {{ row.maximumTotalScore ?? assessment?.maximumScore }}
        </span>
        <span *ngIf="row.theoryScore != null" class="text-xs text-gray-500">
          Theory: {{ row.theoryScore }} · Practical: {{ row.practicalScore ?? 0 }}
        </span>
      </div>
    </ng-container>
    <ng-container *ngSwitchCase="competencyType">
      <span class="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-bold border
                   bg-teal-50 text-teal-700 border-teal-200 dark:bg-teal-900/30 dark:text-teal-300">
        {{ row.rating || '—' }}
      </span>
    </ng-container>
  </ng-container>
</ng-template>

<ng-template #gradeCellRef let-row>
  <ng-container [ngSwitch]="assessmentType">
    <span *ngSwitchCase="formativeType" class="font-semibold text-gray-900 dark:text-white">
      {{ row.grade || '—' }}
    </span>
    <span *ngSwitchCase="summativeType"
      [ngClass]="row.isPassed
        ? 'px-2.5 py-1 rounded-full text-xs font-semibold bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
        : 'px-2.5 py-1 rounded-full text-xs font-semibold bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400'">
      {{ row.isPassed ? 'Pass' : 'Fail' }}
    </span>
    <span *ngSwitchCase="competencyType" class="text-sm text-gray-700 dark:text-gray-300">
      {{ row.competencyLevel || '—' }}
    </span>
  </ng-container>
</ng-template>

<ng-template #feedbackCellRef let-row>
  <p class="text-xs text-gray-500 dark:text-gray-400 max-w-xs truncate"
    [matTooltip]="row.feedback || row.remarks || row.evidence || ''">
    {{ row.feedback || row.remarks || row.evidence || '—' }}
  </p>
</ng-template>

<ng-template #statusCellRef let-row>
  <ng-container [ngSwitch]="assessmentType">
    <span *ngSwitchCase="formativeType" class="px-2.5 py-1 rounded-full text-xs font-semibold"
      [ngClass]="row.isSubmitted
        ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
        : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'">
      {{ row.isSubmitted ? 'Submitted' : 'Pending' }}
    </span>
    <span *ngSwitchCase="summativeType" class="px-2.5 py-1 rounded-full text-xs font-semibold
      bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">
      {{ row.performanceStatus || 'Recorded' }}
    </span>
    <span *ngSwitchCase="competencyType" class="px-2.5 py-1 rounded-full text-xs font-semibold"
      [ngClass]="row.isFinalized
        ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
        : 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'">
      {{ row.isFinalized ? 'Finalized' : 'Draft' }}
    </span>
  </ng-container>
</ng-template>

<!-- ══════════════════════════════════════════════════════════════ -->
<!-- Score Modal                                                   -->
<!-- ══════════════════════════════════════════════════════════════ -->
<div *ngIf="showModal"
  class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-xl max-h-[90vh] overflow-y-auto">

    <!-- Modal Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
      <div class="flex items-center gap-3">
        <div class="flex items-center justify-center w-10 h-10 rounded-xl shadow-lg"
          [ngClass]="{
            'bg-gradient-to-br from-indigo-500 to-indigo-700': assessmentType === formativeType,
            'bg-gradient-to-br from-violet-500 to-violet-700': assessmentType === summativeType,
            'bg-gradient-to-br from-teal-500   to-teal-700':   assessmentType === competencyType
          }">
          <mat-icon class="text-white !w-5 !h-5">grade</mat-icon>
        </div>
        <div>
          <h3 class="text-lg font-bold text-gray-900 dark:text-white">
            {{ editingScore ? 'Edit Score' : 'Add Score' }}
          </h3>
          <p class="text-xs text-gray-500">{{ typeLabel }} Assessment</p>
        </div>
      </div>
      <button mat-icon-button (click)="closeModal()">
        <mat-icon>close</mat-icon>
      </button>
    </div>

    <!-- Modal Body -->
    <div class="p-6 space-y-6">

      <!-- Student selector -->
      <mat-card class="shadow-none border border-gray-200 dark:border-gray-700">
        <mat-card-header>
          <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
            <mat-icon class="text-indigo-600 icon-size-4">person</mat-icon>
            Student
            <span *ngIf="!editingScore"
              class="ml-auto text-xs font-medium px-2 py-0.5 bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded">
              Required
            </span>
          </mat-card-title>
        </mat-card-header>
        <mat-card-content class="!pt-3">
          <ng-container *ngIf="!editingScore">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Select Student <span class="text-red-500">*</span></mat-label>
              <mat-select [(ngModel)]="scoreForm.studentId">
                <mat-option disabled class="!h-auto !px-0 !py-0">
                  <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                    <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none
                                  focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      placeholder="Search students…"
                      (keydown.Space)="$event.stopPropagation()"
                      [(ngModel)]="studentSearch"
                      (ngModelChange)="filterStudents()" />
                  </div>
                </mat-option>
                <mat-option value="">— Select student —</mat-option>
                <mat-option *ngFor="let s of filteredStudents" [value]="s.id">
                  {{ s.firstName }} {{ s.lastName }}
                  <span class="text-xs text-gray-400 ml-2">({{ s.admissionNo }})</span>
                </mat-option>
              </mat-select>
              <mat-icon matPrefix class="text-gray-400">person_search</mat-icon>
            </mat-form-field>
          </ng-container>

          <ng-container *ngIf="editingScore">
            <div class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-700/50 rounded-xl">
              <div class="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500
                          flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                {{ getInitials(editingScore.studentName) }}
              </div>
              <div>
                <p class="text-sm font-semibold text-gray-900 dark:text-white">{{ editingScore.studentName }}</p>
                <p class="text-xs text-gray-400">{{ editingScore.studentAdmissionNo }}</p>
              </div>
              <mat-chip class="ml-auto" [highlighted]="true" color="accent">Editing</mat-chip>
            </div>
          </ng-container>
        </mat-card-content>
      </mat-card>

      <!-- ── FORMATIVE Score Fields ──────────────────────────────── -->
      <ng-container *ngIf="assessmentType === formativeType">
        <mat-card class="shadow-none border border-indigo-200 dark:border-indigo-800">
          <mat-card-header class="bg-indigo-50 dark:bg-indigo-900/20 !rounded-t-xl">
            <mat-card-title class="!text-sm !font-semibold text-indigo-700 dark:text-indigo-300 !mb-0 flex items-center gap-2">
              <mat-icon class="text-indigo-600 icon-size-4">edit_note</mat-icon>
              Formative Score
            </mat-card-title>
          </mat-card-header>
          <mat-card-content class="!pt-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Score</mat-label>
                <input matInput type="number" [(ngModel)]="scoreForm.score"
                  min="0" [max]="assessment?.maximumScore" placeholder="0" />
                <mat-icon matPrefix class="text-gray-400">numbers</mat-icon>
                <mat-hint>Max: {{ assessment?.maximumScore }}</mat-hint>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Grade Letter</mat-label>
                <input matInput [(ngModel)]="scoreForm.grade" placeholder="e.g. A, B+, C" />
                <mat-icon matPrefix class="text-gray-400">star</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full sm:col-span-2">
                <mat-label>Performance Level</mat-label>
                <mat-select [(ngModel)]="scoreForm.performanceLevel">
                  <mat-option disabled class="!h-auto !px-0 !py-0">
                    <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                      <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-indigo-500"
                        placeholder="Search levels…" (keydown.Space)="$event.stopPropagation()"
                        [(ngModel)]="modalFilters.performanceLevel"
                        (ngModelChange)="filterModalStatic('performanceLevel')" />
                    </div>
                  </mat-option>
                  <mat-option value="">— Select level —</mat-option>
                  <mat-option *ngFor="let o of modalFiltered.performanceLevels" [value]="o.value">
                    {{ o.label }}
                  </mat-option>
                </mat-select>
                <mat-icon matPrefix class="text-gray-400">insights</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full sm:col-span-2">
                <mat-label>Feedback</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.feedback" rows="2"
                  placeholder="Feedback for student…"></textarea>
                <mat-icon matPrefix class="text-gray-400">rate_review</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Strengths</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.strengths" rows="2"
                  placeholder="Areas of strength…"></textarea>
                <mat-icon matPrefix class="text-gray-400">thumb_up</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Areas for Improvement</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.areasForImprovement" rows="2"
                  placeholder="Areas to improve…"></textarea>
                <mat-icon matPrefix class="text-gray-400">trending_up</mat-icon>
              </mat-form-field>

              <div class="flex flex-col gap-3 sm:col-span-2">
                <mat-slide-toggle color="primary" [(ngModel)]="scoreForm.isSubmitted">
                  Submitted
                </mat-slide-toggle>
                <mat-slide-toggle color="primary" [(ngModel)]="scoreForm.competencyAchieved">
                  Competency Achieved
                </mat-slide-toggle>
              </div>

            </div>
          </mat-card-content>
        </mat-card>
      </ng-container>

      <!-- ── SUMMATIVE Score Fields ──────────────────────────────── -->
      <ng-container *ngIf="assessmentType === summativeType">
        <mat-card class="shadow-none border border-violet-200 dark:border-violet-800">
          <mat-card-header class="bg-violet-50 dark:bg-violet-900/20 !rounded-t-xl">
            <mat-card-title class="!text-sm !font-semibold text-violet-700 dark:text-violet-300 !mb-0 flex items-center gap-2">
              <mat-icon class="text-violet-600 icon-size-4">fact_check</mat-icon>
              Summative Score
            </mat-card-title>
          </mat-card-header>
          <mat-card-content class="!pt-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Theory Score</mat-label>
                <input matInput type="number" [(ngModel)]="scoreForm.theoryScore" min="0" />
                <mat-icon matPrefix class="text-gray-400">menu_book</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Practical Score</mat-label>
                <input matInput type="number" [(ngModel)]="scoreForm.practicalScore" min="0" />
                <mat-icon matPrefix class="text-gray-400">science</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Position in Class</mat-label>
                <input matInput type="number" [(ngModel)]="scoreForm.positionInClass" min="1" />
                <mat-icon matPrefix class="text-gray-400">leaderboard</mat-icon>
              </mat-form-field>

              <div class="flex items-center gap-3 self-center pt-1">
                <mat-slide-toggle color="primary" [(ngModel)]="scoreForm.isPassed">
                  Passed
                </mat-slide-toggle>
              </div>

              <mat-form-field appearance="outline" class="w-full sm:col-span-2">
                <mat-label>Remarks</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.remarks" rows="2"
                  placeholder="Teacher remarks…"></textarea>
                <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full sm:col-span-2">
                <mat-label>Comments</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.comments" rows="2"
                  placeholder="Additional comments…"></textarea>
                <mat-icon matPrefix class="text-gray-400">comment</mat-icon>
              </mat-form-field>

            </div>
          </mat-card-content>
        </mat-card>
      </ng-container>

      <!-- ── COMPETENCY Score Fields ─────────────────────────────── -->
      <ng-container *ngIf="assessmentType === competencyType">
        <mat-card class="shadow-none border border-teal-200 dark:border-teal-800">
          <mat-card-header class="bg-teal-50 dark:bg-teal-900/20 !rounded-t-xl">
            <mat-card-title class="!text-sm !font-semibold text-teal-700 dark:text-teal-300 !mb-0 flex items-center gap-2">
              <mat-icon class="text-teal-600 icon-size-4">verified_user</mat-icon>
              Competency Score
            </mat-card-title>
          </mat-card-header>
          <mat-card-content class="!pt-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Rating <span class="text-red-500">*</span></mat-label>
                <mat-select [(ngModel)]="scoreForm.rating">
                  <mat-option disabled class="!h-auto !px-0 !py-0">
                    <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                      <input class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-teal-500"
                        placeholder="Search ratings…" (keydown.Space)="$event.stopPropagation()"
                        [(ngModel)]="modalFilters.rating"
                        (ngModelChange)="filterModalStatic('rating')" />
                    </div>
                  </mat-option>
                  <mat-option value="">— Select rating —</mat-option>
                  <mat-option *ngFor="let o of modalFiltered.ratings" [value]="o.value">{{ o.label }}</mat-option>
                </mat-select>
                <mat-icon matPrefix class="text-gray-400">star_rate</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Score Value</mat-label>
                <input matInput type="number" [(ngModel)]="scoreForm.scoreValue" min="0" />
                <mat-icon matPrefix class="text-gray-400">numbers</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full sm:col-span-2">
                <mat-label>Evidence</mat-label>
                <textarea matInput [(ngModel)]="scoreForm.evidence" rows="3"
                  placeholder="Evidence of competency demonstrated…"></textarea>
                <mat-icon matPrefix class="text-gray-400">fact_check</mat-icon>
              </mat-form-field>

              <div class="flex items-center gap-3 sm:col-span-2">
                <mat-slide-toggle color="primary" [(ngModel)]="scoreForm.isFinalized">
                  Finalized
                </mat-slide-toggle>
                <span class="text-xs text-gray-400 ml-2">Finalized scores cannot be edited</span>
              </div>

            </div>
          </mat-card-content>
        </mat-card>
      </ng-container>

      <fuse-alert *ngIf="!scoreForm.studentId && !editingScore" type="warning" appearance="soft" [showIcon]="true">
        Please select a student before saving.
      </fuse-alert>

    </div>

    <!-- Modal Footer -->
    <div class="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
      <button mat-stroked-button (click)="closeModal()">Cancel</button>
      <button mat-flat-button color="primary"
        [disabled]="isSavingScore || (!scoreForm.studentId && !editingScore)"
        (click)="saveScore()">
        <mat-progress-spinner *ngIf="isSavingScore" mode="indeterminate" diameter="18"
          strokeWidth="2" class="inline-block mr-2"></mat-progress-spinner>
        <mat-icon *ngIf="!isSavingScore" class="icon-size-4">
          {{ editingScore ? 'save' : 'add_circle' }}
        </mat-icon>
        <span class="ml-2">{{ isSavingScore ? 'Saving…' : (editingScore ? 'Update' : 'Save Score') }}</span>
      </button>
    </div>

  </div>
</div>
  `,
})
export class AssessmentGradesComponent implements OnInit, OnDestroy, AfterViewInit {

  @ViewChild('studentCellRef')  studentCellTpl!:  TemplateRef<any>;
  @ViewChild('scoreCellRef')    scoreCellTpl!:    TemplateRef<any>;
  @ViewChild('gradeCellRef')    gradeCellTpl!:    TemplateRef<any>;
  @ViewChild('feedbackCellRef') feedbackCellTpl!: TemplateRef<any>;
  @ViewChild('statusCellRef')   statusCellTpl!:   TemplateRef<any>;

  private _destroy$  = new Subject<void>();
  private _route     = inject(ActivatedRoute);
  private _router    = inject(Router);
  private _service   = inject(AssessmentService);
  private _alert     = inject(AlertService);
  private _reportSvc = inject(AssessmentReportService);

  readonly formativeType  = AssessmentType.Formative;
  readonly summativeType  = AssessmentType.Summative;
  readonly competencyType = AssessmentType.Competency;

  assessmentId!:   string;
  assessmentType!: AssessmentType;

  // FIX 2: AssessmentResponse (full DTO) — classId is a plain string here
  assessment: AssessmentResponse | null = null;

  scores:   AssessmentScoreResponse[] = [];
  students: any[] = [];

  isLoading     = false;
  isDownloading = false;
  isSavingScore = false;
  showModal     = false;
  editingScore: AssessmentScoreResponse | null = null;
  scoreForm:    Partial<UpsertScoreRequest> = {};

  currentPage  = 1;
  itemsPerPage = 20;

  studentSearch    = '';
  filteredStudents: any[] = [];

  modalFilters: Record<string, string> = { performanceLevel: '', rating: '' };

  private readonly ALL_PERFORMANCE_LEVELS = [
    { value: 'EE', label: 'EE – Exceeds Expectations'    },
    { value: 'ME', label: 'ME – Meets Expectations'      },
    { value: 'AE', label: 'AE – Approaches Expectations' },
    { value: 'BE', label: 'BE – Below Expectations'      },
  ];
  private readonly ALL_RATINGS = [...this.ALL_PERFORMANCE_LEVELS];

  modalFiltered: Record<string, any[]> = {
    performanceLevels: [...this.ALL_PERFORMANCE_LEVELS],
    ratings:           [...this.ALL_RATINGS],
  };

  cellTemplates: Record<string, TemplateRef<any>> = {};

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',   url: '/dashboard' },
    { label: 'Assessments', url: '/assessment/assessments' },
    { label: 'Grades' },
  ];

  get assessmentTitle(): string {
    return this.assessment?.title ? `Grades – ${this.assessment.title}` : 'Assessment Grades';
  }

  get typeLabel(): string { return getAssessmentTypeLabel(this.assessmentType); }

  get paginatedScores(): AssessmentScoreResponse[] {
    const s = (this.currentPage - 1) * this.itemsPerPage;
    return this.scores.slice(s, s + this.itemsPerPage);
  }

  get submittedCount(): number { return this.scores.filter(s => s.isSubmitted).length; }
  get passedCount():    number { return this.scores.filter(s => s.isPassed).length; }
  get finalizedCount(): number { return this.scores.filter(s => s.isFinalized).length; }

  get avgScore(): number | null {
    const vals = this.scores
      .map(s => s.score ?? s.totalScore)
      .filter((v): v is number => v != null);
    return vals.length ? vals.reduce((a, b) => a + b, 0) / vals.length : null;
  }

  get tableColumns(): TableColumn<AssessmentScoreResponse>[] {
    return [
      { id: 'student',  label: 'Student',  align: 'left', sortable: true },
      { id: 'score',    label: 'Score',    align: 'left'  },
      {
        id:    'grade',
        label: this.assessmentType === AssessmentType.Competency ? 'Level' : 'Grade',
        align: 'center',
      },
      { id: 'feedback', label: 'Feedback / Remarks', align: 'left', hideOnMobile: true },
      { id: 'status',   label: 'Status',  align: 'center' },
    ];
  }

  tableActions: TableAction<AssessmentScoreResponse>[] = [
    { id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'indigo', handler: s => this.openScoreModal(s) },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red',    handler: s => this.deleteScore(s)    },
  ];

  tableHeader: TableHeader = {
    title:        'Grades',
    subtitle:     '',
    icon:         'grading',
    iconGradient: 'bg-gradient-to-br from-teal-500 via-emerald-600 to-green-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'grade',
    message:     'No scores recorded yet',
    description: 'Click "Add Score" to record the first grade',
    action:      { label: 'Add Score', icon: 'add', handler: () => this.openScoreModal(null) },
  };

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    this.assessmentId   = this._route.snapshot.paramMap.get('id')!;
    const typeParam     = this._route.snapshot.queryParamMap.get('type');
    this.assessmentType = typeParam ? (Number(typeParam) as AssessmentType) : AssessmentType.Formative;
    this.loadData();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      student:  this.studentCellTpl,
      score:    this.scoreCellTpl,
      grade:    this.gradeCellTpl,
      feedback: this.feedbackCellTpl,
      status:   this.statusCellTpl,
    };
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data loading ───────────────────────────────────────────────
  // FIX 1: Use switchMap to chain assessment load → class-scoped student load.
  // No non-existent getStudentsByAssessment() call needed.
  private loadData(): void {
    this.isLoading = true;

    this._service.getById(this.assessmentId, this.assessmentType)
      .pipe(
        takeUntil(this._destroy$),
        catchError(() => of(null)),
        switchMap(assessment => {
          this.assessment = assessment;          // FIX 2: AssessmentResponse type
          const classId   = assessment?.classId; // plain string — no type error

          return forkJoin({
            scores:   this._service.getScores(this.assessmentId, this.assessmentType)
                        .pipe(catchError(() => of([]))),
            // Scope students to the assessment's class; classId may be undefined
            // (getStudents() accepts optional classId — existing service method)
            students: this._service.getStudents(classId ?? undefined)
                        .pipe(catchError(() => of([]))),
          });
        }),
        finalize(() => this.isLoading = false),
      )
      .subscribe(({ scores, students }) => {
        this.scores           = scores;
        this.students         = students;
        this.filteredStudents = [...students];
        this.tableHeader.subtitle =
          `${scores.length} score${scores.length !== 1 ? 's' : ''} recorded`;
      });
  }

  // ── Student search ─────────────────────────────────────────────
  filterStudents(): void {
    const q = this.studentSearch.toLowerCase();
    this.filteredStudents = q
      ? this.students.filter(s =>
          `${s.firstName} ${s.lastName}`.toLowerCase().includes(q) ||
          (s.admissionNo || '').toLowerCase().includes(q))
      : [...this.students];
  }

  // ── Modal static filter ────────────────────────────────────────
  filterModalStatic(key: string): void {
    const q = (this.modalFilters[key] || '').toLowerCase();
    const map: Record<string, { src: any[]; dest: string }> = {
      performanceLevel: { src: this.ALL_PERFORMANCE_LEVELS, dest: 'performanceLevels' },
      rating:           { src: this.ALL_RATINGS,            dest: 'ratings'           },
    };
    const m = map[key];
    if (!m) return;
    this.modalFiltered[m.dest] = q
      ? m.src.filter(o => o.label.toLowerCase().includes(q))
      : [...m.src];
  }

  // ── Modal ──────────────────────────────────────────────────────
  openScoreModal(score: AssessmentScoreResponse | null): void {
    this.editingScore     = score;
    this.studentSearch    = '';
    this.filteredStudents = [...this.students];

    this.scoreForm = score
      ? { ...score }
      : {
          assessmentId:       this.assessmentId,
          assessmentType:     this.assessmentType,
          isSubmitted:        false,
          isFinalized:        false,
          isPassed:           false,
          competencyAchieved: false,
        };

    this.modalFilters  = { performanceLevel: '', rating: '' };
    this.modalFiltered = {
      performanceLevels: [...this.ALL_PERFORMANCE_LEVELS],
      ratings:           [...this.ALL_RATINGS],
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal     = false;
    this.editingScore  = null;
    this.scoreForm     = {};
    this.studentSearch = '';
  }

  saveScore(): void {
    if (!this.scoreForm.studentId && !this.editingScore) {
      this._alert.error('Please select a student.'); return;
    }
    this.isSavingScore = true;

    // FIX 3: Spread scoreForm first, then override the three authoritative
    // fields exactly once — eliminates the TS1117 duplicate-key error.
    const request: UpsertScoreRequest = {
      ...this.scoreForm,
      assessmentId:   this.assessmentId,
      assessmentType: Number(this.assessmentType) as AssessmentType,
      studentId:      this.editingScore?.studentId ?? (this.scoreForm.studentId as string),
    } as UpsertScoreRequest;

    this._service.upsertScore(request)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isSavingScore = false))
      .subscribe({
        next:  () => {
          this._alert.success('Score saved successfully.');
          this.closeModal();
          this.loadData();
        },
        error: err => this._alert.error(err?.error?.message || 'Failed to save score'),
      });
  }

  deleteScore(score: AssessmentScoreResponse): void {
    this._alert.confirm({
      title:       'Delete Score',
      message:     `Delete the score for "${score.studentName}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => {
        this._service.deleteScore(score.id, Number(this.assessmentType) as AssessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alert.success('Score deleted'); this.loadData(); },
            error: err => this._alert.error(err?.error?.message || 'Failed to delete score'),
          });
      },
    });
  }

  downloadPdf(): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._reportSvc.downloadAssessmentGrades(this.assessmentId)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
      .subscribe({
        next:  r  => r.success ? this._alert.success('PDF downloaded') : this._alert.error(r.message ?? 'Error'),
        error: () => this._alert.error('Download failed'),
      });
  }

  getInitials(name: string): string {
    return (name || '?').split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
  }
}