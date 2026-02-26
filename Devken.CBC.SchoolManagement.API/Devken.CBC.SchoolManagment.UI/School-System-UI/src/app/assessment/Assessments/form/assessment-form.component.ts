// ═══════════════════════════════════════════════════════════════════
// assessment-form.component.ts  (Create / Edit)
//
// FIX: AssessmentDetailsStepComponent was missing from the imports[]
//      array, so app-assessment-details-step rendered as a blank
//      unknown element. Added to both the import statement and the
//      @Component imports array.
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, HostListener,
} from '@angular/core';
import { CommonModule }           from '@angular/common';
import { FormsModule }            from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule }        from '@angular/material/button';
import { MatIconModule }          from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule }       from '@angular/material/tooltip';
import { Subject, forkJoin, of, Observable } from 'rxjs';
import { takeUntil, catchError, tap, map } from 'rxjs/operators';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

import { AlertService }           from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }            from 'app/core/auth/auth.service';
import { AssessmentService }      from 'app/core/DevKenService/assessments/Assessments/AssessmentService';

// ── Step components ────────────────────────────────────────────────
import { AssessmentInfoStepComponent }    from '../steps/assessment-info-step.component';
// FIX: was missing — caused app-assessment-details-step to render blank
import { AssessmentDetailsStepComponent } from '../steps/assessment-details-step.component';
import { AssessmentReviewStepComponent }  from '../steps/assessment-review-step.component';

import {
  AssessmentType,
  CreateAssessmentRequest,
  UpdateAssessmentRequest,
} from 'app/assessment/types/assessments';

interface FormStep { label: string; icon: string; sectionKey: string; }

@Component({
  selector: 'app-assessment-form',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule,
    // ── Step components ──────────────────────────────────────────
    AssessmentInfoStepComponent,
    AssessmentDetailsStepComponent,   // FIX: was missing from imports[]
    AssessmentReviewStepComponent,
  ],
  templateUrl: './assessment-form.component.html',
  animations: [
    trigger('stepTransition', [
      transition(':increment', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',        style({ opacity: 0, transform: 'translateX(-40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)'     }))], { optional: true }),
        ]),
      ]),
      transition(':decrement', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(-40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',        style({ opacity: 0, transform: 'translateX(40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)'    }))], { optional: true }),
        ]),
      ]),
    ]),
  ],
})
export class AssessmentFormComponent implements OnInit, OnDestroy {

  private _destroy$ = new Subject<void>();

  // ─── Mode ──────────────────────────────────────────────────────
  assessmentId: string | null = null;
  isEditMode    = false;

  // ─── Sidebar ───────────────────────────────────────────────────
  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  @HostListener('window:resize') onResize(): void { this.checkViewport(); }

  private checkViewport(): void {
    const w = window.innerWidth;
    this.isMobileView      = w < 1024;
    if (w >= 1024 && w < 1280) this.isSidebarCollapsed = true;
    if (!this.isMobileView)    this.showMobileSidebar  = false;
  }

