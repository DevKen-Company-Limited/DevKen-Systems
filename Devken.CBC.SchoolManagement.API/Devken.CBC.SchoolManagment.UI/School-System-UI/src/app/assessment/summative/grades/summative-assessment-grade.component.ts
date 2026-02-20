// ═══════════════════════════════════════════════════════════════════
// summative-assessment-grade.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }            from '@angular/common';
import { FormsModule }             from '@angular/forms';
import { Router, ActivatedRoute }  from '@angular/router';
import { MatIconModule }           from '@angular/material/icon';
import { MatButtonModule }         from '@angular/material/button';
import { MatTooltipModule }        from '@angular/material/tooltip';
import { MatProgressSpinnerModule }from '@angular/material/progress-spinner';
import { Subject, forkJoin, of }   from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';

import { AlertService }               from 'app/core/DevKenService/Alert/AlertService';
import { SummativeAssessmentReportService } from 'app/core/DevKenService/assessments/Summative/summative-assessment-report.service';
import { CreateSummativeAssessmentScoreRequest, PERFORMANCE_STATUS_COLORS, SummativeAssessmentDto, UpdateSummativeAssessmentScoreRequest } from 'app/assessment/types/summative-assessment.types';
import { SummativeAssessmentService } from 'app/core/DevKenService/assessments/Summative/summative-assessment.service';


interface GradeRow {
  studentId:    string;
  studentName:  string;
  existingScoreId?: string;
  theoryScore:  number | null;
  practicalScore: number | null;
  grade:        string;
  remarks:      string;
  comments:     string;
  isNew:        boolean;
  isDirty:      boolean;
  isSaving:     boolean;
  error:        string;
}

@Component({
  selector: 'app-summative-assessment-grade',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule, MatProgressSpinnerModule,
  ],
  templateUrl: './summative-assessment-grade.component.html',
})
export class SummativeAssessmentGradeComponent implements OnInit, OnDestroy {

  private _destroy$    = new Subject<void>();
  private _router      = inject(Router);
  private _route       = inject(ActivatedRoute);
  private _alert       = inject(AlertService);
  private _reportSvc   = inject(SummativeAssessmentReportService);

  assessmentId!:   string;
  assessment:      SummativeAssessmentDto | null = null;
  rows:            GradeRow[] = [];
  students:        any[] = [];

  isLoading        = false;
  isSavingAll      = false;
  isRecalculating  = false;
  isDownloading    = false;

  readonly PERFORMANCE_COLORS = PERFORMANCE_STATUS_COLORS;

  constructor(private _service: SummativeAssessmentService) {}

