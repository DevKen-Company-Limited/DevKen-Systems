// grade-enrollment/grade-enrollment.component.ts
import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { GradeService }  from 'app/core/DevKenService/GradeService/GradeService';
import { AuthService }   from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }     from 'app/Tenant/types/school';
import { GradeReviewStepComponent } from '../grade-review-step/grade-review-step.component';
import { GradeSettingsStepComponent } from '../grade-settings-step/grade-settings-step.component';
import { GradeSubjectStepComponent } from '../grade-subject-step/grade-subject-step.component';
import { GradeScoreStepComponent } from '../grade-score-step/grade-score-step.component';


export interface GradeEnrollmentStep {
  label:      string;
  icon:       string;
  sectionKey: string;
}

/** Resolve grade letter: numeric string "0"→0, or already a number */
function resolveGradeLetter(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  return isNaN(n) ? null : n;
}

/** Resolve grade type: numeric or string name */
function resolveGradeType(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n)) return n;
  const map: Record<string, number> = { formative: 0, summative: 1, competency: 2 };
  return map[String(val).toLowerCase()] ?? null;
}

@Component({
  selector: 'app-grade-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    GradeSubjectStepComponent,
    GradeScoreStepComponent,
    GradeSettingsStepComponent,
    GradeReviewStepComponent,
  ],
  templateUrl: './grade-enrollment.component.html',
  animations: [
    trigger('stepTransition', [
      transition(':increment', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in', style({ opacity: 0, transform: 'translateX(-40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))], { optional: true }),
        ]),
      ]),
      transition(':decrement', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(-40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in', style({ opacity: 0, transform: 'translateX(40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))], { optional: true }),
        ]),
      ]),
    ]),
  ],
})
export class GradeEnrollmentComponent implements OnInit, OnDestroy {

  // ─── State ──────────────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();
  gradeId:       string | null = null;
  isEditMode     = false;
  isSaving       = false;
  isSubmitting   = false;
  lastSaved:     Date | null = null;

  schools: SchoolDto[] = [];
  private destroy$ = new Subject<void>();

  // ─── Sidebar ────────────────────────────────────────────────────────────
  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  @HostListener('window:resize')
  onResize(): void { this.checkViewport(); }

  private checkViewport(): void {
    const w = window.innerWidth;
    this.isMobileView = w < 1024;
    if (w < 1280 && w >= 1024) this.isSidebarCollapsed = true;
    if (!this.isMobileView) this.showMobileSidebar = false;
  }

