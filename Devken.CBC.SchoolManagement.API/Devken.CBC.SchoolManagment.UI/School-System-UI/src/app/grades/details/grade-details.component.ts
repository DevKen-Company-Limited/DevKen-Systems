// details/grade-details.component.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }                         from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatIconModule }                        from '@angular/material/icon';
import { MatButtonModule }                      from '@angular/material/button';
import { MatProgressSpinnerModule }             from '@angular/material/progress-spinner';
import { MatDividerModule }                     from '@angular/material/divider';
import { MatTooltipModule }                     from '@angular/material/tooltip';
import { MatMenuModule }                        from '@angular/material/menu';
import { Subject }                              from 'rxjs';
import { takeUntil, catchError, finalize }      from 'rxjs/operators';
import { of }                                   from 'rxjs';

import { AlertService }    from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { GradeService }    from 'app/core/DevKenService/GradeService/GradeService';
import { GradeDto } from '../types/gradedto';
import { getGradeLetterLabel, getGradeTypeLabel, getPerformanceLabel } from '../types/GradeEnums';

interface DetailItem {
  label:     string;
  value:     string | number | boolean | undefined | null;
  icon?:     string;
  copyable?: boolean;
  type?:     'text' | 'badge' | 'status' | 'boolean' | 'date' | 'percent';
}

@Component({
  selector: 'app-grade-details',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule,
    MatDividerModule, MatTooltipModule, MatMenuModule,
    PageHeaderComponent,
  ],
  templateUrl: './grade-details.component.html',
})
export class GradeDetailsComponent implements OnInit, OnDestroy {

  private _destroy$     = new Subject<void>();
  private _route        = inject(ActivatedRoute);
  private _router       = inject(Router);
  private _service      = inject(GradeService);
  private _alertService = inject(AlertService);

  grade:    GradeDto | null = null;
  isLoading = true;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard'       },
    { label: 'Academic',  url: '/academic'         },
    { label: 'Grades',    url: '/academic/grades'  },
    { label: 'Details' },
  ];

  getGradeLetterName  = getGradeLetterLabel;
  getGradeTypeName    = getGradeTypeLabel;
  getPerformanceName  = getPerformanceLabel;

  ngOnInit():    void { this._loadGrade(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private _loadGrade(): void {
    const id = this._route.snapshot.paramMap.get('id');
    if (!id) {
      this._alertService.error('Invalid grade ID');
      this._router.navigate(['/academic/grades']);
      return;
    }

    this.isLoading = true;
    this._service.getById(id)
      .pipe(
        takeUntil(this._destroy$),
        catchError(err => {
          this._alertService.error(err.error?.message || 'Failed to load grade details');
          this._router.navigate(['/academic/grades']);
          return of(null as any);
        }),
        finalize(() => { this.isLoading = false; }),
      )
      .subscribe(grade => {
        if (!grade) return;
        this.grade = grade;
        this.breadcrumbs[3] = { label: `${grade.studentName || 'Grade'} — ${grade.subjectName || ''}` };
      });
  }

  // ─── Detail Items ─────────────────────────────────────────────────────────
  get detailItems(): DetailItem[] {
    if (!this.grade) return [];
    return [
      { label: 'Student',          value: this.grade.studentName,                      icon: 'person',      type: 'badge' },
      { label: 'Subject',          value: this.grade.subjectName,                      icon: 'menu_book',   type: 'badge' },
      { label: 'School',           value: this.grade.schoolName,                       icon: 'school' },
      { label: 'Term',             value: this.grade.termName || '—',                  icon: 'date_range' },
      { label: 'Score',            value: this.grade.score !== null
          ? `${this.grade.score} / ${this.grade.maximumScore ?? '?'}`
          : '—',                                                                        icon: 'numbers' },
      { label: 'Percentage',       value: this.grade.percentage !== null
          ? `${this.grade.percentage}%` : '—',                                          icon: 'percent' },
      { label: 'Performance',      value: getPerformanceLabel(this.grade.percentage),  icon: 'trending_up' },
      { label: 'Grade Letter',     value: getGradeLetterLabel(this.grade.gradeLetter), icon: 'star' },
      { label: 'Assessment Type',  value: getGradeTypeLabel(this.grade.gradeType),     icon: 'category' },
      { label: 'Assessment Date',  value: this.formatDate(this.grade.assessmentDate),  icon: 'event',  type: 'date' },
      { label: 'Finalized',        value: this.grade.isFinalized ? 'Yes' : 'No',       icon: 'lock',   type: 'boolean' },
      { label: 'Status',           value: this.grade.isFinalized ? 'Finalized' : 'Pending',
                                                                                        icon: 'info',   type: 'status'  },
      { label: 'Remarks',          value: this.grade.remarks,                          icon: 'notes' },
      { label: 'Date Created',     value: this.formatDate(this.grade.createdOn),       icon: 'event',  type: 'date' },
      { label: 'Last Updated',     value: this.formatDate(this.grade.updatedOn),       icon: 'update', type: 'date' },
    ];
  }

  trackByLabel(index: number, item: DetailItem): string { return item.label; }

  formatDate(val: string | null | undefined): string {
    if (!val) return '—';
    try {
      const d = new Date(val);
      return isNaN(d.getTime()) ? '—' :
        d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
    } catch { return '—'; }
  }

  copyToClipboard(val: any): void {
    if (!val) return;
    navigator.clipboard.writeText(val.toString()).then(
      () => this._alertService.success('Copied to clipboard'),
      () => this._alertService.error('Failed to copy'),
    );
  }

  // ─── Actions ─────────────────────────────────────────────────────────────
  editGrade(): void {
    if (this.grade) this._router.navigate(['/academic/grades/edit', this.grade.id]);
  }

  finalizeGrade(): void {
    if (!this.grade || this.grade.isFinalized) return;
    this._alertService.confirm({
      title:       'Finalize Grade',
      message:     `Finalize this grade for "${this.grade.studentName}"? This cannot be undone.`,
      confirmText: 'Finalize',
      cancelText:  'Cancel',
      onConfirm:   () => {
        this._service.finalize(this.grade!.id)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next: () => { this._alertService.success('Grade finalized'); this._loadGrade(); },
            error: err => this._alertService.error(err.error?.message || 'Failed to finalize'),
          });
      },
    });
  }

  deleteGrade(): void {
    if (!this.grade || this.grade.isFinalized) return;
    this._alertService.confirm({
      title:       'Delete Grade',
      message:     `Delete this grade for "${this.grade.studentName}" in "${this.grade.subjectName}"? This cannot be undone.`,
      confirmText: 'Delete',
      cancelText:  'Cancel',
      onConfirm:   () => {
        this._service.delete(this.grade!.id)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next: () => {
              this._alertService.success('Grade deleted');
              this._router.navigate(['/academic/grades']);
            },
            error: err => this._alertService.error(err.error?.message || 'Failed to delete grade'),
          });
      },
    });
  }

  goBack(): void { this._router.navigate(['/academic/grades']); }
}