  ngOnInit(): void {
    this.assessmentId = this._route.snapshot.paramMap.get('id')!;
    this.loadData();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Load ─────────────────────────────────────────────────────────
  private loadData(): void {
    this.isLoading = true;
    forkJoin({
      assessment: this._service.getWithScores(this.assessmentId).pipe(catchError(() => of(null))),
    })
    .pipe(takeUntil(this._destroy$), finalize(() => this.isLoading = false))
    .subscribe(({ assessment }) => {
      if (!assessment) { this._alert.error('Assessment not found'); this.goBack(); return; }
      this.assessment = assessment;
      this.loadStudents();
    });
  }

  private loadStudents(): void {
    const classId = this.assessment?.classId;
    if (!classId) { this._alert.error('No class assigned to this assessment'); return; }

    this._service.getStudentsByClass(classId)
      .pipe(takeUntil(this._destroy$))
      .subscribe(students => {
        this.students = students;
        this.buildRows();
      });
  }

  private buildRows(): void {
    const existingScores = this.assessment?.scores ?? [];
    const scoreMap = new Map(existingScores.map(s => [s.studentId, s]));

    this.rows = this.students.map(student => {
      const existing = scoreMap.get(student.id);
      return {
        studentId:       student.id,
        studentName:     `${student.firstName} ${student.lastName}`,
        existingScoreId: existing?.id,
        theoryScore:     existing?.theoryScore    ?? null,
        practicalScore:  existing?.practicalScore ?? null,
        grade:           existing?.grade          ?? '',
        remarks:         existing?.remarks        ?? '',
        comments:        existing?.comments       ?? '',
        isNew:           !existing,
        isDirty:         false,
        isSaving:        false,
        error:           '',
      };
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────
  get hasPractical(): boolean { return this.assessment?.hasPracticalComponent ?? false; }
  get maxTheory():    number  { return this.hasPractical ? (this.assessment!.maximumScore * this.assessment!.theoryWeight / 100) : this.assessment?.maximumScore ?? 0; }
  get maxPractical(): number  { return this.hasPractical ? (this.assessment!.maximumScore * this.assessment!.practicalWeight / 100) : 0; }

  calcTotal(row: GradeRow): number {
    return (row.theoryScore ?? 0) + (this.hasPractical ? (row.practicalScore ?? 0) : 0);
  }

  calcPercent(row: GradeRow): number {
    const max = this.assessment?.maximumScore ?? 0;
    return max > 0 ? Math.round((this.calcTotal(row) / max) * 100) : 0;
  }

  isPassed(row: GradeRow): boolean {
    return this.calcPercent(row) >= (this.assessment?.passMark ?? 50);
  }

  performanceStatus(pct: number): string {
    if (pct >= 80) return 'Excellent';
    if (pct >= 70) return 'Very Good';
    if (pct >= 60) return 'Good';
    if (pct >= 50) return 'Average';
    if (pct >= 40) return 'Below Average';
    return 'Poor';
  }

  getStatusColor(pct: number): string {
    return PERFORMANCE_STATUS_COLORS[this.performanceStatus(pct)] ?? 'gray';
  }

  get dirtyCount(): number { return this.rows.filter(r => r.isDirty).length; }
  get savedCount():  number { return this.rows.filter(r => !r.isNew).length; }

  onRowChange(row: GradeRow): void { row.isDirty = true; row.error = ''; }

  // ─── Save individual row ──────────────────────────────────────────
  async saveRow(row: GradeRow): Promise<void> {
    if (!row.isDirty) return;
    if (row.theoryScore === null) { row.error = 'Theory score is required'; return; }

    row.isSaving = true;
    row.error    = '';

    try {
      if (row.isNew) {
        const req: CreateSummativeAssessmentScoreRequest = {
          summativeAssessmentId: this.assessmentId,
          studentId:             row.studentId,
          theoryScore:           +row.theoryScore,
          practicalScore:        this.hasPractical ? (row.practicalScore ?? undefined) : undefined,
          maximumTheoryScore:    this.maxTheory,
          maximumPracticalScore: this.hasPractical ? this.maxPractical : undefined,
          grade:                 row.grade    || undefined,
          remarks:               row.remarks  || undefined,
          comments:              row.comments || undefined,
        };
        const res = await this._service.createScore(req).toPromise();
        row.existingScoreId = res?.data?.id;
        row.isNew           = false;
      } else {
        const req: UpdateSummativeAssessmentScoreRequest = {
          theoryScore:           +row.theoryScore,
          practicalScore:        this.hasPractical ? (row.practicalScore ?? undefined) : undefined,
          maximumTheoryScore:    this.maxTheory,
          maximumPracticalScore: this.hasPractical ? this.maxPractical : undefined,
          grade:                 row.grade    || undefined,
          remarks:               row.remarks  || undefined,
          comments:              row.comments || undefined,
        };
        await this._service.updateScore(row.existingScoreId!, req).toPromise();
      }
      row.isDirty = false;
    } catch (err: any) {
      row.error = err?.error?.message || 'Save failed';
    } finally {
      row.isSaving = false;
    }
  }

  // ─── Save all ─────────────────────────────────────────────────────
  async saveAll(): Promise<void> {
    const dirty = this.rows.filter(r => r.isDirty && r.theoryScore !== null);
    if (!dirty.length) { this._alert.info('No changes to save'); return; }

    this.isSavingAll = true;
    let success = 0, failed = 0;

    for (const row of dirty) {
      await this.saveRow(row);
      row.error ? failed++ : success++;
    }

    this.isSavingAll = false;
    if (success) this._alert.success(`${success} score(s) saved successfully`);
    if (failed)  this._alert.error(`${failed} score(s) failed to save`);
  }

  // ─── Recalculate positions ────────────────────────────────────────
  recalculatePositions(): void {
    this.isRecalculating = true;
    this._service.recalculatePositions(this.assessmentId)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isRecalculating = false))
      .subscribe({
        next:  r  => { this._alert.success(r.message || 'Positions recalculated'); this.loadData(); },
        error: err => this._alert.error(err?.error?.message || 'Failed to recalculate'),
      });
  }

  // ─── Report ───────────────────────────────────────────────────────
  downloadReport(): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._reportSvc.downloadScoreSheet(this.assessmentId)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
      .subscribe({ next: () => {}, error: () => this._alert.error('Download failed') });
  }

  // ─── Delete score ─────────────────────────────────────────────────
  deleteScore(row: GradeRow): void {
    if (row.isNew) return;
    this._alert.confirm({
      title: 'Delete Score',
      message: `Delete score for ${row.studentName}?`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.deleteScore(row.existingScoreId!)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next: () => {
              row.isNew           = true;
              row.existingScoreId = undefined;
              row.theoryScore     = null;
              row.practicalScore  = null;
              row.grade           = '';
              row.remarks         = '';
              row.comments        = '';
              row.isDirty         = false;
              this._alert.success('Score deleted');
            },
            error: err => this._alert.error(err?.error?.message || 'Delete failed'),
          });
      },
    });
  }

  goBack(): void { this._router.navigate(['/assessments/summative']); }
}