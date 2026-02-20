// ═══════════════════════════════════════════════════════════════════
// assessment-form.component.ts  (Create / Edit)
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, HostListener,
} from '@angular/core';
import { CommonModule }           from '@angular/common';
import { FormsModule }            from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule }        from '@angular/material/button';
import { MatIconModule }          from '@angular/material/icon';
import { Subject, forkJoin, of }  from 'rxjs';
import { takeUntil, catchError }  from 'rxjs/operators';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

import { AlertService }       from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }        from 'app/core/auth/auth.service';
import { ASSESSMENT_TYPES, CreateAssessmentRequest } from 'app/assessment/types/AssessmentDtos';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import {  AssessmentInfoStepComponent,  } from '../steps/assessment-info-step.component';
import { AssessmentReviewStepComponent } from '../steps/assessment-review-step.component';
import { AssessmentDetailsStepComponent } from '../steps/assessment-details-step.component';


interface FormStep { label: string; icon: string; sectionKey: string; }

@Component({
  selector: 'app-assessment-form',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatButtonModule, MatIconModule,
    AssessmentInfoStepComponent, AssessmentDetailsStepComponent, AssessmentReviewStepComponent,
  ],
  templateUrl: './assessment-form.component.html',
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
export class AssessmentFormComponent implements OnInit, OnDestroy {

  private _destroy$ = new Subject<void>();

  // ─── Mode ─────────────────────────────────────────────────────────
  assessmentId: string | null = null;
  isEditMode = false;

  // ─── Sidebar ──────────────────────────────────────────────────────
  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  @HostListener('window:resize') onResize(): void { this.checkViewport(); }

  private checkViewport(): void {
    const w = window.innerWidth;
    this.isMobileView = w < 1024;
    if (w >= 1024 && w < 1280) this.isSidebarCollapsed = true;
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
    { label: 'Basic Info',       icon: 'info',     sectionKey: 'info'    },
    { label: 'Details',          icon: 'tune',     sectionKey: 'details' },
    { label: 'Review & Save',    icon: 'check',    sectionKey: 'review'  },
  ];

  sectionValid: Record<string, boolean> = { info: false, details: true };

  formSections: Record<string, any> = {
    info:    {},
    details: { assessmentType: 'Formative', maximumScore: 100 },
  };

  // ─── Lookups ──────────────────────────────────────────────────────
  classes:       any[] = [];
  teachers:      any[] = [];
  subjects:      any[] = [];
  terms:         any[] = [];
  academicYears: any[] = [];
  schools:       any[] = [];
  assessmentTypes = ASSESSMENT_TYPES;

  isSaving     = false;
  isSubmitting = false;

  constructor(
    private _service:      AssessmentService,
    private _router:       Router,
    private _route:        ActivatedRoute,
    private _alertService: AlertService,
    private _authService:  AuthService,
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
      schools:       this._service.getSchools().pipe(catchError(() => of([]))),
    })
    .pipe(takeUntil(this._destroy$))
    .subscribe(d => {
      this.classes       = d.classes;
      this.teachers      = d.teachers;
      this.subjects      = d.subjects;
      this.terms         = d.terms;
      this.academicYears = d.academicYears;
      this.schools       = d.schools;
    });
  }

  // ─── Load existing ─────────────────────────────────────────────────
  private loadExisting(): void {
    this._service.getById(this.assessmentId!)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: a => {
          this.formSections.info = {
            title:          a.title,
            description:    a.description,
            assessmentDate: a.assessmentDate?.split('T')[0],
            teacherId:      a.teacherId,
            subjectId:      a.subjectId,
            classId:        a.classId,
            termId:         a.termId,
            academicYearId: a.academicYearId,
            schoolId:       a.schoolId,
          };
          this.formSections.details = {
            assessmentType: a.assessmentType,
            maximumScore:   a.maximumScore,
          };
          this.steps.slice(0, 2).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => this.sectionValid[k] = true);
          this._alertService.info('Editing existing assessment.');
        },
        error: () => this._alertService.error('Could not load assessment data.'),
      });
  }

  // ─── Draft ────────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'assessment_form_draft';

  private loadDraft(): void {
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const d = JSON.parse(raw);
      this.formSections   = { ...this.formSections,  ...d.formSections };
      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep    = d.currentStep ?? 0;
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
  onSectionChanged(section: string, data: any):       void { this.formSections[section] = { ...this.formSections[section], ...data }; }
  onSectionValidChanged(section: string, valid: boolean): void { this.sectionValid[section] = valid; }

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
    setTimeout(() => { this.isSaving = false; this._alertService.success('Draft saved locally.'); }, 400);
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
      setTimeout(() => this._router.navigate(['/assessment/assessments']), 1200);
    } catch (err: any) {
      this._alertService.error(err?.error?.message || 'Submission failed. Please try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): CreateAssessmentRequest {
    const { info, details } = this.formSections;
    return {
      title:          info.title?.trim(),
      description:    info.description?.trim() || undefined,
      assessmentDate: info.assessmentDate,
      teacherId:      info.teacherId   || undefined,
      subjectId:      info.subjectId   || undefined,
      classId:        info.classId     || undefined,
      termId:         info.termId      || undefined,
      academicYearId: info.academicYearId || undefined,
      schoolId:       info.schoolId    || undefined,
      assessmentType: details.assessmentType,
      maximumScore:   +details.maximumScore,
    };
  }

  goBack(): void { this._router.navigate(['/assessment/assessments']); }
}