import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AlertService }    from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }     from 'app/core/auth/auth.service';
import { SchoolService }   from 'app/core/DevKenService/Tenant/SchoolService';
import { ParentService }   from 'app/core/DevKenService/Parents/Parent.service';
import { SchoolDto }       from 'app/Tenant/types/school';

import { ParentBasicComponent }      from '../parent-basic/parent-basic.component';
import { ParentContactComponent }    from '../parent-contact/parent-contact.component';
import { ParentEmploymentComponent } from '../parent-employment/parent-employment.component';
import { ParentIdentityComponent }   from '../parent-identity/parent-identity.component';
import { ParentReviewStepComponent } from '../parent-review-step/parent-review-step.component';
import { ParentSettingsComponent }   from '../parent-settings/parent-settings.component';

export interface ParentEnrollmentStep {
  label:      string;
  icon:       string;
  sectionKey: string;
}

@Component({
  selector: 'app-parent-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    ParentBasicComponent,
    ParentContactComponent,
    ParentIdentityComponent,
    ParentEmploymentComponent,
    ParentSettingsComponent,
    ParentReviewStepComponent,
  ],
  templateUrl: './parent-enrollment.component.html',
  animations: [
    trigger('stepTransition', [
      transition(':increment', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(40px)' })],  { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',        style({ opacity: 0, transform: 'translateX(-40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))],     { optional: true }),
        ]),
      ]),
      transition(':decrement', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(-40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in',        style({ opacity: 0, transform: 'translateX(40px)' }))],  { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))],     { optional: true }),
        ]),
      ]),
    ]),
  ],
})
export class ParentEnrollmentComponent implements OnInit, OnDestroy {

  // ─── State ──────────────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();
  parentId:   string | null = null;
  isEditMode  = false;
  isSaving    = false;
  isSubmitting = false;
  lastSaved:  Date | null = null;

  // ─── SuperAdmin school lookup ───────────────────────────────────────────
  schools: SchoolDto[] = [];

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Sidebar state ──────────────────────────────────────────────────────
  isSidebarCollapsed = false;
  showMobileSidebar  = false;
  isMobileView       = false;

  private destroy$ = new Subject<void>();

  @HostListener('window:resize')
  onResize(): void { this.checkViewport(); }

  private checkViewport(): void {
    const width = window.innerWidth;
    this.isMobileView = width < 1024;
    if (width < 1280 && width >= 1024) this.isSidebarCollapsed = true;
    if (!this.isMobileView) this.showMobileSidebar = false;
  }

  toggleSidebar(): void {
    if (this.isMobileView) {
      this.showMobileSidebar = !this.showMobileSidebar;
    } else {
      this.isSidebarCollapsed = !this.isSidebarCollapsed;
    }
  }

  // ─── Steps ──────────────────────────────────────────────────────────────
  steps: ParentEnrollmentStep[] = [
    { label: 'Basic Information', icon: 'person',        sectionKey: 'basic'      },
    { label: 'Contact Details',   icon: 'contacts',      sectionKey: 'contact'    },
    { label: 'Identity',          icon: 'badge',         sectionKey: 'identity'   },
    { label: 'Employment',        icon: 'work',          sectionKey: 'employment' },
    { label: 'Settings',          icon: 'settings',      sectionKey: 'settings'   },
    { label: 'Review & Submit',   icon: 'check_circle',  sectionKey: 'review'     },
  ];

  // ─── Section validity ────────────────────────────────────────────────────
  sectionValid: Record<string, boolean> = {
    basic:      false,
    contact:    true,
    identity:   true,
    employment: true,
    settings:   true,
  };

  // ─── Form data per section ───────────────────────────────────────────────
  formSections: Record<string, any> = {
    basic:      {},
    contact:    {},
    identity:   {},
    employment: {},
    settings:   { isPrimaryContact: true, isEmergencyContact: true, hasPortalAccess: false },
  };

  constructor(
    private _alertService:  AlertService,
    private _authService:   AuthService,
    private _schoolService: SchoolService,
    private _parentService: ParentService,
    private _router:        Router,
    private _route:         ActivatedRoute,
  ) {}

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.parentId   = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.parentId;

    // SuperAdmin needs the school list for the school-selector step
    if (this.isSuperAdmin) {
      this._schoolService.getAll()
        .pipe(takeUntil(this.destroy$))
        .subscribe(res => { this.schools = (res as any).data ?? []; });
    }

    if (this.parentId) {
      this.loadExistingParent(this.parentId);
    } else {
      this.loadDraft();
    }

