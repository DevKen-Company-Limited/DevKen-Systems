// assessment-enrollment/assessment-enrollment.component.ts
import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AlertService }                    from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }                     from 'app/core/auth/auth.service';
import { AssessmentIdentityStepComponent } from '../assessment-identity-step/assessment-identity-step.component';
import { AssessmentDetailsStepComponent }  from '../assessment-details-step/assessment-details-step.component';
import {
  AssessmentReviewStepComponent,
  AssessmentEnrollmentStep,
} from '../assessment-review-step/assessment-review-step.component';
import { AssessmentTypeStepComponent } from '../steps/assessment-type-step.component';
import { AssessmentService }           from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AssessmentType }              from '../types/AssessmentDtos';

@Component({
  selector: 'app-assessment-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    AssessmentTypeStepComponent,
    AssessmentIdentityStepComponent,
    AssessmentDetailsStepComponent,
    AssessmentReviewStepComponent,
  ],
  templateUrl: './assessment-enrollment.component.html',
  animations: [
    trigger('stepTransition', [
      transition(':increment', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(40px)' })],  { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',       style({ opacity: 0, transform: 'translateX(-40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)'    }))], { optional: true }),
        ]),
      ]),
      transition(':decrement', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(-40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',       style({ opacity: 0, transform: 'translateX(40px)'  }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)'    }))], { optional: true }),
        ]),
      ]),
    ]),
  ],
})
export class AssessmentEnrollmentComponent implements OnInit, OnDestroy {

  // ── State ──────────────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();
  assessmentId:  string | null = null;
  isEditMode     = false;
  isSaving       = false;
  isSubmitting   = false;
  lastSaved:     Date | null = null;

  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  private destroy$ = new Subject<void>();

  @HostListener('window:resize')
  onResize(): void { this._checkViewport(); }

  // ── Steps ──────────────────────────────────────────────────────────────
  steps: AssessmentEnrollmentStep[] = [
    { label: 'Assessment Type', icon: 'assignment',   sectionKey: 'type'     },
    { label: 'Core Details',    icon: 'info',         sectionKey: 'identity' },
    { label: 'Type Settings',   icon: 'tune',         sectionKey: 'details'  },
    { label: 'Review & Submit', icon: 'check_circle', sectionKey: 'review'   },
  ];

  // ── Section validity ───────────────────────────────────────────────────
  sectionValid: Record<string, boolean> = {
    type:     false,
    identity: false,
    details:  true,   // optional — always valid
  };

  // ── Section data ───────────────────────────────────────────────────────
  formSections: Record<string, any> = {
    type:     { assessmentType: null },
    identity: {},
    details:  {
      isObservationBased: true, assessmentWeight: 100,
      passMark: 50, theoryWeight: 100, practicalWeight: 0,
    },
  };

  get currentAssessmentType(): AssessmentType {
    return this.formSections['type']?.assessmentType ?? AssessmentType.Formative;
  }

  get stepGradient(): string {
    const t = this.currentAssessmentType;
    if (t === AssessmentType.Summative)  return 'from-violet-500 to-purple-600';
    if (t === AssessmentType.Competency) return 'from-teal-500 to-emerald-600';
    return 'from-indigo-500 to-blue-600';
  }

  constructor(
    private service:      AssessmentService,
    private alertService: AlertService,
    private authService:  AuthService,
    private router:       Router,
    private route:        ActivatedRoute,
  ) {}

