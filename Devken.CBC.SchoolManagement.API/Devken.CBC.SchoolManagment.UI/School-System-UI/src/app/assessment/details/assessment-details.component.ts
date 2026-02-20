// details/assessment-details.component.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }                  from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatIconModule }                 from '@angular/material/icon';
import { MatButtonModule }               from '@angular/material/button';
import { MatProgressSpinnerModule }      from '@angular/material/progress-spinner';
import { MatTooltipModule }              from '@angular/material/tooltip';
import { MatMenuModule }                 from '@angular/material/menu';
import { MatDividerModule }              from '@angular/material/divider';
import { Subject }                       from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';
import { of }                            from 'rxjs';

import { AlertService }    from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import {
  AssessmentResponse,
  AssessmentType,
  getAssessmentTypeLabel,
  getAssessmentTypeColor,
  getAssessmentTypeIcon,
  getAssessmentMethodLabel,
  AssessmentTypeOptions,
} from '../types/AssessmentDtos';

interface DetailItem {
  label:     string;
  value:     any;
  icon?:     string;
  copyable?: boolean;
  type?:     'text' | 'badge' | 'status' | 'boolean' | 'date' | 'score';
}

@Component({
  selector: 'app-assessment-details',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule,
    MatMenuModule, MatDividerModule,
    PageHeaderComponent,
  ],
  templateUrl: './assessment-details.component.html',
})
export class AssessmentDetailsComponent implements OnInit, OnDestroy {

  private _destroy$     = new Subject<void>();
  private _route        = inject(ActivatedRoute);
  private _router       = inject(Router);
  private _service      = inject(AssessmentService);
  private _alertService = inject(AlertService);

  assessment: AssessmentResponse | null = null;
  isLoading  = true;
  assessmentType!: AssessmentType;