    this.checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load existing parent ────────────────────────────────────────────────
  private loadExistingParent(id: string): void {
    this._parentService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          const parent = res.data || res;
          this.hydrateFromParent(parent);
          this.steps.slice(0, 5).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(k => { this.sectionValid[k] = true; });
          this._alertService.info('Editing existing parent record');
        },
        error: (err) => {
          this._alertService.error(err?.error?.message || 'Could not load parent data.');
          this._router.navigate(['/people/parents']);
        },
      });
  }

  private hydrateFromParent(p: any): void {
    this.formSections['basic'] = {
      firstName:    p.firstName    || '',
      middleName:   p.middleName   || '',
      lastName:     p.lastName     || '',
      relationship: p.relationship ?? null,
      // SuperAdmin: preserve the tenantId so the school selector shows the right school
      tenantId:     p.tenantId     || '',
    };
    this.formSections['contact'] = {
      phoneNumber:            p.phoneNumber            || '',
      alternativePhoneNumber: p.alternativePhoneNumber || '',
      email:                  p.email                  || '',
      address:                p.address                || '',
    };
    this.formSections['identity'] = {
      nationalIdNumber: p.nationalIdNumber || '',
      passportNumber:   p.passportNumber   || '',
    };
    this.formSections['employment'] = {
      occupation:      p.occupation      || '',
      employer:        p.employer        || '',
      employerContact: p.employerContact || '',
    };
    this.formSections['settings'] = {
      isPrimaryContact:   p.isPrimaryContact   ?? true,
      isEmergencyContact: p.isEmergencyContact ?? true,
      hasPortalAccess:    p.hasPortalAccess     ?? false,
      portalUserId:       p.portalUserId        || '',
    };
  }

  // ─── Draft persistence ───────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'parent_enrollment_draft';

  private loadDraft(): void {
    if (this.parentId) return;
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections   = { ...this.formSections,   ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep    = draft.currentStep ?? 0;
      this.lastSaved      = draft.savedAt ? new Date(draft.savedAt) : null;
      this._alertService.info('Draft loaded. You can continue where you left off.');
    } catch { /* malformed */ }
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

  private clearDraft(): void {
    localStorage.removeItem(this.DRAFT_KEY);
  }

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
    setTimeout(() => {
      this.isSaving = false;
      this._alertService.success('Draft saved locally. You can continue later.');
    }, 500);
  }

  // ─── Submit ──────────────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;

    // SuperAdmin must have selected a school
    if (this.isSuperAdmin && !this.formSections['basic']?.tenantId) {
      this._alertService.error('Please select a school before submitting.');
      this.currentStep = 0;
      return;
    }

    this.isSubmitting = true;
    try {
      const payload = this.buildPayload();
      console.log('[ParentEnrollment] Submitting payload:', payload);

      if (this.parentId) {
        await this._parentService.update(this.parentId, payload).toPromise();
        this._alertService.success('Parent updated successfully!');
      } else {
        await this._parentService.create(payload).toPromise();
        this._alertService.success('Parent created successfully!');
      }

      this.clearDraft();
      setTimeout(() => this._router.navigate(['/people/parents']), 1500);
    } catch (err: any) {
      console.error('[ParentEnrollment] Submission error:', err);
      this._alertService.error(
        err?.error?.message || err?.error?.title || 'Submission failed. Please review and try again.'
      );
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): any {
    const basic      = this.formSections['basic'];
    const contact    = this.formSections['contact'];
    const identity   = this.formSections['identity'];
    const employment = this.formSections['employment'];
    const settings   = this.formSections['settings'];

    const payload: any = {
      firstName:              basic.firstName?.trim(),
      middleName:             basic.middleName?.trim()  || null,
      lastName:               basic.lastName?.trim(),
      relationship:           Number(basic.relationship),
      phoneNumber:            contact.phoneNumber?.trim()             || null,
      alternativePhoneNumber: contact.alternativePhoneNumber?.trim()  || null,
      email:                  contact.email?.trim()                   || null,
      address:                contact.address?.trim()                 || null,
      nationalIdNumber:       identity.nationalIdNumber?.trim()       || null,
      passportNumber:         identity.passportNumber?.trim()         || null,
      occupation:             employment.occupation?.trim()           || null,
      employer:               employment.employer?.trim()             || null,
      employerContact:        employment.employerContact?.trim()      || null,
      isPrimaryContact:       settings.isPrimaryContact   ?? true,
      isEmergencyContact:     settings.isEmergencyContact ?? true,
      hasPortalAccess:        settings.hasPortalAccess    ?? false,
      portalUserId:           settings.portalUserId?.trim()           || null,
    };

    // ✅ SuperAdmin must send tenantId; regular users omit it (server fills it in)
    if (this.isSuperAdmin && basic.tenantId) {
      payload.tenantId = basic.tenantId;
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
    if (index === 0) return true;
    if (index <= this.currentStep) return true;
    if (this.isEditMode) return true;
    return this.completedSteps.has(index - 1);
  }

  isStepCompleted(index: number): boolean { return this.completedSteps.has(index); }

  allStepsCompleted(): boolean {
    if (this.isEditMode) return true;
    return this.steps.slice(0, 5).every((_, i) => this.completedSteps.has(i));
  }

  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const circumference = 2 * Math.PI * 56;
    return circumference * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  goBack(): void { this._router.navigate(['/people/parents']); }
}