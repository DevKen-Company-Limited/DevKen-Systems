import { Component, OnInit, OnDestroy, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { InvoiceService } from 'app/core/DevKenService/Invoice/Invoice.service ';
import { InvoiceDetailsComponent, InvoiceLookupItem } from '../invoice-details/invoice-details.component';
import { InvoiceItemsComponent } from '../invoice-items/invoice-items.component';
import { InvoiceNotesComponent } from '../invoice-notes/invoice-notes.component';
import { InvoiceReviewStepComponent } from '../invoice-review-step/invoice-review-step.component';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { ParentService } from 'app/core/DevKenService/Parents/Parent.service';
import { TermService } from 'app/core/DevKenService/TermService/term.service';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolDto } from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';

export interface InvoiceEnrollmentStep {
  label: string;
  icon: string;
  sectionKey: string;
}

@Component({
  selector: 'app-invoice-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    InvoiceDetailsComponent,
    InvoiceItemsComponent,
    InvoiceNotesComponent,
    InvoiceReviewStepComponent,
  ],
  templateUrl: './invoice-enrollment.component.html',
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
export class InvoiceEnrollmentComponent implements OnInit, OnDestroy {
  currentStep = 0;
  completedSteps = new Set<number>();
  invoiceId: string | null = null;
  isEditMode = false;
  isSaving = false;
  isSubmitting = false;
  lastSaved: Date | null = null;

  schools: SchoolDto[] = [];
  private _schoolService = inject(SchoolService);

  // Lookup data for the Details step
  students:      InvoiceLookupItem[] = [];
  academicYears: InvoiceLookupItem[] = [];
  terms:         InvoiceLookupItem[] = [];
  parents:       InvoiceLookupItem[] = [];

  
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private destroy$ = new Subject<void>();

  isSidebarCollapsed = false;
  showMobileSidebar = false;
  isMobileView = false;

  @HostListener('window:resize')
  onResize(): void { this.checkViewport(); }

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }
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

  steps: InvoiceEnrollmentStep[] = [
    { label: 'Invoice Details', icon: 'receipt',        sectionKey: 'details' },
    { label: 'Line Items',      icon: 'list_alt',        sectionKey: 'items'   },
    { label: 'Notes',           icon: 'sticky_note_2',   sectionKey: 'notes'   },
    { label: 'Review & Submit', icon: 'check_circle',    sectionKey: 'review'  },
  ];

  sectionValid: Record<string, boolean> = {
    details: false,
    items:   false,
    notes:   true,
  };

  formSections: Record<string, any> = {
    details: {},
    items:   [],
    notes:   '',
  };

  constructor(
    private alertService:       AlertService,
    private invoiceService:     InvoiceService,
    private studentService:     StudentService,
    private academicYearService: AcademicYearService,
    private termService:        TermService,
    private parentService:      ParentService,
    private router:             Router,
    private route:              ActivatedRoute,
  ) {}

  ngOnInit(): void {
    if (this.isSuperAdmin) {
        this._schoolService.getAll()
          .pipe(takeUntil(this.destroy$))
          .subscribe((res: any) => { this.schools = res.data ?? []; });
      }
    this.invoiceId  = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.invoiceId;

    this.loadLookups();

    if (this.invoiceId) {
      this.loadExistingInvoice(this.invoiceId);
    } else {
      this.loadDraft();
    }

    this.checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load dropdown data ───────────────────────────────────────────────────
  private loadLookups(): void {
    // Students
    this.studentService.getAll()
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = Array.isArray(res) ? res : (res?.data ?? []);
        this.students = list.map((s: any) => ({
          id:   s.id,
          name: s.fullName ?? `${s.firstName} ${s.lastName}`,
        }));
      });

    // Academic Years
    this.academicYearService.getAll()
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = Array.isArray(res) ? res : (res?.data ?? []);
        this.academicYears = list.map((y: any) => ({ id: y.id, name: y.name }));
      });

    // Terms
    this.termService.getAll()
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = Array.isArray(res) ? res : (res?.data ?? []);
        this.terms = list.map((t: any) => ({ id: t.id, name: t.name }));
      });

    // Parents
    this.parentService.query({})
      .pipe(catchError(() => of({ success: true, data: [] })), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = res?.data ?? [];
        this.parents = list.map((p: any) => ({
          id:   p.id,
          name: p.fullName,
        }));
      });
  }

  // ── Load existing invoice ────────────────────────────────────────────────
  private loadExistingInvoice(id: string): void {
    this.invoiceService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          const invoice = res.data || res;
          this.hydrateFromInvoice(invoice);
          this.steps.slice(0, 3).forEach((_, i) => this.completedSteps.add(i));
          Object.keys(this.sectionValid).forEach(key => { this.sectionValid[key] = true; });
          this.alertService.info('Editing existing invoice');
        },
        error: (err) => {
          this.alertService.error(err?.error?.message || 'Could not load invoice data.');
          this.router.navigate(['/finance/invoices']);
        },
      });
  }

  private hydrateFromInvoice(inv: any): void {
    this.formSections['details'] = {
      studentId:      inv.studentId      || '',
      academicYearId: inv.academicYearId || '',
      termId:         inv.termId         || '',
      parentId:       inv.parentId       || '',
      invoiceDate:    new Date(inv.invoiceDate),
      dueDate:        new Date(inv.dueDate),
      description:    inv.description    || '',
    };
    this.formSections['items'] = inv.items || [];
    this.formSections['notes'] = inv.notes || '';
  }

  // ── Draft ────────────────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'invoice_enrollment_draft';

  private loadDraft(): void {
    if (this.invoiceId) return;
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections   = { ...this.formSections,   ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep    = draft.currentStep ?? 0;
      this.lastSaved      = draft.savedAt ? new Date(draft.savedAt) : null;
      this.alertService.info('Draft loaded. You can continue where you left off.');
    } catch { /* ignore */ }
  }

  private persistDraft(): void {
    const draft = {
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    };
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify(draft));
    this.lastSaved = new Date();
  }

  private clearDraft(): void {
    localStorage.removeItem(this.DRAFT_KEY);
  }

  // ── Section events ───────────────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    this.formSections[section] = data;
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  navigateToStep(index: number): void {
    if (this.canNavigateTo(index)) {
      this.currentStep = index;
      if (this.isMobileView) this.showMobileSidebar = false;
    }
  }

  prevStep(): void {
    if (this.currentStep > 0) this.currentStep--;
  }

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
      this.alertService.success('Draft saved locally. You can continue later.');
    }, 500);
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;

     // SuperAdmin guard
    if (this.isSuperAdmin && !this.formSections['details']?.tenantId) {
      this._alertService.error('Please select a school in the Invoice Details step.');
      this.currentStep = 0;
      return;
    }
    this.isSubmitting = true;
    try {
      const payload = this.buildPayload();

      if (this.invoiceId) {
        await this.invoiceService.update(this.invoiceId, payload).toPromise();
        this.alertService.success('Invoice updated successfully!');
      } else {
        await this.invoiceService.create(payload).toPromise();
        this.alertService.success('Invoice created successfully!');
      }

      this.clearDraft();
      setTimeout(() => this.router.navigate(['/finance/invoices']), 1500);
    } catch (err: any) {
      this.alertService.error(err?.error?.message || err?.error?.title || 'Submission failed.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): any {
  const details = this.formSections['details'];
  const items   = this.formSections['items'];
  const notes   = this.formSections['notes'];

  const payload: any = {
    studentId:      details.studentId,
    academicYearId: details.academicYearId,
    termId:         details.termId   || undefined,
    parentId:       details.parentId || undefined,
    invoiceDate:    details.invoiceDate
      ? new Date(details.invoiceDate).toISOString()
      : new Date().toISOString(),
    dueDate:        details.dueDate
      ? new Date(details.dueDate).toISOString()
      : new Date().toISOString(),
    description:    details.description || null,
    notes:          notes               || null,
    items: items.map((item: any) => ({
      description: item.description,
      itemType:    item.itemType    || null,
      quantity:    item.quantity,
      unitPrice:   item.unitPrice,
      discount:    item.discount   || 0,
      isTaxable:   item.isTaxable  || false,
      taxRate:     item.isTaxable  ? (item.taxRate || 0) : null,
      glCode:      item.glCode     || null,
      notes:       item.notes      || null,
    })),
  };

  // ✅ SuperAdmin must send tenantId; regular users omit it
  if (this.isSuperAdmin && details.tenantId) {
    payload.tenantId = details.tenantId;
  }

  return payload;
}

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

  isStepCompleted(index: number): boolean {
    return this.completedSteps.has(index);
  }

  allStepsCompleted(): boolean {
    if (this.isEditMode) return true;
    return this.steps.slice(0, 3).every((_, i) => this.completedSteps.has(i));
  }

  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const circumference = 2 * Math.PI * 56;
    const pct = this.completedSteps.size / (this.steps.length - 1);
    return circumference * (1 - pct);
  }

  goBack(): void {
    this.router.navigate(['/finance/invoices']);
  }
}