  toggleSidebar(): void {
    if (this.isMobileView) this.showMobileSidebar  = !this.showMobileSidebar;
    else                   this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  // ─── Steps ─────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();

  steps: FormStep[] = [
    { label: 'Basic Info',    icon: 'info',  sectionKey: 'info'    },
    { label: 'Details',       icon: 'tune',  sectionKey: 'details' },
    { label: 'Review & Save', icon: 'check', sectionKey: 'review'  },
  ];

  sectionValid: Record<string, boolean> = { info: false, details: true };

  formSections: Record<string, any> = {
    info: {},
    details: {
      assessmentType:     AssessmentType.Formative,
      maximumScore:       100,
      assessmentWeight:   100,
      theoryWeight:       100,
      passMark:           50,
      isObservationBased: true,
    },
  };

  // ─── Academic lookups ─────────────────────────────────────────
  classes:       any[] = [];
  teachers:      any[] = [];
  subjects:      any[] = [];
  terms:         any[] = [];
  academicYears: any[] = [];
  schools:       any[] = [];

  /** activeSchoolId — SuperAdmin: passed to details step as [schoolId] */
  activeSchoolId: string | undefined = undefined;

  isSaving         = false;
  isSubmitting     = false;
  isLoadingLookups = false;

  constructor(
    private _service:      AssessmentService,
    private _router:       Router,
    private _route:        ActivatedRoute,
    private _alertService: AlertService,
    private _authService:  AuthService,
  ) {}

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  // ─── Lifecycle ─────────────────────────────────────────────────
  ngOnInit(): void {
    this.assessmentId = this._route.snapshot.paramMap.get('id');
    this.isEditMode   = !!this.assessmentId;
    this.checkViewport();

    if (this.isSuperAdmin) {
      this._loadSuperAdminBase()
        .pipe(takeUntil(this._destroy$))
        .subscribe(() => {
          if (this.isEditMode) this._loadExisting();
          else                 this._loadDraft();
        });
    } else {
      this._loadAllLookups()
        .pipe(takeUntil(this._destroy$))
        .subscribe(() => {
          if (this.isEditMode) this._loadExisting();
          else                 this._loadDraft();
        });
    }
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Lookup helpers ────────────────────────────────────────────

  private _loadSuperAdminBase(): Observable<void> {
    this.isLoadingLookups = true;
    return this._service.getSchools().pipe(
      catchError(() => of([])),
      tap(schools => {
        this.schools          = schools;
        this.isLoadingLookups = false;
      }),
      map(() => void 0 as void),
    );
  }

  private _loadAllLookups(schoolId?: string): Observable<void> {
    this.isLoadingLookups = true;
    return forkJoin({
      classes:       this._service.getClasses(schoolId).pipe(catchError(() => of([]))),
      teachers:      this._service.getTeachers(schoolId).pipe(catchError(() => of([]))),
      subjects:      this._service.getSubjects(schoolId).pipe(catchError(() => of([]))),
      terms:         this._service.getTerms(schoolId).pipe(catchError(() => of([]))),
      academicYears: this._service.getAcademicYears(schoolId).pipe(catchError(() => of([]))),
      schools:       this._service.getSchools().pipe(catchError(() => of([]))),
    }).pipe(
      tap(d => {
        this.classes          = d.classes;
        this.teachers         = d.teachers;
        this.subjects         = d.subjects;
        this.terms            = d.terms;
        this.academicYears    = d.academicYears;
        this.schools          = d.schools;
        this.isLoadingLookups = false;
      }),
      map(() => void 0 as void),
    );
  }

  private _loadSchoolScopedLookups(schoolId: string): Observable<void> {
    this.isLoadingLookups = true;
    return forkJoin({
      classes:       this._service.getClasses(schoolId).pipe(catchError(() => of([]))),
      teachers:      this._service.getTeachers(schoolId).pipe(catchError(() => of([]))),
      subjects:      this._service.getSubjects(schoolId).pipe(catchError(() => of([]))),
      terms:         this._service.getTerms(schoolId).pipe(catchError(() => of([]))),
      academicYears: this._service.getAcademicYears(schoolId).pipe(catchError(() => of([]))),
    }).pipe(
      tap(d => {
        this.classes          = d.classes;
        this.teachers         = d.teachers;
        this.subjects         = d.subjects;
        this.terms            = d.terms;
        this.academicYears    = d.academicYears;
        this.isLoadingLookups = false;
      }),
      map(() => void 0 as void),
    );
  }

  onSchoolChanged(schoolId: string): void {
    this.activeSchoolId = schoolId || undefined;
    if (!schoolId) {
      this.classes = []; this.teachers = []; this.subjects = [];
      this.terms   = []; this.academicYears = [];
      return;
    }
    this._loadSchoolScopedLookups(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe();
  }

  // ─── Load existing (Edit mode) ──────────────────────────────────
  private _loadExisting(): void {
    const typeParam      = this._route.snapshot.queryParamMap.get('type');
    const assessmentType = typeParam
      ? (Number(typeParam) as AssessmentType)
      : AssessmentType.Formative;

    this._service.getById(this.assessmentId!, assessmentType)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: a => {
          if (this.isSuperAdmin && a.schoolId) {
            this.activeSchoolId = a.schoolId;
            this._loadSchoolScopedLookups(a.schoolId)
              .pipe(takeUntil(this._destroy$))
              .subscribe(() => this._populateFormSections(a));
          } else {
            this._populateFormSections(a);
          }
        },
        error: () => this._alertService.error('Could not load assessment data.'),
      });
  }

  private _populateFormSections(a: any): void {
    this.formSections = {
      ...this.formSections,
      info: {
        title:          a.title          ?? '',
        description:    a.description    ?? '',
        assessmentDate: a.assessmentDate ? a.assessmentDate.split('T')[0] : '',
        teacherId:      a.teacherId      ?? '',
        subjectId:      a.subjectId      ?? '',
        classId:        a.classId        ?? '',
        termId:         a.termId         ?? '',
        academicYearId: a.academicYearId ?? '',
        schoolId:       a.schoolId       ?? '',
      },
      details: {
        assessmentType:          Number(a.assessmentType),
        maximumScore:            a.maximumScore            ?? 100,
        formativeType:           a.formativeType           ?? '',
        competencyArea:          a.competencyArea          ?? '',
        strandId:                a.strandId                ?? '',
        subStrandId:             a.subStrandId             ?? '',
        learningOutcomeId:       a.learningOutcomeId       ?? '',
        criteria:                a.criteria                ?? '',
        feedbackTemplate:        a.feedbackTemplate        ?? '',
        requiresRubric:          a.requiresRubric          ?? false,
        assessmentWeight:        a.assessmentWeight        ?? 100,
        formativeInstructions:   a.formativeInstructions   ?? '',
        examType:                a.examType                ?? '',
        duration:                a.duration                ?? '',
        numberOfQuestions:       a.numberOfQuestions       ?? null,
        passMark:                a.passMark                ?? 50,
        hasPracticalComponent:   a.hasPracticalComponent   ?? false,
        practicalWeight:         a.practicalWeight         ?? 0,
        theoryWeight:            a.theoryWeight            ?? 100,
        summativeInstructions:   a.summativeInstructions   ?? '',
        competencyName:          a.competencyName          ?? '',
        competencyStrand:        a.competencyStrand        ?? '',
        competencySubStrand:     a.competencySubStrand     ?? '',
        targetLevel:             a.targetLevel      != null ? Number(a.targetLevel)      : null,
        performanceIndicators:   a.performanceIndicators   ?? '',
        assessmentMethod:        a.assessmentMethod != null ? Number(a.assessmentMethod) : null,
        ratingScale:             a.ratingScale             ?? '',
        isObservationBased:      a.isObservationBased      ?? true,
        toolsRequired:           a.toolsRequired           ?? '',
        competencyInstructions:  a.competencyInstructions  ?? '',
        specificLearningOutcome: a.specificLearningOutcome ?? '',
      },
    };

    this.steps.slice(0, 2).forEach((_, i) => this.completedSteps.add(i));
    Object.keys(this.sectionValid).forEach(k => this.sectionValid[k] = true);
  }

  // ─── Draft ─────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'assessment_form_draft';

  private _loadDraft(): void {
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const d           = JSON.parse(raw);
      this.formSections = { ...this.formSections, ...d.formSections };

      const dt = this.formSections.details;
      if (typeof dt.assessmentType === 'string') {
        const m: Record<string, AssessmentType> = {
          Formative:  AssessmentType.Formative,
          Summative:  AssessmentType.Summative,
          Competency: AssessmentType.Competency,
        };
        dt.assessmentType = m[dt.assessmentType] ?? AssessmentType.Formative;
      }
      if (dt.targetLevel      != null) dt.targetLevel      = Number(dt.targetLevel);
      if (dt.assessmentMethod != null) dt.assessmentMethod = Number(dt.assessmentMethod);

      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep    = d.currentStep ?? 0;

      const draftSchoolId = this.formSections.info?.schoolId;
      if (this.isSuperAdmin && draftSchoolId) {
        this.activeSchoolId = draftSchoolId;
        this._loadSchoolScopedLookups(draftSchoolId)
          .pipe(takeUntil(this._destroy$))
          .subscribe();
      }

      this._alertService.info('Draft restored. Continue where you left off.');
    } catch { /* ignore corrupt drafts */ }
  }