  get isSuperAdmin(): boolean {
    return this.authService.authUser?.isSuperAdmin ?? false;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.assessmentId = this.route.snapshot.paramMap.get('id');
    const typeParam   = this.route.snapshot.queryParamMap.get('type');
    this.isEditMode   = !!this.assessmentId;

    if (this.assessmentId && typeParam) {
      this._loadExisting(this.assessmentId, Number(typeParam) as AssessmentType);
    } else {
      this._loadDraft();
    }

    this._checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private _checkViewport(): void {
    const w = window.innerWidth;
    this.isMobileView = w < 1024;
    if (w < 1280 && w >= 1024) this.isSidebarCollapsed = true;
    if (!this.isMobileView) this.showMobileSidebar = false;
  }

  toggleSidebar(): void {
    if (this.isMobileView) this.showMobileSidebar = !this.showMobileSidebar;
    else this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  // ── Load existing assessment ───────────────────────────────────────────
  private _loadExisting(id: string, type: AssessmentType): void {
    this.service.getById(id, type)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (a: any) => {
          this.formSections['type'] = { assessmentType: a.assessmentType ?? type };

          // Identity section — include schoolId so SuperAdmin picker is pre-selected
          this.formSections['identity'] = {
            schoolId:      a.schoolId      ?? null,   // ← SuperAdmin field
            title:         a.title,
            description:   a.description,
            teacherId:     a.teacherId,
            subjectId:     a.subjectId,
            classId:       a.classId,
            termId:        a.termId,
            academicYearId: a.academicYearId,
            assessmentDate: a.assessmentDate,
            maximumScore:  a.maximumScore,
            isPublished:   a.isPublished,
          };

          this.formSections['details'] = {
            // Formative
            formativeType: a.formativeType, competencyArea: a.competencyArea,
            learningOutcomeId: a.learningOutcomeId,
            formativeStrand: a.formativeStrand, formativeSubStrand: a.formativeSubStrand,
            criteria: a.criteria, feedbackTemplate: a.feedbackTemplate,
            requiresRubric: a.requiresRubric ?? false,
            assessmentWeight: a.assessmentWeight ?? 100,
            formativeInstructions: a.formativeInstructions,
            // Summative
            examType: a.examType, duration: a.duration,
            numberOfQuestions: a.numberOfQuestions, passMark: a.passMark ?? 50,
            hasPracticalComponent: a.hasPracticalComponent ?? false,
            practicalWeight: a.practicalWeight ?? 0, theoryWeight: a.theoryWeight ?? 100,
            summativeInstructions: a.summativeInstructions,
            // Competency
            competencyName: a.competencyName, competencyStrand: a.competencyStrand,
            competencySubStrand: a.competencySubStrand,
            performanceIndicators: a.performanceIndicators,
            assessmentMethod: a.assessmentMethod, ratingScale: a.ratingScale,
            isObservationBased: a.isObservationBased ?? true,
            toolsRequired: a.toolsRequired, competencyInstructions: a.competencyInstructions,
            specificLearningOutcome: a.specificLearningOutcome,
          };

          [0, 1, 2].forEach(i => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => { this.sectionValid[k] = true; });
          this.alertService.info('Editing existing assessment');
        },
        error: (e) => this.alertService.error(e?.error?.message || 'Failed to load assessment'),
      });
  }

  // ── Draft persistence ──────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'assessment_enrollment_draft';

  private _loadDraft(): void {
    if (this.isEditMode) return;
    try {
      const raw = localStorage.getItem(this.DRAFT_KEY);
      if (!raw) return;
      const d = JSON.parse(raw);
      this.formSections   = { ...this.formSections, ...(d.formSections ?? {}) };
      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep    = d.currentStep ?? 0;
      this.lastSaved      = d.savedAt ? new Date(d.savedAt) : null;
      this.alertService.info('Draft restored — continue where you left off.');
    } catch { /* ignore */ }
  }

  private _persistDraft(): void {
    const d = {
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    };
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify(d));
    this.lastSaved = new Date();
  }

  private _clearDraft(): void {
    localStorage.removeItem(this.DRAFT_KEY);
  }

  // ── Section events ─────────────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    this.formSections[section] = { ...this.formSections[section], ...data };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ── Navigation ─────────────────────────────────────────────────────────
  navigateToStep(i: number): void {
    if (!this.canNavigateTo(i)) return;
    this.currentStep = i;
    if (this.isMobileView) this.showMobileSidebar = false;
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
      this.alertService.success('Draft saved. Continue any time.');
    }, 500);
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
    return [0, 1].every(i => this.completedSteps.has(i));   // step 2 (details) is optional
  }

  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const r = 56;
    return 2 * Math.PI * r * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  // ── Submit ─────────────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;
    this.isSubmitting = true;

    try {
      const payload = this._buildPayload();
      console.log('[AssessmentEnrollment] payload:', payload);

      if (this.assessmentId) {
        await this.service.update(this.assessmentId, { id: this.assessmentId, ...payload }).toPromise();
        this.alertService.success('Assessment updated successfully!');
      } else {
        await this.service.create(payload).toPromise();
        this.alertService.success('Assessment created successfully!');
      }

      this._clearDraft();
      setTimeout(() => this.router.navigate(['/assessment/assessments']), 1200);
    } catch (err: any) {
      this.alertService.error(err?.error?.message || 'Submission failed. Please review and try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private _buildPayload(): any {
    const id   = this.formSections['identity'];
    const det  = this.formSections['details'];
    const type = this.currentAssessmentType;

    return {
      assessmentType: type,
      // SuperAdmin sends schoolId; tenant users don't need it (resolved server-side)
      ...(this.isSuperAdmin && id.schoolId ? { schoolId: id.schoolId } : {}),
      title:          id.title?.trim(),
      description:    id.description?.trim() || null,
      teacherId:      id.teacherId,
      subjectId:      id.subjectId,
      classId:        id.classId,
      termId:         id.termId,
      academicYearId: id.academicYearId,
      assessmentDate: id.assessmentDate,
      maximumScore:   Number(id.maximumScore),
      isPublished:    id.isPublished ?? false,
      ...det,
    };
  }

  goBack(): void { this.router.navigate(['/assessment/assessments']); }
}