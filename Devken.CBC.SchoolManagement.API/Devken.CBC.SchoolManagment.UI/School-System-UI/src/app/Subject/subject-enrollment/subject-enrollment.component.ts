// subject-enrollment/subject-enrollment.component.ts
// KEY FIX in hydrateFromSubject():
//   cbcLevel: resolveCBCLevel(s.level)   ← was s.cbcLevel which is undefined on SubjectDto

import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';
import { SubjectCurriculumComponent } from '../subject-curriculum/subject-curriculum.component';
import { SubjectReviewStepComponent } from '../subject-review-step/subject-review-step.component';
import { SubjectSettingsComponent } from '../subject-settings/subject-settings.component';
import { SubjectIdentityComponent } from '../subject-identity/subject-identity.component';
import { resolveCBCLevel, resolveSubjectType } from '../Types/SubjectEnums';

export interface SubjectEnrollmentStep {
  label:      string;
  icon:       string;
  sectionKey: string;
}

@Component({
  selector: 'app-subject-enrollment',
  standalone: true,
  imports: [
    CommonModule, MatButtonModule, MatIconModule,
    SubjectIdentityComponent, SubjectCurriculumComponent,
    SubjectSettingsComponent, SubjectReviewStepComponent,
  ],
  templateUrl: './subject-enrollment.component.html',
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
export class SubjectEnrollmentComponent implements OnInit, OnDestroy {

  currentStep    = 0;
  completedSteps = new Set<number>();
  subjectId:     string | null = null;
  isEditMode     = false;
  isSaving       = false;
  isSubmitting   = false;
  lastSaved:     Date | null = null;

  schools: SchoolDto[] = [];
  private destroy$ = new Subject<void>();

  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  @HostListener('window:resize')
  onResize(): void { this.checkViewport(); }

  private checkViewport(): void {
    const width = window.innerWidth;
    this.isMobileView = width < 1024;
    if (width < 1280 && width >= 1024) this.isSidebarCollapsed = true;
    if (!this.isMobileView) this.showMobileSidebar = false;
  }

  toggleSidebar(): void {
    if (this.isMobileView) this.showMobileSidebar = !this.showMobileSidebar;
    else                   this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  steps: SubjectEnrollmentStep[] = [
    { label: 'Subject Identity',   icon: 'badge',        sectionKey: 'identity'   },
    { label: 'Curriculum Details', icon: 'school',       sectionKey: 'curriculum' },
    { label: 'Settings',           icon: 'settings',     sectionKey: 'settings'   },
    { label: 'Review & Submit',    icon: 'check_circle', sectionKey: 'review'     },
  ];

  sectionValid: Record<string, boolean> = {
    identity: false, curriculum: false, settings: true,
  };

  formSections: Record<string, any> = {
    identity:   {},
    curriculum: {},
 };

  constructor(
    private alertService:   AlertService,
    private subjectService: SubjectService,
    private authService:    AuthService,
    private schoolService:  SchoolService,
    private router:         Router,
    private route:          ActivatedRoute,
  ) {}

  get isSuperAdmin(): boolean {
    return this.authService.authUser?.isSuperAdmin ?? false;
  }

  ngOnInit(): void {
    this.subjectId  = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.subjectId;

    if (this.isSuperAdmin) {
      this.schoolService.getAll()
        .pipe(takeUntil(this.destroy$))
        .subscribe(res => { this.schools = (res as any).data ?? []; });
    }

    if (this.subjectId) this.loadExistingSubject(this.subjectId);
    else                this.loadDraft();

    this.checkViewport();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private loadExistingSubject(id: string): void {
    this.subjectService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (subject: any) => {
          this.hydrateFromSubject(subject);
          this.steps.slice(0, 3).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => { this.sectionValid[k] = true; });
          this.alertService.info('Editing existing subject record');
        },
        error: err => this.alertService.error(err?.error?.message || 'Could not load subject data.'),
      });
  }

  private hydrateFromSubject(s: any): void {
    this.formSections['identity'] = {
      name:        s.name        ?? '',
      code:        s.code        ?? '',
      description: s.description ?? '',
      schoolId:    s.tenantId    ?? '',
    };

    this.formSections['curriculum'] = {
      subjectType: resolveSubjectType(s.subjectType),  // handles "1", 1, "Core"

      // FIX: use s.level (SubjectDto field) — NOT s.cbcLevel (which is undefined)
      cbcLevel: resolveCBCLevel(s.level),              // handles "3", 3, "Grade1"
    };

    this.formSections['settings'] = {
      isCompulsory: s.isCompulsory ?? false,
      isActive:     s.isActive     ?? true,
    };

    this.formSections = { ...this.formSections };
  }

  // ─── Draft ───────────────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'subject_enrollment_draft';

  private loadDraft(): void {
    if (this.subjectId) return;
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections   = { ...this.formSections, ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep    = draft.currentStep ?? 0;
      this.lastSaved      = draft.savedAt ? new Date(draft.savedAt) : null;
      this.alertService.info('Draft loaded. You can continue where you left off.');
    } catch { /* ignore malformed draft */ }
  }

  private persistDraft(): void {
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify({
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    }));
    this.lastSaved = new Date();
  }

  private clearDraft(): void { localStorage.removeItem(this.DRAFT_KEY); }

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
    this.persistDraft();
  }

  saveDraft(): void {
    this.isSaving = true;
    this.persistDraft();
    setTimeout(() => { this.isSaving = false; this.alertService.success('Draft saved locally.'); }, 500);
  }

  // ─── Submit ──────────────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;
    this.isSubmitting = true;
    try {
      const payload = this.buildPayload();
      if (this.subjectId) {
        await this.subjectService.update(this.subjectId, payload).toPromise();
        this.alertService.success('Subject updated successfully!');
      } else {
        await this.subjectService.create(payload).toPromise();
        this.alertService.success('Subject created successfully!');
      }
      this.clearDraft();
      setTimeout(() => this.router.navigate(['/academic/subjects']), 1500);
    } catch (err: any) {
      this.alertService.error(err?.error?.message || err?.error?.title || 'Submission failed.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): any {
    const { identity, curriculum, settings } = {
      identity:   this.formSections['identity'],
      curriculum: this.formSections['curriculum'],
      settings:   this.formSections['settings'],
    };

    const payload: any = {
      name:         identity.name?.trim(),
      description:  identity.description?.trim() || null,
      subjectType:  Number(curriculum.subjectType),
      cbcLevel:     Number(curriculum.cbcLevel),
      isActive:     settings.isActive     ?? true,
    };

    if (this.isSuperAdmin && identity.schoolId) {
      payload.tenantId = identity.schoolId;
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
    if (index === 0 || index <= this.currentStep || this.isEditMode) return true;
    return this.completedSteps.has(index - 1);
  }

  isStepCompleted(index: number): boolean { return this.completedSteps.has(index); }

  allStepsCompleted(): boolean {
    if (this.isEditMode) return true;
    return this.steps.slice(0, 3).every((_, i) => this.completedSteps.has(i));
  }

  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    return 2 * Math.PI * 56 * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  goBack(): void { this.router.navigate(['/academic/subjects']); }
}