  toggleSidebar(): void {
    if (this.isMobileView) this.showMobileSidebar = !this.showMobileSidebar;
    else this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  // ─── Steps ──────────────────────────────────────────────────────────────
  steps: GradeEnrollmentStep[] = [
    { label: 'Student & Subject',  icon: 'person',        sectionKey: 'subject'   },
    { label: 'Score & Grade',      icon: 'grade',         sectionKey: 'score'     },
    { label: 'Status & Remarks',   icon: 'settings',      sectionKey: 'settings'  },
    { label: 'Review & Submit',    icon: 'check_circle',  sectionKey: 'review'    },
  ];

  // ─── Section validity ────────────────────────────────────────────────────
  sectionValid: Record<string, boolean> = {
    subject:  false,
    score:    true,   // score is optional
    settings: true,
  };

  // ─── Form data per section ───────────────────────────────────────────────
  formSections: Record<string, any> = {
    subject:  {},
    score:    { score: null, maximumScore: null, gradeLetter: null, gradeType: null },
    settings: { assessmentDate: new Date().toISOString(), remarks: '', isFinalized: false },
  };

  constructor(
    private alertService:  AlertService,
    private gradeService:  GradeService,
    private authService:   AuthService,
    private schoolService: SchoolService,
    private router:        Router,
    private route:         ActivatedRoute,
  ) {}

  get isSuperAdmin(): boolean {
    return this.authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.gradeId    = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.gradeId;

    if (this.isSuperAdmin) {
      this.schoolService.getAll()
        .pipe(takeUntil(this.destroy$))
        .subscribe(res => { this.schools = (res as any).data ?? []; });
    }

    if (this.gradeId) {
      this._loadExistingGrade(this.gradeId);
    } else {
      this._loadDraft();
    }

    this.checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load existing grade ─────────────────────────────────────────────────
  private _loadExistingGrade(id: string): void {
    this.gradeService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (grade: any) => {
          this._hydrateFromGrade(grade);
          this.steps.slice(0, 3).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => { this.sectionValid[k] = true; });
          this.alertService.info('Editing existing grade record');
        },
        error: err => this.alertService.error(err?.error?.message || 'Could not load grade data.'),
      });
  }

  private _hydrateFromGrade(g: any): void {
    this.formSections['subject'] = {
      studentId:    g.studentId    ?? '',
      subjectId:    g.subjectId    ?? '',
      termId:       g.termId       ?? null,
      assessmentId: g.assessmentId ?? null,
      ...(this.isSuperAdmin ? { tenantId: g.tenantId ?? g.schoolId ?? '' } : {}),
    };

    this.formSections['score'] = {
      score:        g.score        ?? null,
      maximumScore: g.maximumScore ?? null,
      gradeLetter:  resolveGradeLetter(g.gradeLetter),
      gradeType:    resolveGradeType(g.gradeType),
    };

    this.formSections['settings'] = {
      assessmentDate: g.assessmentDate ?? new Date().toISOString(),
      remarks:        g.remarks        ?? '',
      isFinalized:    g.isFinalized    ?? false,
    };
  }

  // ─── Draft ───────────────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'grade_enrollment_draft';

  private _loadDraft(): void {
    if (this.gradeId) return;
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections   = { ...this.formSections,   ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep    = draft.currentStep ?? 0;
      this.lastSaved      = draft.savedAt ? new Date(draft.savedAt) : null;
      this.alertService.info('Draft loaded. You can continue where you left off.');
    } catch { /* malformed */ }
  }

  private _persistDraft(): void {
    const draft = {
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    };
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify(draft));
    this.lastSaved = new Date();
  }

  private _clearDraft(): void { localStorage.removeItem(this.DRAFT_KEY); }

  // ─── Section events ──────────────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    this.formSections[section] = { ...this.formSections[section], ...data };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ─── Navigation ──────────────────────────────────────────────────────────
  navigateToStep(index: number): void {
    if (this.canNavigateTo(index)) {
      this.currentStep = index;
      if (this.isMobileView) this.showMobileSidebar = false;
    }
  }

  prevStep(): void { if (this.currentStep > 0) this.currentStep--; }

  nextStep(): void {
    if (!this.canProceed()) return;
    this.completedSteps.add(this.currentStep);
    if (this.currentStep < this.steps.length - 1) this.currentStep++;
    this._persistDraft();
  }

  saveDraft(): void {
    this.isSaving = true;
    this._persistDraft();
    setTimeout(() => {
      this.isSaving = false;
      this.alertService.success('Draft saved locally.');
    }, 500);
  }

  // ─── Submit ──────────────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;
    this.isSubmitting = true;
    try {
      const payload = this._buildPayload();

      if (this.gradeId) {
        // Edit: only score/type/date/remarks/finalized updatable
        const updatePayload = {
          score:          payload.score,
          maximumScore:   payload.maximumScore,
          gradeLetter:    payload.gradeLetter,
          gradeType:      payload.gradeType,
          assessmentDate: payload.assessmentDate,
          remarks:        payload.remarks,
          isFinalized:    payload.isFinalized,
        };
        await this.gradeService.update(this.gradeId, updatePayload).toPromise();
        this.alertService.success('Grade updated successfully!');
      } else {
        await this.gradeService.create(payload).toPromise();
        this.alertService.success('Grade created successfully!');
      }

      this._clearDraft();
      setTimeout(() => this.router.navigate(['/academic/grades']), 1500);
    } catch (err: any) {
      this.alertService.error(err?.error?.message || err?.error?.title || 'Submission failed. Please review and try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private _buildPayload(): any {
    const subject  = this.formSections['subject'];
    const score    = this.formSections['score'];
    const settings = this.formSections['settings'];

    const dateVal = settings.assessmentDate instanceof Date
      ? settings.assessmentDate.toISOString()
      : settings.assessmentDate;

    const payload: any = {
      studentId:      subject.studentId,
      subjectId:      subject.subjectId,
      termId:         subject.termId       || null,
      assessmentId:   subject.assessmentId || null,
      score:          score.score          !== null ? Number(score.score)       : null,
      maximumScore:   score.maximumScore   !== null ? Number(score.maximumScore): null,
      gradeLetter:    score.gradeLetter    !== null ? Number(score.gradeLetter)  : null,
      gradeType:      score.gradeType      !== null ? Number(score.gradeType)    : null,
      assessmentDate: dateVal,
      remarks:        settings.remarks?.trim() || null,
      isFinalized:    settings.isFinalized ?? false,
    };

    if (this.isSuperAdmin && subject.tenantId) {
      payload.tenantId = subject.tenantId;
    }

    return payload;
  }

  // ─── Guards ──────────────────────────────────────────────────────────────
  canProceed(): boolean {
    if (this.isEditMode) return true;
    const key = this.steps[this.currentStep]?.sectionKey;
    return this.sectionValid[key] !== false;
  }

  canNavigateTo(index: number): boolean {
    if (index === 0 || index <= this.currentStep) return true;
    if (this.isEditMode) return true;
    return this.completedSteps.has(index - 1);
  }

  isStepCompleted(index: number): boolean { return this.completedSteps.has(index); }

  allStepsCompleted(): boolean {
    if (this.isEditMode) return true;
    return this.steps.slice(0, 3).every((_, i) => this.completedSteps.has(i));
  }

  // ─── Progress ring ────────────────────────────────────────────────────────
  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const circumference = 2 * Math.PI * 56;
    return circumference * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  goBack(): void { this.router.navigate(['/academic/grades']); }
}