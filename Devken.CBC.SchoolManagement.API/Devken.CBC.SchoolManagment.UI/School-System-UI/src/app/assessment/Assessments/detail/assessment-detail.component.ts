// ═══════════════════════════════════════════════════════════════════
// detail/assessment-detail.component.ts
// Full detail view for Formative, Summative, Competency assessments
// ═══════════════════════════════════════════════════════════════════

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AssessmentReportService } from 'app/core/DevKenService/assessments/Assessments/AssessmentReportService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { AssessmentResponse, AssessmentType, getAssessmentTypeLabel } from 'app/assessment/types/assessments';

@Component({
  selector: 'app-assessment-detail',
  standalone: true,
  imports: [
    CommonModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatDividerModule,
    PageHeaderComponent,
  ],
  template: `
<!-- Page Header -->
<div class="absolute inset-0 flex min-w-0 flex-col overflow-y-auto">
  <app-page-header
    [title]="assessment?.title || 'Assessment Details'"
    description="Full assessment information"
    icon="assignment"
    [breadcrumbs]="breadcrumbs">
  </app-page-header>

  <div class="bg-card -mt-10 flex-auto rounded-t-2xl p-6 shadow sm:p-10">

    <!-- Loading -->
    <div *ngIf="isLoading" class="flex items-center justify-center py-24">
      <mat-progress-spinner mode="indeterminate" diameter="48"></mat-progress-spinner>
    </div>

    <ng-container *ngIf="!isLoading && assessment">

      <!-- ── Header actions ──────────────────────────────────────── -->
      <div class="flex flex-wrap items-center justify-between gap-3 mb-8">
        <!-- Type badge -->
        <span class="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-bold border"
          [ngClass]="typeBadgeClass">
          <mat-icon class="icon-size-4">{{ typeIcon }}</mat-icon>
          {{ typeLabel }}
        </span>
        <!-- Actions -->
        <div class="flex gap-2 flex-wrap">
          <button mat-stroked-button (click)="editAssessment()"
            class="border-indigo-200 text-indigo-700 hover:bg-indigo-50">
            <mat-icon class="icon-size-4">edit</mat-icon>
            <span class="ml-2">Edit</span>
          </button>
          <button mat-stroked-button (click)="viewGrades()"
            class="border-teal-200 text-teal-700 hover:bg-teal-50">
            <mat-icon class="icon-size-4">grading</mat-icon>
            <span class="ml-2">View Grades</span>
          </button>
          <button mat-stroked-button [disabled]="isDownloading" (click)="downloadPdf()"
            class="border-green-200 text-green-700 hover:bg-green-50">
            <mat-icon class="icon-size-4">download</mat-icon>
            <span class="ml-2">PDF Report</span>
          </button>
          <button mat-flat-button (click)="togglePublish()"
            [ngClass]="assessment.isPublished
              ? 'bg-amber-100 text-amber-800 hover:bg-amber-200'
              : 'bg-green-600 text-white hover:bg-green-700'">
            <mat-icon class="icon-size-4">{{ assessment.isPublished ? 'unpublished' : 'publish' }}</mat-icon>
            <span class="ml-2">{{ assessment.isPublished ? 'Unpublish' : 'Publish' }}</span>
          </button>
        </div>
      </div>

      <!-- ── Grid Layout ──────────────────────────────────────────── -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

        <!-- LEFT: Main info -->
        <div class="lg:col-span-2 space-y-6">

          <!-- Basic Info Card -->
          <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 overflow-hidden shadow-sm">
            <div class="flex items-center gap-3 px-6 py-4 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
              <mat-icon class="text-indigo-600">info</mat-icon>
              <h3 class="font-semibold text-gray-900 dark:text-white">Basic Information</h3>
            </div>
            <div class="p-6 grid grid-cols-1 sm:grid-cols-2 gap-5">
              <div *ngFor="let f of basicFields">
                <p class="text-xs text-gray-400 mb-1">{{ f.label }}</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white">{{ f.value || '—' }}</p>
              </div>
              <div *ngIf="assessment.description" class="sm:col-span-2">
                <p class="text-xs text-gray-400 mb-1">Description</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white leading-relaxed">{{ assessment.description }}</p>
              </div>
            </div>
          </div>

          <!-- Type-specific Card -->
          <div class="bg-white dark:bg-gray-800 rounded-2xl border overflow-hidden shadow-sm"
            [ngClass]="{
              'border-indigo-200 dark:border-indigo-800': assessment.assessmentType === formativeType,
              'border-violet-200 dark:border-violet-800': assessment.assessmentType === summativeType,
              'border-teal-200   dark:border-teal-800':   assessment.assessmentType === competencyType
            }">
            <div class="flex items-center gap-3 px-6 py-4 border-b border-gray-100 dark:border-gray-700"
              [ngClass]="{
                'bg-indigo-50 dark:bg-indigo-900/20': assessment.assessmentType === formativeType,
                'bg-violet-50 dark:bg-violet-900/20': assessment.assessmentType === summativeType,
                'bg-teal-50   dark:bg-teal-900/20':   assessment.assessmentType === competencyType
              }">
              <mat-icon [ngClass]="{
                'text-indigo-600': assessment.assessmentType === formativeType,
                'text-violet-600': assessment.assessmentType === summativeType,
                'text-teal-600':   assessment.assessmentType === competencyType
              }">{{ typeIcon }}</mat-icon>
              <h3 class="font-semibold text-gray-900 dark:text-white">{{ typeLabel }} Details</h3>
            </div>
            <div class="p-6 grid grid-cols-1 sm:grid-cols-2 gap-5">
              <div *ngFor="let f of typeSpecificFields">
                <p class="text-xs text-gray-400 mb-1">{{ f.label }}</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white">{{ f.value ?? '—' }}</p>
              </div>
            </div>
          </div>

        </div>

        <!-- RIGHT: Stats / Meta -->
        <div class="space-y-6">

          <!-- Stats Card -->
          <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
            <div class="flex items-center gap-3 px-6 py-4 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
              <mat-icon class="text-violet-600">bar_chart</mat-icon>
              <h3 class="font-semibold text-gray-900 dark:text-white">Statistics</h3>
            </div>
            <div class="p-6 space-y-4">
              <div class="flex items-center justify-between">
                <span class="text-sm text-gray-500">Max Score</span>
                <span class="text-xl font-bold text-indigo-600">{{ assessment.maximumScore }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-sm text-gray-500">Submissions</span>
                <span class="text-xl font-bold text-gray-900 dark:text-white">{{ assessment.scoreCount }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-sm text-gray-500">Status</span>
                <span class="px-3 py-1 rounded-full text-xs font-semibold"
                  [ngClass]="assessment.isPublished
                    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                    : 'bg-gray-100  text-gray-600  dark:bg-gray-800     dark:text-gray-400'">
                  {{ assessment.isPublished ? 'Published' : 'Draft' }}
                </span>
              </div>
            </div>
          </div>

          <!-- Meta Card -->
          <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
            <div class="flex items-center gap-3 px-6 py-4 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
              <mat-icon class="text-gray-500">schedule</mat-icon>
              <h3 class="font-semibold text-gray-900 dark:text-white">Timeline</h3>
            </div>
            <div class="p-6 space-y-3">
              <div>
                <p class="text-xs text-gray-400 mb-0.5">Assessment Date</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white">{{ assessment.assessmentDate | date:'fullDate' }}</p>
              </div>
              <div>
                <p class="text-xs text-gray-400 mb-0.5">Created</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white">{{ assessment.createdOn | date:'mediumDate' }}</p>
              </div>
              <div *ngIf="assessment.publishedDate">
                <p class="text-xs text-gray-400 mb-0.5">Published</p>
                <p class="text-sm font-medium text-gray-900 dark:text-white">{{ assessment.publishedDate | date:'mediumDate' }}</p>
              </div>
            </div>
          </div>

        </div>
      </div>

    </ng-container>

    <!-- Error state -->
    <div *ngIf="!isLoading && !assessment" class="flex flex-col items-center justify-center py-24 gap-4">
      <mat-icon class="text-gray-300 dark:text-gray-600" style="font-size:64px;width:64px;height:64px">assignment_late</mat-icon>
      <p class="text-gray-500 dark:text-gray-400">Assessment not found or could not be loaded.</p>
      <button mat-flat-button color="primary" (click)="goBack()">Back to Assessments</button>
    </div>

  </div>
</div>
  `,
})
export class AssessmentDetailComponent implements OnInit, OnDestroy {
  private _destroy$  = new Subject<void>();
  private _route     = inject(ActivatedRoute);
  private _router    = inject(Router);
  private _service   = inject(AssessmentService);
  private _alert     = inject(AlertService);
  private _reportSvc = inject(AssessmentReportService);