  private _persistDraft(): void {
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify({
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    }));
  }

  private _clearDraft(): void { localStorage.removeItem(this.DRAFT_KEY); }

  // ─── Section events ─────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    this.formSections = {
      ...this.formSections,
      [section]: { ...this.formSections[section], ...data },
    };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ─── Navigation ─────────────────────────────────────────────────
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
    this._persistDraft();
  }

  saveDraft(): void {
    this.isSaving = true;
    this._persistDraft();
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

  isStepCompleted(i: number):  boolean { return this.completedSteps.has(i); }

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

  // ─── Submit ─────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;
    this.isSubmitting = true;
    try {
      const payload = this._buildPayload();
      if (this.isEditMode) {
        const updatePayload: UpdateAssessmentRequest = { id: this.assessmentId!, ...payload };
        await this._service.update(this.assessmentId!, updatePayload).toPromise();
        this._alertService.success('Assessment updated successfully!');
      } else {
        await this._service.create(payload).toPromise();
        this._alertService.success('Assessment created successfully!');
      }
      this._clearDraft();
      setTimeout(() => this._router.navigate(['/assessment/assessments']), 1200);
    } catch (err: any) {
      this._alertService.error(err?.error?.message || 'Submission failed. Please try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  // ─── Build Payload ───────────────────────────────────────────────
  private _buildPayload(): CreateAssessmentRequest {
    const { info, details } = this.formSections;
    const typeNum: AssessmentType = Number(details.assessmentType) as AssessmentType;

    const shared: Partial<CreateAssessmentRequest> = {
      assessmentType: typeNum,
      title:          info.title?.trim(),
      description:    info.description?.trim() || undefined,
      assessmentDate: info.assessmentDate,
      teacherId:      info.teacherId        || undefined,
      subjectId:      info.subjectId        || undefined,
      classId:        info.classId          || undefined,
      termId:         info.termId           || undefined,
      academicYearId: info.academicYearId   || undefined,
      schoolId:       info.schoolId         || undefined,
      maximumScore:   +details.maximumScore,
    };

    if (typeNum === AssessmentType.Formative) {
      return {
        ...shared,
        formativeType:         details.formativeType          || undefined,
        competencyArea:        details.competencyArea         || undefined,
        strandId:              details.strandId               || undefined,
        subStrandId:           details.subStrandId            || undefined,
        learningOutcomeId:     details.learningOutcomeId      || undefined,
        criteria:              details.criteria               || undefined,
        feedbackTemplate:      details.feedbackTemplate       || undefined,
        requiresRubric:        !!details.requiresRubric,
        assessmentWeight:      details.assessmentWeight != null ? +details.assessmentWeight : 100,
        formativeInstructions: details.formativeInstructions  || undefined,
      } as CreateAssessmentRequest;
    }

    if (typeNum === AssessmentType.Summative) {
      return {
        ...shared,
        examType:              details.examType               || undefined,
        duration:              details.duration               || undefined,
        numberOfQuestions:     details.numberOfQuestions != null ? +details.numberOfQuestions : undefined,
        passMark:              details.passMark       != null ? +details.passMark             : 50,
        hasPracticalComponent: !!details.hasPracticalComponent,
        practicalWeight:       details.practicalWeight != null ? +details.practicalWeight     : 0,
        theoryWeight:          details.theoryWeight   != null ? +details.theoryWeight         : 100,
        summativeInstructions: details.summativeInstructions  || undefined,
      } as CreateAssessmentRequest;
    }

    if (typeNum === AssessmentType.Competency) {
      return {
        ...shared,
        competencyName:          details.competencyName?.trim()     || undefined,
        competencyStrand:        details.competencyStrand           || undefined,
        competencySubStrand:     details.competencySubStrand        || undefined,
        targetLevel:             details.targetLevel     != null ? Number(details.targetLevel)     : undefined,
        performanceIndicators:   details.performanceIndicators      || undefined,
        assessmentMethod:        details.assessmentMethod != null ? Number(details.assessmentMethod) : undefined,
        ratingScale:             details.ratingScale                || undefined,
        isObservationBased:      details.isObservationBased != null ? !!details.isObservationBased : true,
        toolsRequired:           details.toolsRequired              || undefined,
        competencyInstructions:  details.competencyInstructions     || undefined,
        specificLearningOutcome: details.specificLearningOutcome    || undefined,
      } as CreateAssessmentRequest;
    }

    return shared as CreateAssessmentRequest;
  }

  goBack(): void { this._router.navigate(['/assessment/assessments']); }
}