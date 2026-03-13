import {
  Component, OnInit, OnDestroy, HostListener, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { TermService } from 'app/core/DevKenService/TermService/term.service';
import { InvoiceLookupItem } from '../../invoice-details/invoice-details.component';
import { InvoiceService } from 'app/core/DevKenService/Finance/Invoice/Invoice.service ';

// Sub-components
import { BulkInvoiceScopeComponent } from '../bulk-invoice-scope/bulk-invoice-scope.component';
import { BulkInvoiceStudentsComponent } from '../bulk-invoice-students/bulk-invoice-students.component';
import { BulkInvoiceReviewComponent } from '../bulk-invoice-review/bulk-invoice-review.component';
import { BulkInvoiceProgressDialogComponent, BulkProgressDialogData } from 'app/dialog-modals/Finance/Invoice/bulk-invoice-progress-dialog/bulk-invoice-progress-dialog.component';
import { InvoiceLineItemsComponent } from '../../invoice-items/invoice-line-items.component';
import { InvoiceNotesComponent } from '../../invoice-notes/invoice-notes.component';
import { BulkInvoiceStudentRow, BulkInvoiceFeeItem } from '../../Types/bulk-invoice.types';

// Fix 1: Import ClassDto from its real source, not the local stub
import { ClassDto } from 'app/Classes/Types/Class';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { ClassService } from 'app/core/DevKenService/ClassService/ClassService';

interface BulkStep { label: string; icon: string; key: string; }

@Component({
  selector: 'app-bulk-invoice-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    BulkInvoiceScopeComponent,
    BulkInvoiceStudentsComponent,
    BulkInvoiceReviewComponent,
    InvoiceLineItemsComponent,
    InvoiceNotesComponent,
  ],
  templateUrl: './bulk-invoice-enrollment.component.html',
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
export class BulkInvoiceEnrollmentComponent implements OnInit, OnDestroy {
  currentStep = 0;
  completedSteps = new Set<number>();
  isSaving = false;
  isSubmitting = false;
  studentsLoading = false;

  isSidebarCollapsed = false;
  showMobileSidebar = false;
  isMobileView = false;

  schools: SchoolDto[] = [];
  classes: ClassDto[] = [];
  academicYears: InvoiceLookupItem[] = [];
  terms: InvoiceLookupItem[] = [];

  selectedClassName = '';

  formData: {
    scope: any;
    students: BulkInvoiceStudentRow[];
    feeItems: BulkInvoiceFeeItem[];
    notes: string;
  } = {
    scope: {},
    students: [],
    feeItems: [],
    notes: '',
  };

  sectionValid: Record<string, boolean> = {
    scope: false, students: false, feeItems: false, notes: true,
  };

  steps: BulkStep[] = [
    { label: 'Invoice Scope',     icon: 'group',         key: 'scope'    },
    { label: 'Select Students',   icon: 'people',        key: 'students' },
    { label: 'Fee Items',         icon: 'list_alt',      key: 'feeItems' },
    { label: 'Notes',             icon: 'sticky_note_2', key: 'notes'    },
    { label: 'Review & Submit',   icon: 'check_circle',  key: 'review'   },
  ];

  private destroy$ = new Subject<void>();
  private _authService    = inject(AuthService);
  private _alertService   = inject(AlertService);
  private _schoolService  = inject(SchoolService);
  private _academicYearSvc = inject(AcademicYearService);
  private _termService    = inject(TermService);
  private _studentService = inject(StudentService);
  private _classService   = inject(ClassService);
  private _invoiceService = inject(InvoiceService);
  private _dialog         = inject(MatDialog);
  private _router         = inject(Router);

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get selectedStudentCount(): number {
    return this.formData.students.filter(s => s.selected).length;
  }

  @HostListener('window:resize')
  onResize(): void { this.checkViewport(); }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.checkViewport();
    this.loadDraft();

    if (this.isSuperAdmin) {
      this._schoolService.getAll()
        .pipe(takeUntil(this.destroy$))
        .subscribe((res: any) => { this.schools = res.data ?? []; });
    } else {
      this.loadLookups();
      this.loadClasses();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Lookups ───────────────────────────────────────────────────────────────

  onSchoolSelected(schoolId: string): void {
    if (schoolId) {
      this.loadLookups(schoolId);
      this.loadClasses(schoolId);
    }
  }

  private loadLookups(schoolId?: string): void {
    if (this.isSuperAdmin && !schoolId) return;

    this._academicYearSvc.getAll(schoolId)
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = Array.isArray(res) ? res : (res?.data ?? []);
        this.academicYears = list.map((y: any) => ({ id: y.id, name: y.name }));
      });

    this._termService.getAll(schoolId)
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        const list = Array.isArray(res) ? res : (res?.data ?? []);
        this.terms = list.map((t: any) => ({ id: t.id, name: t.name }));
      });
  }

  private loadClasses(schoolId?: string): void {
    if (this.isSuperAdmin && !schoolId) return;

    const call = schoolId
      ? this._classService.getAll(schoolId)
      : this._classService.getAll();

    call.pipe(catchError(() => of({ data: [] })), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        // Fix 2: assign the full ClassDto objects directly — no remapping needed
        // since ClassService already returns ClassDto[] matching the type exactly.
        const list: ClassDto[] = Array.isArray(res) ? res : (res?.data ?? []);
        this.classes = list.filter((c: ClassDto) => c.isActive !== false);
      });
  }

  // ── Step: Class selected → load students ─────────────────────────────────

  onClassSelected(classId: string): void {
    if (!classId) return;
    const cls = this.classes.find(c => c.id === classId);
    this.selectedClassName = cls?.name ?? classId;
    this.loadStudentsByClass(classId);
  }

  private loadStudentsByClass(classId: string): void {
    this.studentsLoading = true;
    const schoolId = this.isSuperAdmin ? this.formData.scope?.tenantId : undefined;

    this._studentService.getAll(schoolId)
      .pipe(catchError(() => of([])), takeUntil(this.destroy$))
      .subscribe((res: any) => {
        this.studentsLoading = false;
        const raw: any[] = Array.isArray(res) ? res : (res?.data ?? []);

        // Filter to chosen class — covers classId, gradeId, and nested variants
        const inClass = raw.filter((s: any) =>
          s.classId   === classId ||
          s.gradeId   === classId ||
          s.grade?.id === classId ||
          s.class?.id === classId
        );

        // If the API already filtered server-side, inClass may be empty — use raw
        const list = inClass.length > 0 ? inClass : raw;

        this.formData.students = list.map((s: any) => ({
          studentId:       s.id,
          studentName:     s.fullName ?? `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
          admissionNumber: s.admissionNumber ?? s.studentNumber ?? s.regNumber ?? '',
          selected:        true,
          status:          'pending' as const,
        }));
      });
  }

  // ── Section events ────────────────────────────────────────────────────────

  onScopeChanged(data: any): void   { this.formData.scope = data; }
  onStudentsChanged(students: BulkInvoiceStudentRow[]): void { this.formData.students = students; }
  onFeeItemsChanged(items: any[]): void { this.formData.feeItems = items; }

  // ── Navigation ────────────────────────────────────────────────────────────

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

  canProceed(): boolean {
    const key = this.steps[this.currentStep]?.key;
    if (key === 'review') return true;
    return this.sectionValid[key] !== false;
  }

  canNavigateTo(i: number): boolean {
    if (i === 0 || i <= this.currentStep) return true;
    return this.completedSteps.has(i - 1);
  }

  isStepCompleted(i: number): boolean {
    return this.completedSteps.has(i);
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  async submitBulk(): Promise<void> {
    const selected = this.formData.students.filter(s => s.selected);
    if (!selected.length) { this._alertService.error('No students selected.'); return; }
    if (!this.formData.feeItems?.length) { this._alertService.error('Add at least one fee item.'); return; }

    this.isSubmitting = true;

    const results: BulkInvoiceStudentRow[] = selected.map(s => ({ ...s, status: 'pending' as const }));

    const dialogRef = this._dialog.open(BulkInvoiceProgressDialogComponent, {
      width: '560px',
      disableClose: true,
      data: { results } as BulkProgressDialogData,
    });

    const scope = this.formData.scope;

    for (const row of results) {
      try {
        const payload = {
          studentId:      row.studentId,
          academicYearId: scope.academicYearId,
          termId:         scope.termId      || undefined,
          invoiceDate:    new Date(scope.invoiceDate).toISOString(),
          dueDate:        new Date(scope.dueDate).toISOString(),
          description:    scope.description || null,
          notes:          this.formData.notes || null,
          tenantId:       this.isSuperAdmin ? scope.tenantId : undefined,
          items: this.formData.feeItems.map((item: any) => ({
            description: item.description,
            itemType:    item.itemType   || null,
            feeItemId:   item.feeItemId  || null,
            quantity:    item.quantity,
            unitPrice:   item.unitPrice,
            discount:    item.discount   || 0,
            isTaxable:   item.isTaxable  || false,
            taxRate:     item.isTaxable  ? (item.taxRate || 0) : null,
            glCode:      item.glCode     || null,
            notes:       item.notes      || null,
          })),
        };

        const res: any = await this._invoiceService.create(payload as any).toPromise();
        row.status = 'success';
        row.invoiceId = res?.data?.id;
        (row as any)['invoiceNumber'] = res?.data?.invoiceNumber;
      } catch (err: any) {
        row.status = 'error';
        row.errorMessage = err?.error?.message || 'Failed';
      }
    }

    this.isSubmitting = false;

    dialogRef.afterClosed().subscribe(() => {
      this.clearDraft();
      const success = results.filter(r => r.status === 'success').length;
      if (success > 0) {
        this._alertService.success(`${success} invoice${success > 1 ? 's' : ''} created successfully!`);
        this._router.navigate(['/finance/invoices']);
      }
    });
  }

  // ── Draft ─────────────────────────────────────────────────────────────────

  private readonly DRAFT_KEY = 'bulk_invoice_draft';

  saveDraft(): void {
    this.isSaving = true;
    this.persistDraft();
    setTimeout(() => { this.isSaving = false; this._alertService.success('Draft saved locally.'); }, 400);
  }

  private persistDraft(): void {
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify({
      formData:       { ...this.formData, students: [] },
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
    }));
  }

  private loadDraft(): void {
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const d = JSON.parse(raw);
      this.formData       = { ...this.formData, ...d.formData };
      this.completedSteps = new Set(d.completedSteps ?? []);
      this.currentStep    = d.currentStep ?? 0;
      this._alertService.info('Draft restored. Continue where you left off.');
    } catch { /* ignore */ }
  }

  private clearDraft(): void { localStorage.removeItem(this.DRAFT_KEY); }

  // ── Helpers ───────────────────────────────────────────────────────────────

  getProgressPct(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    return (2 * Math.PI * 56) * (1 - this.completedSteps.size / (this.steps.length - 1));
  }

  goBack(): void { this._router.navigate(['/finance/invoices']); }

  private checkViewport(): void {
    this.isMobileView = window.innerWidth < 1024;
    if (window.innerWidth < 1280 && window.innerWidth >= 1024) this.isSidebarCollapsed = true;
    if (!this.isMobileView) this.showMobileSidebar = false;
  }
}