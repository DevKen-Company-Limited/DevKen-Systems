// ═══════════════════════════════════════════════════════════════════
// summative-assessment-form.component.ts  (Create / Edit)
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, HostListener,
} from '@angular/core';
import { CommonModule }          from '@angular/common';
import { FormsModule }           from '@angular/forms';
import { Router, ActivatedRoute }from '@angular/router';
import { MatButtonModule }       from '@angular/material/button';
import { MatIconModule }         from '@angular/material/icon';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError } from 'rxjs/operators';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

import { AlertService }               from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }                from 'app/core/auth/auth.service';
import { SummativeAssessmentService } from 'app/core/DevKenService/assessments/Summative/summative-assessment.service';
import { CreateSummativeAssessmentRequest, EXAM_TYPES } from 'app/assessment/types/summative-assessment.types';
import { SummativeAssessmentInfoComponent } from '../steps/summative-assessment-info.component';
import { SummativeAssessmentReviewComponent, SummativeAssessmentSettingsComponent } from '../steps/summative-assessment-settings.component';


interface FormStep { label: string; icon: string; sectionKey: string; }

@Component({
  selector: 'app-summative-assessment-form',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatIconModule,
    SummativeAssessmentInfoComponent,
    SummativeAssessmentSettingsComponent,
    SummativeAssessmentReviewComponent,
  ],
  templateUrl: './summative-assessment-form.component.html',
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
export class SummativeAssessmentFormComponent implements OnInit, OnDestroy {

  private _destroy$ = new Subject<void>();

  // ─── Mode ─────────────────────────────────────────────────────────
  assessmentId: string | null = null;
  isEditMode = false;

  // ─── Sidebar ──────────────────────────────────────────────────────
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

  // ─── Steps ────────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();

  steps: FormStep[] = [
    { label: 'Basic Information', icon: 'info',     sectionKey: 'info'     },
    { label: 'Exam Settings',     icon: 'settings', sectionKey: 'settings' },
    { label: 'Review & Save',     icon: 'check',    sectionKey: 'review'   },
  ];

  // ─── Section validity ─────────────────────────────────────────────
  sectionValid: Record<string, boolean> = {
    info:     false,
    settings: true,
  };

  // ─── Form data ────────────────────────────────────────────────────
  formSections: Record<string, any> = {
    info:     {},
    settings: {
      passMark: 50,
      theoryWeight: 100,
      practicalWeight: 0,
      hasPracticalComponent: false,
      numberOfQuestions: 0,
    },
  };

  // ─── Lookup data ──────────────────────────────────────────────────
  classes:       any[] = [];
  teachers:      any[] = [];
  subjects:      any[] = [];
  terms:         any[] = [];
  academicYears: any[] = [];
  schools:       any[] = [];
  examTypes      = EXAM_TYPES;

  // ─── Submission ───────────────────────────────────────────────────
  isSaving     = false;
  isSubmitting = false;

  constructor(
    private _service:     SummativeAssessmentService,
    private _router:      Router,
    private _route:       ActivatedRoute,
    private _alertService: AlertService,
    private _authService: AuthService,
  ) {}

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  ngOnInit(): void {
    this.assessmentId = this._route.snapshot.paramMap.get('id');
    this.isEditMode   = !!this.assessmentId;
    this.loadLookups();
    if (this.isEditMode) this.loadExisting();
    else this.loadDraft();
    this.checkViewport();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Lookups ──────────────────────────────────────────────────────
  private loadLookups(): void {
    forkJoin({
      classes:       this._service.getClasses().pipe(catchError(() => of([]))),
      teachers:      this._service.getTeachers().pipe(catchError(() => of([]))),
      subjects:      this._service.getSubjects().pipe(catchError(() => of([]))),
      terms:         this._service.getTerms().pipe(catchError(() => of([]))),
      academicYears: this._service.getAcademicYears().pipe(catchError(() => of([]))),
    })
    .pipe(takeUntil(this._destroy$))
    .subscribe(data => {
      this.classes       = data.classes;
      this.teachers      = data.teachers;
      this.subjects      = data.subjects;
      this.terms         = data.terms;
      this.academicYears = data.academicYears;
    });
  }

  // ─── Load existing for edit ───────────────────────────────────────
  private loadExisting(): void {
    this._service.getById(this.assessmentId!)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: a => {
          this.formSections.info = {
            title:          a.title,
            description:    a.description,
            assessmentDate: a.assessmentDate?.split('T')[0],
            maximumScore:   a.maximumScore,
            teacherId:      a.teacherId,
            subjectId:      a.subjectId,
            classId:        a.classId,
            termId:         a.termId,
            academicYearId: a.academicYearId,
            schoolId:       a.schoolId,
            examType:       a.examType,
          };
          this.formSections.settings = {
            passMark:              a.passMark,
            theoryWeight:          a.theoryWeight,
            practicalWeight:       a.practicalWeight,
            hasPracticalComponent: a.hasPracticalComponent,
            numberOfQuestions:     a.numberOfQuestions,
            duration:              a.duration,
            instructions:          a.instructions,
          };
          // Mark all steps as valid in edit mode
          this.steps.slice(0, 2).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => this.sectionValid[k] = true);
          this._alertService.info('Editing existing assessment');
        },
        error: () => this._alertService.error('Could not load assessment data'),
      });
  }

  // ─── Draft ────────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'summative_assessment_draft';

  private loadDraft(): void {
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const d = JSON.parse(raw);
      this.formSections  = { ...this.formSections, ...d.formSections };
      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep   = d.currentStep ?? 0;
      this._alertService.info('Draft restored. Continue where you left off.');
    } catch { /* ignore */ }
  }

  private persistDraft(): void {
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify({
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    }));
  }

  private clearDraft(): void { localStorage.removeItem(this.DRAFT_KEY); }

  // ─── Section events ───────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    this.formSections[section] = { ...this.formSections[section], ...data };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ─── Navigation ───────────────────────────────────────────────────
  navigateToStep(i: number): void {
    if (this.canNavigateTo(i)) {
      this.currentStep = i;
      if (this.isMobileView) this.showMobileSidebar = false;
    }
  }

  prevStep(): void { if (this.currentStep > 0) this.currentStep--; }

  nextStep(): void {
    if (!this.canProceed()) return;
    this.completedSteps.add(this.currentStep);
    if (this.currentStep < this.steps.length - 1) this.currentStep++;
    this.persistDraft();
  }

  saveDraft(): void {
    this.isSaving = true;
    this.persistDraft();
    setTimeout(() => {
      this.isSaving = false;
      this._alertService.success('Draft saved locally.');
    }, 400);
  }

  canProceed(): boolean {
    if (this.isEditMode) return true;
    const key = this.steps[this.currentStep]?.sectionKey;
    return this.sectionValid[key] !== false;
  }

  canNavigateTo(i: number): boolean {
    if (i === 0 || i <= this.currentStep || this.isEditMode) return true;
    return this.completedSteps.has(i - 1);
  }

  isStepCompleted(i: number): boolean { return this.completedSteps.has(i); }

  allStepsCompleted(): boolean {
    if (this.isEditMode) return true;
    return this.steps.slice(0, 2).every((_, i) => this.completedSteps.has(i));
  }

  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const c = 2 * Math.PI * 56;
    return c * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  // ─── Submit ───────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;
    this.isSubmitting = true;

    try {
      const payload = this.buildPayload();

      if (this.isEditMode) {
        await this._service.update(this.assessmentId!, payload).toPromise();
        this._alertService.success('Assessment updated successfully!');
      } else {
        await this._service.create(payload).toPromise();
        this._alertService.success('Assessment created successfully!');
      }

      this.clearDraft();
      setTimeout(() => this._router.navigate(['/assessment/assessments/summative']), 1200);
    } catch (err: any) {
      this._alertService.error(err?.error?.message || 'Submission failed. Please try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): CreateSummativeAssessmentRequest {
    const { info, settings } = this.formSections;
    return {
      title:                 info.title?.trim(),
      description:           info.description?.trim() || undefined,
      assessmentDate:        info.assessmentDate,
      maximumScore:          +info.maximumScore,
      teacherId:             info.teacherId  || undefined,
      subjectId:             info.subjectId  || undefined,
      classId:               info.classId    || undefined,
      termId:                info.termId     || undefined,
      academicYearId:        info.academicYearId || undefined,
      schoolId:              info.schoolId   || undefined,
      examType:              info.examType   || undefined,

      passMark:              +settings.passMark,
      theoryWeight:          +settings.theoryWeight,
      practicalWeight:       +settings.practicalWeight,
      hasPracticalComponent: !!settings.hasPracticalComponent,
      numberOfQuestions:     +settings.numberOfQuestions,
      duration:              settings.duration   || undefined,
      instructions:          settings.instructions?.trim() || undefined,
    };
  }

  goBack(): void { this._router.navigate(['/assessment/assessments/summative']); }
}