// ═══════════════════════════════════════════════════════════════════
// payment-form.component.ts  (Create / Edit)
// Mirrors assessment-form.component.ts exactly
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, HostListener,
} from '@angular/core';
import { CommonModule }             from '@angular/common';
import { FormsModule }              from '@angular/forms';
import { Router, ActivatedRoute }   from '@angular/router';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { Subject, forkJoin, of, Observable } from 'rxjs';
import { takeUntil, catchError, tap, map }   from 'rxjs/operators';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

import { AlertService }   from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }    from 'app/core/auth/auth.service';
import { PaymentService } from 'app/core/DevKenService/payments/payment.service';
import { PaymentDetailsStepComponent } from '../steps/payment-details-step.component';
import { PaymentInfoStepComponent } from '../steps/payment-info-step.component';
import { PaymentReviewStepComponent } from '../steps/payment-review-step.component';
import { PaymentMethod, PaymentStatus, UpdatePaymentDto, CreatePaymentDto, PaymentMethodValue, PaymentStatusValue } from '../types/payments';


interface FormStep { label: string; icon: string; sectionKey: string; }

@Component({
  selector: 'app-payment-form',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule,
    // Step components
    PaymentInfoStepComponent,
    PaymentDetailsStepComponent,
    PaymentReviewStepComponent,
  ],
  templateUrl: './payment-form.component.html',
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
export class PaymentFormComponent implements OnInit, OnDestroy {

  private _destroy$ = new Subject<void>();

  // ─── Mode ──────────────────────────────────────────────────────
  paymentId:  string | null = null;
  isEditMode  = false;

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
    { label: 'Payment Info',    icon: 'info',     sectionKey: 'info'    },
    { label: 'Payment Details', icon: 'tune',     sectionKey: 'details' },
    { label: 'Review & Submit', icon: 'check',    sectionKey: 'review'  },
  ];

  sectionValid: Record<string, boolean> = {
    info:    false,
    details: true,   // details always valid — conditional fields
  };

  formSections: Record<string, any> = {
    info: {
      paymentMethod: 'Cash' as PaymentMethod,
      statusPayment: 'Completed' as PaymentStatus,
      paymentDate:   new Date().toISOString().split('T')[0],
    },
    details: {},
  };

  // ─── Lookups ───────────────────────────────────────────────────
  students:  any[] = [];
  invoices:  any[] = [];
  staffList: any[] = [];
  schools:   any[] = [];

  activeSchoolId:    string | undefined = undefined;
  activeStudentId:   string | undefined = undefined;

  isSaving         = false;
  isSubmitting     = false;
  isLoadingLookups = false;

  constructor(
    private _service:      PaymentService,
    private _router:       Router,
    private _route:        ActivatedRoute,
    private _alertService: AlertService,
    private _authService:  AuthService,
  ) {}

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  // ─── Lifecycle ─────────────────────────────────────────────────
  ngOnInit(): void {
    this.paymentId  = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.paymentId;
    this.checkViewport();

    if (this.isSuperAdmin) {
      this._loadSchools()
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

  private _loadSchools(): Observable<void> {
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
      students:  this._service.getStudents(schoolId).pipe(catchError(() => of([]))),
      staffList: this._service.getStaff(schoolId).pipe(catchError(() => of([]))),
      schools:   this._service.getSchools().pipe(catchError(() => of([]))),
    }).pipe(
      tap(d => {
        this.students         = d.students;
        this.staffList        = d.staffList;
        this.schools          = d.schools;
        this.isLoadingLookups = false;
      }),
      map(() => void 0 as void),
    );
  }

  private _loadSchoolScopedLookups(schoolId: string): Observable<void> {
    this.isLoadingLookups = true;
    return forkJoin({
      students:  this._service.getStudents(schoolId).pipe(catchError(() => of([]))),
      staffList: this._service.getStaff(schoolId).pipe(catchError(() => of([]))),
    }).pipe(
      tap(d => {
        this.students         = d.students;
        this.staffList        = d.staffList;
        this.isLoadingLookups = false;
      }),
      map(() => void 0 as void),
    );
  }

  private _loadStudentInvoices(studentId: string, schoolId?: string): void {
    this._service.getInvoicesByStudent(studentId, schoolId)
      .pipe(takeUntil(this._destroy$), catchError(() => of([])))
      .subscribe(invoices => this.invoices = invoices);
  }

  onSchoolChanged(schoolId: string): void {
    this.activeSchoolId = schoolId || undefined;
    this.invoices       = [];
    this.activeStudentId = undefined;
    if (!schoolId) {
      this.students = []; this.staffList = [];
      return;
    }
    this._loadSchoolScopedLookups(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe();
  }

  onStudentChanged(studentId: string): void {
    this.activeStudentId = studentId || undefined;
    this.invoices        = [];
    if (!studentId) return;
    this._loadStudentInvoices(studentId, this.activeSchoolId);
  }

  // ─── Load existing (Edit mode) ──────────────────────────────────
  private _loadExisting(): void {
    this._service.getById(this.paymentId!)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: p => {
          if (this.isSuperAdmin && p.tenantId) {
            this.activeSchoolId    = p.tenantId;
            this.activeStudentId   = p.studentId;
            this._loadSchoolScopedLookups(p.tenantId)
              .pipe(takeUntil(this._destroy$))
              .subscribe(() => {
                this._loadStudentInvoices(p.studentId, p.tenantId);
                this._populateFormSections(p);
              });
          } else {
            this.activeStudentId = p.studentId;
            this._loadStudentInvoices(p.studentId, undefined);
            this._populateFormSections(p);
          }
        },
        error: () => this._alertService.error('Could not load payment data.'),
      });
  }

  private _populateFormSections(p: any): void {
    this.formSections = {
      ...this.formSections,
      info: {
        studentId:     p.studentId      ?? '',
        invoiceId:     p.invoiceId      ?? '',
        receivedBy:    p.receivedBy     ?? '',
        paymentDate:   p.paymentDate    ? p.paymentDate.split('T')[0] : '',
        receivedDate:  p.receivedDate   ? p.receivedDate.split('T')[0] : '',
        amount:        p.amount         ?? 0,
        paymentMethod: p.paymentMethod  ?? 'Cash',
        statusPayment: p.statusPayment  ?? 'Completed',
        description:   p.description    ?? '',
        notes:         p.notes          ?? '',
        schoolId:      p.tenantId       ?? '',
      },
      details: {
        // M-Pesa
        mpesaCode:            p.mpesaCode            ?? '',
        phoneNumber:          p.phoneNumber          ?? '',
        // Bank
        bankName:             p.bankName             ?? '',
        accountNumber:        p.accountNumber        ?? '',
        // Cheque
        chequeNumber:         p.chequeNumber         ?? '',
        chequeClearanceDate:  p.chequeClearanceDate
          ? p.chequeClearanceDate.split('T')[0] : '',
        // General
        transactionReference: p.transactionReference ?? '',
      },
    };

    this.steps.slice(0, 2).forEach((_, i) => this.completedSteps.add(i));
    Object.keys(this.sectionValid).forEach(k => this.sectionValid[k] = true);
  }

  // ─── Draft ─────────────────────────────────────────────────────
  private readonly DRAFT_KEY = 'payment_form_draft';

  private _loadDraft(): void {
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const d           = JSON.parse(raw);
      this.formSections = { ...this.formSections, ...d.formSections };
      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep    = d.currentStep ?? 0;

      const schoolId  = this.formSections.info?.schoolId;
      const studentId = this.formSections.info?.studentId;

      if (this.isSuperAdmin && schoolId) {
        this.activeSchoolId = schoolId;
        this._loadSchoolScopedLookups(schoolId)
          .pipe(takeUntil(this._destroy$))
          .subscribe(() => {
            if (studentId) this._loadStudentInvoices(studentId, schoolId);
          });
      } else if (studentId) {
        this._loadStudentInvoices(studentId, undefined);
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
      if (this.isEditMode) {
        const dto: UpdatePaymentDto = this._buildUpdatePayload();
        await this._service.update(this.paymentId!, dto).toPromise();
        this._alertService.success('Payment updated successfully!');
      } else {
        const dto: CreatePaymentDto = this._buildCreatePayload();
        await this._service.create(dto).toPromise();
        this._alertService.success('Payment recorded successfully!');
      }
      this._clearDraft();
      setTimeout(() => this._router.navigate(['/finance/payments']), 1200);
    } catch (err: any) {
      this._alertService.error(err?.error?.message || 'Submission failed. Please try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  // ─── Build Payloads ──────────────────────────────────────────────
private _buildCreatePayload(): CreatePaymentDto {
  const { info, details } = this.formSections;
  const method: PaymentMethod = info.paymentMethod;

  const base: CreatePaymentDto = {
    studentId:     info.studentId,
    invoiceId:     info.invoiceId,
    receivedBy:    info.receivedBy     || undefined,
    paymentDate:   info.paymentDate,
    receivedDate:  info.receivedDate   || undefined,
    amount:        +info.amount,
    paymentMethod: PaymentMethodValue[method] as any,
    statusPayment: PaymentStatusValue[info.statusPayment ?? 'Completed'] as any,
    description:   info.description   || undefined,
    notes:         info.notes         || undefined,
    tenantId:      info.schoolId      || undefined,
    transactionReference: details.transactionReference || undefined,
  };

  if (method === 'Mpesa') {
    base.mpesaCode   = details.mpesaCode   || undefined;
    base.phoneNumber = details.phoneNumber || undefined;
  } else if (method === 'BankTransfer') {
    base.bankName      = details.bankName      || undefined;
    base.accountNumber = details.accountNumber || undefined;
  } else if (method === 'Cheque') {
    base.chequeNumber        = details.chequeNumber        || undefined;
    base.chequeClearanceDate = details.chequeClearanceDate || undefined;
    base.bankName            = details.bankName            || undefined;
    base.accountNumber       = details.accountNumber       || undefined;
  }

  return base;
}

private _buildUpdatePayload(): UpdatePaymentDto {
  const { info, details } = this.formSections;
  const method: PaymentMethod = info.paymentMethod;

  return {
    paymentDate:   info.paymentDate   || undefined,
    receivedDate:  info.receivedDate  || undefined,
    amount:        info.amount != null ? +info.amount : undefined,
    paymentMethod: PaymentMethodValue[method] as any,
    statusPayment: info.statusPayment
      ? PaymentStatusValue[info.statusPayment as PaymentStatus] as any
      : undefined,
    receivedBy:    info.receivedBy    || undefined,
    description:   info.description  || undefined,
    notes:         info.notes        || undefined,
    transactionReference: details.transactionReference || undefined,
    mpesaCode:     details.mpesaCode      || undefined,
    phoneNumber:   details.phoneNumber    || undefined,
    bankName:      details.bankName       || undefined,
    accountNumber: details.accountNumber  || undefined,
    chequeNumber:  details.chequeNumber   || undefined,
    chequeClearanceDate: details.chequeClearanceDate || undefined,
  };
}

  goBack(): void { this._router.navigate(['/finance/payments']); }
}