  assessment:   AssessmentResponse | null = null;
  isLoading     = false;
  isDownloading = false;

  // Expose enum values to template
  readonly formativeType  = AssessmentType.Formative;
  readonly summativeType  = AssessmentType.Summative;
  readonly competencyType = AssessmentType.Competency;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',    url: '/dashboard' },
    { label: 'Assessments',  url: '/assessment/assessments' },
    { label: 'Details' },
  ];

  ngOnInit(): void {
    const id         = this._route.snapshot.paramMap.get('id')!;
    const typeParam  = this._route.snapshot.queryParamMap.get('type');
    const type       = typeParam ? Number(typeParam) as AssessmentType : AssessmentType.Formative;
    this.loadAssessment(id, type);
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private loadAssessment(id: string, type: AssessmentType): void {
    this.isLoading = true;
    this._service.getById(id, type)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next:  a  => { this.assessment = a; this.isLoading = false; },
        error: () => { this.isLoading = false; this._alert.error('Failed to load assessment details.'); },
      });
  }

  // ── Template helpers ────────────────────────────────────────────
  get typeLabel(): string {
    return this.assessment ? getAssessmentTypeLabel(this.assessment.assessmentType) : '';
  }

  get typeIcon(): string {
    if (!this.assessment) return 'assignment';
    switch (this.assessment.assessmentType) {
      case AssessmentType.Formative:  return 'edit_note';
      case AssessmentType.Summative:  return 'fact_check';
      case AssessmentType.Competency: return 'verified_user';
      default: return 'assignment';
    }
  }

  get typeBadgeClass(): string {
    if (!this.assessment) return '';
    switch (this.assessment.assessmentType) {
      case AssessmentType.Formative:  return 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-900/30 dark:text-indigo-300';
      case AssessmentType.Summative:  return 'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-900/30 dark:text-violet-300';
      case AssessmentType.Competency: return 'bg-teal-50   text-teal-700   border-teal-200   dark:bg-teal-900/30  dark:text-teal-300';
      default: return '';
    }
  }

  get basicFields(): { label: string; value: any }[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    return [
      { label: 'Title',          value: a.title },
      { label: 'Assessment Date', value: a.assessmentDate ? new Date(a.assessmentDate).toLocaleDateString('en-US', { dateStyle: 'long' }) : '' },
      { label: 'Teacher',        value: a.teacherName },
      { label: 'Subject',        value: a.subjectName },
      { label: 'Class',          value: a.className },
      { label: 'Term',           value: a.termName },
      { label: 'Academic Year',  value: a.academicYearName },
    ];
  }

  get typeSpecificFields(): { label: string; value: any }[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    switch (a.assessmentType) {
      case AssessmentType.Formative:
        return [
          { label: 'Formative Type',     value: a.formativeType },
          { label: 'Competency Area',    value: a.competencyArea },
          { label: 'Strand',             value: a.strandName },
          { label: 'Sub-Strand',         value: a.subStrandName },
          { label: 'Learning Outcome',   value: a.learningOutcomeName },
          { label: 'Weight (%)',         value: a.assessmentWeight },
          { label: 'Requires Rubric',    value: a.requiresRubric ? 'Yes' : 'No' },
          { label: 'Criteria',           value: a.criteria },
        ].filter(f => f.value != null && f.value !== '');

      case AssessmentType.Summative:
        return [
          { label: 'Exam Type',      value: a.examType },
          { label: 'Duration',       value: a.duration },
          { label: 'Questions',      value: a.numberOfQuestions },
          { label: 'Pass Mark (%)',  value: a.passMark },
          { label: 'Theory Weight',  value: a.theoryWeight != null ? `${a.theoryWeight}%` : null },
          { label: 'Practical',      value: a.hasPracticalComponent ? `Yes (${a.practicalWeight}%)` : 'No' },
        ].filter(f => f.value != null && f.value !== '');

      case AssessmentType.Competency:
        return [
          { label: 'Competency Name',  value: a.competencyName },
          { label: 'Strand',           value: a.competencyStrand },
          { label: 'Sub-Strand',       value: a.competencySubStrand },
          { label: 'CBC Level',        value: a.targetLevel as string },
          { label: 'Rating Scale',     value: a.ratingScale },
          { label: 'Observation Based', value: a.isObservationBased ? 'Yes' : 'No' },
          { label: 'Tools Required',   value: a.toolsRequired },
          { label: 'Performance Indicators', value: a.performanceIndicators },
        ].filter(f => f.value != null && f.value !== '');

      default: return [];
    }
  }

  // ── Actions ─────────────────────────────────────────────────────
  editAssessment(): void {
    this._router.navigate(['/assessment/assessments/edit', this.assessment!.id], {
      queryParams: { type: this.assessment!.assessmentType },
    });
  }

  viewGrades(): void {
    this._router.navigate(['/assessment/assessments/grades', this.assessment!.id], {
      queryParams: { type: this.assessment!.assessmentType },
    });
  }

  downloadPdf(): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._reportSvc.downloadAssessmentGrades(this.assessment!.id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next:  r  => { r.success ? this._alert.success('PDF downloaded') : this._alert.error(r.message ?? 'Error'); this.isDownloading = false; },
        error: () => { this._alert.error('Download failed'); this.isDownloading = false; },
      });
  }

  togglePublish(): void {
    this._service.publish(this.assessment!.id, this.assessment!.assessmentType)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: r => {
          this._alert.success(r.message);
          this.assessment!.isPublished = !this.assessment!.isPublished;
        },
        error: err => this._alert.error(err.error?.message || 'Failed to update publish status'),
      });
  }

  goBack(): void { this._router.navigate(['/assessment/assessments']); }
}