  readonly AssessmentType = AssessmentType;
  getTypeName   = getAssessmentTypeLabel;
  getTypeColor  = getAssessmentTypeColor;
  getTypeIcon   = getAssessmentTypeIcon;
  getMethodName = getAssessmentMethodLabel;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',    url: '/dashboard'            },
    { label: 'Academic',     url: '/academic'             },
    { label: 'Assessments',  url: '/academic/assessments' },
    { label: 'Details'                                    },
  ];

  get typeOption() {
    return AssessmentTypeOptions.find(o => o.value === this.assessmentType);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    const id   = this._route.snapshot.paramMap.get('id');
    const type = Number(this._route.snapshot.queryParamMap.get('type')) as AssessmentType;

    if (!id || !type) {
      this._alertService.error('Invalid assessment ID or type');
      this._router.navigate(['/academic/assessments']);
      return;
    }

    this.assessmentType = type;
    this._loadAssessment(id, type);
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  private _loadAssessment(id: string, type: AssessmentType): void {
    this.isLoading = true;
    this._service.getById(id, type)
      .pipe(
        takeUntil(this._destroy$),
        catchError(err => {
          this._alertService.error(err?.error?.message || 'Failed to load assessment details');
          this._router.navigate(['/academic/assessments']);
          return of(null as any);
        }),
        finalize(() => { this.isLoading = false; })
      )
      .subscribe(a => {
        if (!a) return;
        this.assessment = a;
        this.breadcrumbs[3] = { label: a.title || 'Details' };
      });
  }

  // ── Detail Sections ───────────────────────────────────────────────────────
  get coreDetailItems(): DetailItem[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    const items: DetailItem[] = [
      { label: 'Title',           value: a.title,                                               icon: 'title',              type: 'badge'  },
      { label: 'Teacher',         value: a.teacherName,                                          icon: 'person',             type: 'text'   },
      { label: 'Subject',         value: a.subjectName,                                          icon: 'menu_book',          type: 'text'   },
      { label: 'Class',           value: a.className,                                            icon: 'class',              type: 'text'   },
      { label: 'Term',            value: a.termName,                                             icon: 'date_range',         type: 'text'   },
      { label: 'Academic Year',   value: a.academicYearName,                                     icon: 'school',             type: 'text'   },
      { label: 'Maximum Score',   value: a.maximumScore,                                         icon: 'grade',              type: 'score'  },
      { label: 'Assessment Date', value: this.formatDate(a.assessmentDate),                      icon: 'event',              type: 'date'   },
      { label: 'Status',          value: a.isPublished ? 'Published' : 'Draft',                  icon: 'public',             type: 'status' },
      { label: 'Published On',    value: a.publishedDate ? this.formatDate(a.publishedDate) : '—', icon: 'event_available', type: 'date'   },
      { label: 'Score Entries',   value: `${a.scoreCount} student(s) scored`,                    icon: 'assignment_turned_in', type: 'text' },
      { label: 'Created On',      value: this.formatDate(a.createdOn),                           icon: 'history',            type: 'date'   },
    ];
    return items;
  }

  get formativeDetailItems(): DetailItem[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    const items: DetailItem[] = [
      { label: 'Formative Type',    value: a.formativeType,                                              icon: 'assignment',   type: 'text'    },
      { label: 'Competency Area',   value: a.competencyArea,                                             icon: 'hub',          type: 'text'    },
      { label: 'Learning Outcome',  value: a.learningOutcomeName,                                        icon: 'flag',         type: 'text'    },
      { label: 'Strand',            value: a.formativeStrand,                                            icon: 'account_tree', type: 'text'    },
      { label: 'Sub-Strand',        value: a.formativeSubStrand,                                         icon: 'device_hub',   type: 'text'    },
      { label: 'Assessment Weight', value: a.assessmentWeight ? `${a.assessmentWeight}%` : '—',          icon: 'percent',      type: 'text'    },
      { label: 'Requires Rubric',   value: a.requiresRubric ? 'Yes' : 'No',                              icon: 'table_chart',  type: 'boolean' },
      { label: 'Criteria',          value: a.criteria,                                                   icon: 'checklist',    type: 'text'    },
      { label: 'Feedback Template', value: a.feedbackTemplate,                                           icon: 'rate_review',  type: 'text'    },
      { label: 'Instructions',      value: a.formativeInstructions,                                      icon: 'notes',        type: 'text'    },
    ];
    return items.filter(i => i.value && i.value !== '—');
  }

  get summativeDetailItems(): DetailItem[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    const items: DetailItem[] = [
      { label: 'Exam Type',        value: a.examType,                                                        icon: 'assignment_late',       type: 'text'    },
      { label: 'Duration',         value: a.duration,                                                         icon: 'timer',                 type: 'text'    },
      { label: 'Questions',        value: a.numberOfQuestions,                                                 icon: 'format_list_numbered',  type: 'text'    },
      { label: 'Pass Mark',        value: a.passMark ? `${a.passMark}%` : '—',                                icon: 'percent',               type: 'text'    },
      { label: 'Has Practical',    value: a.hasPracticalComponent ? 'Yes' : 'No',                             icon: 'science',               type: 'boolean' },
      { label: 'Practical Weight', value: a.hasPracticalComponent ? `${a.practicalWeight}%` : '—',           icon: 'percent',               type: 'text'    },
      { label: 'Theory Weight',    value: a.hasPracticalComponent ? `${a.theoryWeight}%` : '—',              icon: 'percent',               type: 'text'    },
      { label: 'Instructions',     value: a.summativeInstructions,                                            icon: 'notes',                 type: 'text'    },
    ];
    return items.filter(i => i.value && i.value !== '—');
  }

  get competencyDetailItems(): DetailItem[] {
    if (!this.assessment) return [];
    const a = this.assessment;
    const items: DetailItem[] = [
      { label: 'Competency Name',   value: a.competencyName,                                                   icon: 'verified_user', type: 'badge'   },
      { label: 'CBC Strand',        value: a.competencyStrand,                                                  icon: 'account_tree',  type: 'text'    },
      { label: 'CBC Sub-Strand',    value: a.competencySubStrand,                                               icon: 'device_hub',    type: 'text'    },
      { label: 'Assessment Method', value: a.assessmentMethod ? this.getMethodName(a.assessmentMethod) : '—',  icon: 'visibility',    type: 'text'    },
      { label: 'Rating Scale',      value: a.ratingScale,                                                       icon: 'stars',         type: 'text'    },
      { label: 'Observation Based', value: a.isObservationBased ? 'Yes' : 'No',                                 icon: 'eye',           type: 'boolean' },
      { label: 'Tools Required',    value: a.toolsRequired,                                                     icon: 'build',         type: 'text'    },
      { label: 'SLO',               value: a.specificLearningOutcome,                                           icon: 'flag',          type: 'text'    },
      { label: 'Perf. Indicators',  value: a.performanceIndicators,                                             icon: 'checklist',     type: 'text'    },
      { label: 'Instructions',      value: a.competencyInstructions,                                            icon: 'notes',         type: 'text'    },
    ];
    return items.filter(i => i.value && i.value !== '—');
  }

  get typeSpecificItems(): DetailItem[] {
    if (!this.assessment) return [];
    switch (this.assessmentType) {
      case AssessmentType.Formative:  return this.formativeDetailItems;
      case AssessmentType.Summative:  return this.summativeDetailItems;
      case AssessmentType.Competency: return this.competencyDetailItems;
      default: return [];
    }
  }

  trackByLabel(index: number, item: DetailItem): string { return item.label; }

  formatDate(val: string | Date | undefined | null): string {
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

  // ── Actions ───────────────────────────────────────────────────────────────
  editAssessment(): void {
    if (this.assessment) {
      this._router.navigate(['/academic/assessments/edit', this.assessment.id], {
        queryParams: { type: this.assessmentType },
      });
    }
  }

  goToScores(): void {
    if (this.assessment) {
      this._router.navigate(['/academic/assessments/scores', this.assessment.id], {
        queryParams: { type: this.assessmentType },
      });
    }
  }

  publishAssessment(): void {
    if (!this.assessment) return;
    this._alertService.confirm({
      title:       'Publish Assessment',
      message:     `Publish "${this.assessment.title}"? It will be visible to teachers and students.`,
      confirmText: 'Publish',
      onConfirm:   () => {
        this._service.publish(this.assessment!.id, this.assessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next: () => {
              this._alertService.success('Assessment published successfully');
              this._loadAssessment(this.assessment!.id, this.assessmentType);
            },
            error: e => this._alertService.error(e?.error?.message || 'Failed to publish'),
          });
      },
    });
  }

  deleteAssessment(): void {
    if (!this.assessment) return;
    this._alertService.confirm({
      title:       'Delete Assessment',
      message:     `Delete "${this.assessment.title}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => {
        this._service.delete(this.assessment!.id, this.assessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next: () => {
              this._alertService.success('Assessment deleted');
              this._router.navigate(['/academic/assessments']);
            },
            error: e => this._alertService.error(e?.error?.message || 'Failed to delete'),
          });
      },
    });
  }

  goBack(): void { this._router.navigate(['/academic/assessments']); }
}