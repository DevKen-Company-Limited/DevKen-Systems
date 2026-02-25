// ═══════════════════════════════════════════════════════════════════
// grades/assessment-grades.component.ts
// Type-aware grades table: Formative / Summative / Competency
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, inject, ViewChild, TemplateRef, AfterViewInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';

import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AssessmentReportService } from 'app/core/DevKenService/assessments/Assessments/AssessmentReportService';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { AssessmentType, AssessmentListItem, AssessmentScoreResponse, UpsertScoreRequest, getAssessmentTypeLabel } from 'app/assessment/types/assessments';


@Component({
  selector: 'app-assessment-grades',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule,
    PageHeaderComponent, DataTableComponent, PaginationComponent,
  ],
  template: `
<div class="absolute inset-0 flex min-w-0 flex-col overflow-y-auto">

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
        <mat-progress-spinner mode="indeterminate" diameter="18" strokeWidth="2" class="inline-block mr-2"></mat-progress-spinner>
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

  <div class="bg-card -mt-10 flex-auto rounded-t-2xl p-6 shadow sm:p-10">

    <!-- Assessment summary strip -->
    <div *ngIf="assessment" class="flex flex-wrap gap-4 mb-6 p-4 rounded-xl border"
      [ngClass]="{
        'bg-indigo-50 border-indigo-200 dark:bg-indigo-900/20 dark:border-indigo-800': assessmentType === formativeType,
        'bg-violet-50 border-violet-200 dark:bg-violet-900/20 dark:border-violet-800': assessmentType === summativeType,
        'bg-teal-50   border-teal-200   dark:bg-teal-900/20   dark:border-teal-800':   assessmentType === competencyType
      }">
      <div class="flex items-center gap-2">
        <mat-icon class="icon-size-5"
          [ngClass]="{
            'text-indigo-600': assessmentType === formativeType,
            'text-violet-600': assessmentType === summativeType,
            'text-teal-600':   assessmentType === competencyType
          }">assignment</mat-icon>
        <span class="font-semibold text-gray-900 dark:text-white">{{ assessment.title }}</span>
      </div>
      <span class="text-sm text-gray-500">{{ assessment.className }} · {{ assessment.termName }}</span>
      <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Max: <strong>{{ assessment.maximumScore }}</strong></span>
      <span class="text-sm text-gray-500">{{ scores.length }} submitted</span>
      <span class="ml-auto px-3 py-1 rounded-full text-xs font-semibold"
        [ngClass]="assessment.isPublished
          ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
          : 'bg-gray-100  text-gray-600  dark:bg-gray-800     dark:text-gray-400'">
        {{ assessment.isPublished ? 'Published' : 'Draft' }}
      </span>
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

<!-- ── Cell Templates ──────────────────────────────────────────── -->

<ng-template #studentCell let-row>
  <div class="flex items-center gap-3">
    <div class="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
      {{ getInitials(row.studentName) }}
    </div>
    <div>
      <p class="text-sm font-semibold text-gray-900 dark:text-white">{{ row.studentName }}</p>
      <p class="text-xs text-gray-500">{{ row.studentAdmissionNo }}</p>
    </div>
  </div>
</ng-template>

<ng-template #scoreCell let-row>
  <ng-container [ngSwitch]="assessmentType">
    <!-- Formative -->
    <ng-container *ngSwitchCase="formativeType">
      <div class="flex flex-col">
        <span class="font-bold text-indigo-600 dark:text-indigo-400">{{ row.score ?? '—' }} / {{ row.maximumScore ?? assessment?.maximumScore }}</span>
        <span *ngIf="row.percentage != null" class="text-xs text-gray-500">{{ row.percentage | number:'1.0-1' }}%</span>
      </div>
    </ng-container>
    <!-- Summative -->
    <ng-container *ngSwitchCase="summativeType">
      <div class="flex flex-col">
        <span class="font-bold text-violet-600 dark:text-violet-400">{{ row.totalScore ?? '—' }} / {{ row.maximumTotalScore ?? assessment?.maximumScore }}</span>
        <span *ngIf="row.theoryScore != null" class="text-xs text-gray-500">Theory: {{ row.theoryScore }} · Practical: {{ row.practicalScore ?? 0 }}</span>
      </div>
    </ng-container>
    <!-- Competency -->
    <ng-container *ngSwitchCase="competencyType">
      <span class="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-bold border bg-teal-50 text-teal-700 border-teal-200 dark:bg-teal-900/30 dark:text-teal-300">
        {{ row.rating || '—' }}
      </span>
    </ng-container>
  </ng-container>
</ng-template>

<ng-template #gradeCell let-row>
  <ng-container [ngSwitch]="assessmentType">
    <span *ngSwitchCase="formativeType" class="font-semibold text-gray-900 dark:text-white">{{ row.grade || '—' }}</span>
    <span *ngSwitchCase="summativeType" [ngClass]="row.isPassed ? 'text-green-600 font-semibold' : 'text-red-500 font-semibold'">
      {{ row.isPassed ? 'Pass' : 'Fail' }}
    </span>
    <span *ngSwitchCase="competencyType" class="text-sm text-gray-700 dark:text-gray-300">{{ row.competencyLevel || '—' }}</span>
  </ng-container>
</ng-template>

<ng-template #feedbackCell let-row>
  <p class="text-xs text-gray-500 dark:text-gray-400 max-w-xs truncate">
    {{ row.feedback || row.remarks || row.evidence || '—' }}
  </p>
</ng-template>

<ng-template #statusCell let-row>
  <ng-container [ngSwitch]="assessmentType">
    <span *ngSwitchCase="formativeType" class="px-2.5 py-1 rounded-full text-xs font-semibold"
      [ngClass]="row.isSubmitted ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'">
      {{ row.isSubmitted ? 'Submitted' : 'Pending' }}
    </span>
    <span *ngSwitchCase="summativeType" class="px-2.5 py-1 rounded-full text-xs font-semibold"
      [ngClass]="row.performanceStatus ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' : 'bg-gray-100 text-gray-600'">
      {{ row.performanceStatus || 'Recorded' }}
    </span>
    <span *ngSwitchCase="competencyType" class="px-2.5 py-1 rounded-full text-xs font-semibold"
      [ngClass]="row.isFinalized ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'">
      {{ row.isFinalized ? 'Finalized' : 'Draft' }}
    </span>
  </ng-container>
</ng-template>

<!-- ── Score Modal ─────────────────────────────────────────────── -->
<div *ngIf="showModal"
  class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
    <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
      <h3 class="text-lg font-bold text-gray-900 dark:text-white">
        {{ editingScore ? 'Edit Score' : 'Add Score' }}
      </h3>
      <button mat-icon-button (click)="closeModal()">
        <mat-icon>close</mat-icon>
      </button>
    </div>
    <div class="p-6 space-y-4">

      <!-- Student selector (new score only) -->
      <div *ngIf="!editingScore">
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          Student <span class="text-red-500">*</span>
        </label>
        <select [(ngModel)]="scoreForm.studentId"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none">
          <option value="">Select student</option>
          <option *ngFor="let s of students" [value]="s.id">{{ s.firstName }} {{ s.lastName }} ({{ s.admissionNo }})</option>
        </select>
      </div>
      <div *ngIf="editingScore" class="p-3 bg-gray-50 dark:bg-gray-700/50 rounded-lg">
        <p class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ editingScore.studentName }}</p>
        <p class="text-xs text-gray-400">{{ editingScore.studentAdmissionNo }}</p>
      </div>

      <!-- Formative score fields -->
      <ng-container *ngIf="assessmentType === formativeType">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Score</label>
            <input type="number" [(ngModel)]="scoreForm.score" min="0" [max]="assessment?.maximumScore"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Grade</label>
            <input type="text" [(ngModel)]="scoreForm.grade" placeholder="A, B+, etc."
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Performance Level</label>
            <select [(ngModel)]="scoreForm.performanceLevel"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none">
              <option value="">Select level</option>
              <option value="EE">Exceeds Expectations (EE)</option>
              <option value="ME">Meets Expectations (ME)</option>
              <option value="AE">Approaches Expectations (AE)</option>
              <option value="BE">Below Expectations (BE)</option>
            </select>
          </div>
          <div class="flex items-center gap-3 self-end pb-2">
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" [(ngModel)]="scoreForm.isSubmitted" class="sr-only peer" />
              <div class="w-11 h-6 bg-gray-200 peer-checked:bg-indigo-600 rounded-full peer peer-focus:ring-4 peer-focus:ring-indigo-300 after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:after:translate-x-full"></div>
              <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Submitted</span>
            </label>
          </div>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Feedback</label>
          <textarea [(ngModel)]="scoreForm.feedback" rows="2" placeholder="Feedback for student…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none resize-none">
          </textarea>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Strengths</label>
          <textarea [(ngModel)]="scoreForm.strengths" rows="2" placeholder="Areas of strength…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none resize-none">
          </textarea>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Areas for Improvement</label>
          <textarea [(ngModel)]="scoreForm.areasForImprovement" rows="2" placeholder="Areas to improve…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none resize-none">
          </textarea>
        </div>
        <div class="flex items-center gap-3">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" [(ngModel)]="scoreForm.competencyAchieved" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-checked:bg-indigo-600 rounded-full peer after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:after:translate-x-full"></div>
            <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Competency Achieved</span>
          </label>
        </div>
      </ng-container>

      <!-- Summative score fields -->
      <ng-container *ngIf="assessmentType === summativeType">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Theory Score</label>
            <input type="number" [(ngModel)]="scoreForm.theoryScore" min="0"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Practical Score</label>
            <input type="number" [(ngModel)]="scoreForm.practicalScore" min="0"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Position in Class</label>
            <input type="number" [(ngModel)]="scoreForm.positionInClass" min="1"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none" />
          </div>
          <div class="flex items-center gap-3 self-end pb-2">
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" [(ngModel)]="scoreForm.isPassed" class="sr-only peer" />
              <div class="w-11 h-6 bg-gray-200 peer-checked:bg-violet-600 rounded-full peer after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:after:translate-x-full"></div>
              <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Passed</span>
            </label>
          </div>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Remarks</label>
          <textarea [(ngModel)]="scoreForm.remarks" rows="2"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none resize-none">
          </textarea>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Comments</label>
          <textarea [(ngModel)]="scoreForm.comments" rows="2"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none resize-none">
          </textarea>
        </div>
      </ng-container>

      <!-- Competency score fields -->
      <ng-container *ngIf="assessmentType === competencyType">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Rating <span class="text-red-500">*</span></label>
            <select [(ngModel)]="scoreForm.rating"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none">
              <option value="">Select rating</option>
              <option value="EE">EE – Exceeds Expectations</option>
              <option value="ME">ME – Meets Expectations</option>
              <option value="AE">AE – Approaches Expectations</option>
              <option value="BE">BE – Below Expectations</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Score Value</label>
            <input type="number" [(ngModel)]="scoreForm.scoreValue" min="0"
              class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none" />
          </div>
          <div class="flex items-center gap-3 self-center">
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" [(ngModel)]="scoreForm.isFinalized" class="sr-only peer" />
              <div class="w-11 h-6 bg-gray-200 peer-checked:bg-teal-600 rounded-full peer after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:after:translate-x-full"></div>
              <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Finalized</span>
            </label>
          </div>
        </div>
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Evidence</label>
          <textarea [(ngModel)]="scoreForm.evidence" rows="3" placeholder="Evidence of competency…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none resize-none">
          </textarea>
        </div>
      </ng-container>

    </div>
    <div class="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
      <button mat-stroked-button (click)="closeModal()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="isSavingScore" (click)="saveScore()">
        {{ isSavingScore ? 'Saving…' : (editingScore ? 'Update' : 'Save') }}
      </button>
    </div>
  </div>
</div>

<!-- Score cell templates -->
<ng-template #scoreCellRef let-row>
  <ng-container [ngTemplateOutlet]="scoreCell" [ngTemplateOutletContext]="{ $implicit: row }"></ng-container>
</ng-template>
<ng-template #gradeCellRef let-row>
  <ng-container [ngTemplateOutlet]="gradeCell" [ngTemplateOutletContext]="{ $implicit: row }"></ng-container>
</ng-template>
<ng-template #feedbackCellRef let-row>
  <ng-container [ngTemplateOutlet]="feedbackCell" [ngTemplateOutletContext]="{ $implicit: row }"></ng-container>
</ng-template>
<ng-template #statusCellRef let-row>
  <ng-container [ngTemplateOutlet]="statusCell" [ngTemplateOutletContext]="{ $implicit: row }"></ng-container>
</ng-template>
<ng-template #studentCellRef let-row>
  <ng-container [ngTemplateOutlet]="studentCell" [ngTemplateOutletContext]="{ $implicit: row }"></ng-container>
</ng-template>
  `,
})
export class AssessmentGradesComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('studentCellRef') studentCellTpl!:  TemplateRef<any>;
  @ViewChild('scoreCellRef')   scoreCellTpl!:    TemplateRef<any>;
  @ViewChild('gradeCellRef')   gradeCellTpl!:    TemplateRef<any>;
  @ViewChild('feedbackCellRef') feedbackCellTpl!: TemplateRef<any>;
  @ViewChild('statusCellRef')  statusCellTpl!:   TemplateRef<any>;

  private _destroy$  = new Subject<void>();
  private _route     = inject(ActivatedRoute);
  private _router    = inject(Router);
  private _service   = inject(AssessmentService);
  private _alert     = inject(AlertService);
  private _reportSvc = inject(AssessmentReportService);

  assessmentId!:     string;
  assessmentType!:   AssessmentType;
  assessment:        AssessmentListItem | null = null;
  scores:            AssessmentScoreResponse[] = [];
  students:          any[] = [];
  isLoading          = false;
  isDownloading      = false;
  isSavingScore      = false;

  // Expose enum to template
  readonly formativeType  = AssessmentType.Formative;
  readonly summativeType  = AssessmentType.Summative;
  readonly competencyType = AssessmentType.Competency;

  // Pagination
  currentPage  = 1;
  itemsPerPage = 20;

  // Modal
  showModal    = false;
  editingScore: AssessmentScoreResponse | null = null;
  scoreForm:    Partial<UpsertScoreRequest> = {};

  // Table
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

  get tableColumns(): TableColumn<AssessmentScoreResponse>[] {
    const cols: TableColumn<AssessmentScoreResponse>[] = [
      { id: 'student',  label: 'Student',  align: 'left', sortable: true },
      { id: 'score',    label: 'Score',    align: 'left' },
      { id: 'grade',    label: this.assessmentType === AssessmentType.Competency ? 'Level' : 'Grade', align: 'center' },
      { id: 'feedback', label: 'Feedback / Remarks', align: 'left', hideOnMobile: true },
      { id: 'status',   label: 'Status',  align: 'center' },
    ];
    return cols;
  }

  tableActions: TableAction<AssessmentScoreResponse>[] = [
    {
      id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'indigo',
      handler: s => this.openScoreModal(s),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: s => this.deleteScore(s),
    },
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

  ngOnInit(): void {
    this.assessmentId   = this._route.snapshot.paramMap.get('id')!;
    const typeParam     = this._route.snapshot.queryParamMap.get('type');
    this.assessmentType = typeParam ? Number(typeParam) as AssessmentType : AssessmentType.Formative;
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

  private loadData(): void {
    this.isLoading = true;
    forkJoin({
      scores:   this._service.getScores(this.assessmentId, this.assessmentType).pipe(catchError(() => of([]))),
      students: this._service.getStudents().pipe(catchError(() => of([]))),
    })
    .pipe(takeUntil(this._destroy$), finalize(() => this.isLoading = false))
    .subscribe(({ scores, students }) => {
      this.scores   = scores;
      this.students = students;
      this.tableHeader.subtitle = `${scores.length} score${scores.length !== 1 ? 's' : ''} recorded`;
    });
  }

  // ── Modal ───────────────────────────────────────────────────────
  openScoreModal(score: AssessmentScoreResponse | null): void {
    this.editingScore = score;
    this.scoreForm    = score
      ? { ...score }
      : { assessmentId: this.assessmentId, assessmentType: this.assessmentType, isSubmitted: false, isFinalized: false, isPassed: false, competencyAchieved: false };
    this.showModal    = true;
  }

  closeModal(): void { this.showModal = false; this.editingScore = null; this.scoreForm = {}; }

  saveScore(): void {
    if (!this.scoreForm.studentId && !this.editingScore) {
      this._alert.error('Please select a student.'); return;
    }
    this.isSavingScore = true;
    const request: UpsertScoreRequest = {
      assessmentId:   this.assessmentId,
      assessmentType: this.assessmentType,
      studentId:      this.editingScore?.studentId ?? this.scoreForm.studentId!,
      ...this.scoreForm,
    };
    this._service.upsertScore(request)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isSavingScore = false))
      .subscribe({
        next:  () => { this._alert.success('Score saved successfully.'); this.closeModal(); this.loadData(); },
        error: err => this._alert.error(err?.error?.message || 'Failed to save score'),
      });
  }

  deleteScore(score: AssessmentScoreResponse): void {
    this._alert.confirm({
      title:       'Delete Score',
      message:     `Delete the score for "${score.studentName}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => {
        this._service.deleteScore(score.id, this.assessmentType)
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