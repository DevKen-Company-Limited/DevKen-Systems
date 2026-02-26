import {
  Component, Inject, OnInit, OnDestroy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup, Validators
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { API_BASE_URL } from 'app/app.config';
import { FeeStructureService } from 'app/core/DevKenService/Finance/FeeStructureService';
import { CBC_LEVEL_OPTIONS, APPLICABLE_TO_OPTIONS, ApplicableTo } from 'app/finance/fee-item/Types/fee-item.model';
import {
  FeeStructureDto,
  FeeItemLookup,
  AcademicYearLookup,
  TermLookup,
  UpdateFeeStructureDto,
  CreateFeeStructureDto,
} from 'app/finance/fee-structure/types/fee-structure.model';
import { ApiResponse } from 'app/Tenant/types/school';

// ─────────────────────────────────────────────────────────────────────────────
// Lookup DTOs
// ─────────────────────────────────────────────────────────────────────────────

export interface SchoolLookup {
  id:   string;
  name: string;
  code: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Dialog input data
// ─────────────────────────────────────────────────────────────────────────────

export interface FeeStructureDialogData {
  mode:          'create' | 'edit';
  item?:         FeeStructureDto;
  isSuperAdmin?: boolean;
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-fee-structure-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './fee-structure-dialog.component.html',
})
export class FeeStructureDialogComponent implements OnInit, OnDestroy {
  private _destroy$     = new Subject<void>();
  private readonly apiBase = inject(API_BASE_URL);

  // ── Form ──────────────────────────────────────────────────────────────────
  form!:         FormGroup;
  isSaving       = false;
  isLoadingLookups = false;

  // ── SuperAdmin school selection ───────────────────────────────────────────
  schools:           SchoolLookup[] = [];
  isLoadingSchools   = false;
  selectedSchoolId:  string | null  = null;

  /**
   * True when the calling user is SuperAdmin.
   * Passed via MAT_DIALOG_DATA.isSuperAdmin.
   */
  get isSuperAdmin(): boolean { return !!this.data.isSuperAdmin; }

  /**
   * For SuperAdmin creating a new record, all controls below the school
   * selector stay disabled until a school is chosen.
   */
  get isFormLocked(): boolean {
    return this.isSuperAdmin && !this.isEdit && !this.selectedSchoolId;
  }

  // ── Lookup data ───────────────────────────────────────────────────────────
  feeItems:      FeeItemLookup[]      = [];
  academicYears: AcademicYearLookup[] = [];
  allTerms:      TermLookup[]         = [];
  filteredTerms: TermLookup[]         = [];

  // ── Static options ────────────────────────────────────────────────────────
  readonly levelOptions      = CBC_LEVEL_OPTIONS;
  readonly applicableOptions = APPLICABLE_TO_OPTIONS;

  get isEdit(): boolean { return this.data.mode === 'edit'; }
  get title():  string  { return this.isEdit ? 'Edit Fee Structure' : 'New Fee Structure'; }

  constructor(
    private fb:        FormBuilder,
    private service:   FeeStructureService,
    private http:      HttpClient,
    private alert:     AlertService,
    private cdr:       ChangeDetectorRef,
    public  dialogRef: MatDialogRef<FeeStructureDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: FeeStructureDialogData,
  ) {}

  ngOnInit(): void {
    this.buildForm();

    if (this.isSuperAdmin) {
      this.loadSchools();

      // When editing as SuperAdmin the school is already set on the item;
      // pre-select it and load contextual lookups immediately.
      if (this.isEdit && this.data.item?.tenantId) {
        this.selectedSchoolId = this.data.item.tenantId;
        this.loadContextualLookups(this.selectedSchoolId);
      }
    } else {
      // Regular user — load all lookups straight away.
      this.loadContextualLookups();
    }
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ── Form Setup ────────────────────────────────────────────────────────────

  private buildForm(): void {
    const item = this.data.item;

    this.form = this.fb.group({
      feeItemId:          [item?.feeItemId      ?? '',   [Validators.required]],
      academicYearId:     [item?.academicYearId ?? '',   [Validators.required]],
      termId:             [item?.termId         ?? null],
      level:              [item?.level          ?? null],
      applicableTo:       [item?.applicableTo   ?? ApplicableTo.All, [Validators.required]],
      amount:             [item?.amount         ?? null, [Validators.required, Validators.min(0.01)]],
      maxDiscountPercent: [item?.maxDiscountPercent ?? null, [Validators.min(0), Validators.max(100)]],
      effectiveFrom:      [item?.effectiveFrom ? new Date(item.effectiveFrom) : null],
      effectiveTo:        [item?.effectiveTo   ? new Date(item.effectiveTo)   : null],
      isActive:           [item?.isActive ?? true],
    });

    // FeeItem, AcademicYear, Term are immutable after creation.
    if (this.isEdit) {
      this.form.get('feeItemId')?.disable();
      this.form.get('academicYearId')?.disable();
      this.form.get('termId')?.disable();
    }

    // Re-filter terms when academic year selection changes.
    this.form.get('academicYearId')?.valueChanges
      .pipe(takeUntil(this._destroy$))
      .subscribe(yearId => {
        this.form.patchValue({ termId: null });
        this.filteredTerms = yearId
          ? this.allTerms.filter(t => t.academicYearId === yearId)
          : [];
      });
  }

  // ── SuperAdmin: School List ───────────────────────────────────────────────

  private loadSchools(): void {
    this.isLoadingSchools = true;
    this.http.get<ApiResponse<SchoolLookup[]>>(`${this.apiBase}/api/schools`)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          this.isLoadingSchools = false;
          if (res.success) this.schools = res.data;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoadingSchools = false;
          this.alert.error('Error', 'Failed to load schools.');
          this.cdr.detectChanges();
        },
      });
  }

  /**
   * Called when the SuperAdmin picks a school from the dropdown.
   * Resets all previously loaded contextual data and re-fetches
   * fee items, academic years, and terms scoped to the chosen school.
   */
  onSchoolChange(schoolId: string | null): void {
    this.selectedSchoolId = schoolId;

    // Clear contextual lookups so stale options don't bleed through.
    this.feeItems      = [];
    this.academicYears = [];
    this.allTerms      = [];
    this.filteredTerms = [];

    // Reset the main form fields that depend on the school.
    this.form.patchValue({
      feeItemId:      '',
      academicYearId: '',
      termId:         null,
    });

    if (schoolId) {
      this.loadContextualLookups(schoolId);
    }

    this.cdr.detectChanges();
  }

  // ── Contextual Lookups (scoped to school) ─────────────────────────────────

  /**
   * Loads fee items, academic years, and terms.
   * Pass schoolId to scope lookups to a specific tenant (SuperAdmin).
   * Omit it to let the server use the JWT tenant (regular users).
   */
  private loadContextualLookups(schoolId?: string): void {
    this.isLoadingLookups = true;

    let loaded = 0;
    const done = () => {
      if (++loaded === 3) {
        this.isLoadingLookups = false;
        this.cdr.detectChanges();
      }
    };

    // Build optional query string for SuperAdmin scoping.
    const schoolQuery = schoolId ? `?schoolId=${schoolId}` : '';

    // ── Fee Items ──────────────────────────────────────────────────────────
    this.http
      .get<ApiResponse<FeeItemLookup[]>>(
        `${this.apiBase}/api/finance/feeitems${schoolQuery}`
      )
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => { if (res.success) this.feeItems = res.data; done(); },
        error: ()  => done(),
      });

    // ── Academic Years ─────────────────────────────────────────────────────
    this.http
      .get<ApiResponse<AcademicYearLookup[]>>(
        `${this.apiBase}/api/academic/academicyear${schoolQuery}`
      )
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) this.academicYears = res.data;
          const currentYear = this.form.get('academicYearId')?.value;
          if (currentYear) {
            this.filteredTerms = this.allTerms.filter(
              t => t.academicYearId === currentYear
            );
          }
          done();
        },
        error: () => done(),
      });

    // ── Terms ──────────────────────────────────────────────────────────────
    this.http
      .get<ApiResponse<TermLookup[]>>(
        `${this.apiBase}/api/academic/terms${schoolQuery}`
      )
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allTerms = res.data;
            const currentYear = this.form.get('academicYearId')?.value;
            if (currentYear) {
              this.filteredTerms = this.allTerms.filter(
                t => t.academicYearId === currentYear
              );
            }
          }
          done();
        },
        error: () => done(),
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  fieldError(name: string): string | null {
    const ctrl = this.form.get(name);
    if (!ctrl?.invalid || !ctrl.touched) return null;
    if (ctrl.errors?.['required']) return 'This field is required.';
    if (ctrl.errors?.['min'])      return `Minimum value is ${ctrl.errors['min'].min}.`;
    if (ctrl.errors?.['max'])      return `Maximum value is ${ctrl.errors['max'].max}.`;
    return 'Invalid value.';
  }

  private formatDate(d: Date | null): string | null {
    if (!d) return null;
    return d instanceof Date ? d.toISOString() : d;
  }

  /** Display name for the currently selected school in the locked state banner. */
  get selectedSchoolName(): string {
    return this.schools.find(s => s.id === this.selectedSchoolId)?.name ?? '';
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // Extra guard: SuperAdmin must have a school selected before creating.
    if (this.isSuperAdmin && !this.isEdit && !this.selectedSchoolId) {
      this.alert.error('School Required', 'Please select a school before saving.');
      return;
    }

    this.isSaving = true;
    const raw = this.form.getRawValue(); // includes disabled controls

    if (this.isEdit) {
      const dto: UpdateFeeStructureDto = {
        level:              raw.level,
        applicableTo:       raw.applicableTo,
        amount:             +raw.amount,
        maxDiscountPercent: raw.maxDiscountPercent != null ? +raw.maxDiscountPercent : null,
        effectiveFrom:      this.formatDate(raw.effectiveFrom),
        effectiveTo:        this.formatDate(raw.effectiveTo),
        isActive:           raw.isActive,
      };

      this.service.update(this.data.item!.id, dto)
        .pipe(takeUntil(this._destroy$))
        .subscribe({
          next: res => {
            this.isSaving = false;
            if (res.success) {
              this.alert.success('Updated', 'Fee structure saved successfully.');
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this.alert.error('Error', res.message);
            }
          },
          error: err => {
            this.isSaving = false;
            this.alert.error('Error', err?.error?.message ?? 'Failed to update.');
          },
        });
    } else {
      const dto: CreateFeeStructureDto = {
        // SuperAdmin explicitly sends tenantId; regular users omit it
        // (the server picks it up from the JWT).
        ...(this.isSuperAdmin && this.selectedSchoolId
          ? { tenantId: this.selectedSchoolId }
          : {}),
        feeItemId:          raw.feeItemId,
        academicYearId:     raw.academicYearId,
        termId:             raw.termId || null,
        level:              raw.level,
        applicableTo:       raw.applicableTo,
        amount:             +raw.amount,
        maxDiscountPercent: raw.maxDiscountPercent != null ? +raw.maxDiscountPercent : null,
        effectiveFrom:      this.formatDate(raw.effectiveFrom),
        effectiveTo:        this.formatDate(raw.effectiveTo),
        isActive:           raw.isActive,
      };

      this.service.create(dto)
        .pipe(takeUntil(this._destroy$))
        .subscribe({
          next: res => {
            this.isSaving = false;
            if (res.success) {
              this.alert.success('Created', 'Fee structure created successfully.');
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this.alert.error('Error', res.message);
            }
          },
          error: err => {
            this.isSaving = false;
            this.alert.error('Error', err?.error?.message ?? 'Failed to create.');
          },
        });
    }
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}