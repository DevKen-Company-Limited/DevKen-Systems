// app/Library/library-settings/library-settings.component.ts
import {
  Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, catchError, of, finalize } from 'rxjs';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { LibrarySettingsService } from 'app/core/DevKenService/Library/library-settings.service';
import { SchoolDto } from 'app/Tenant/types/school';
import {
  LibrarySettingsDto,
  UpsertLibrarySettingsRequest,
} from './Types/library-settings.types';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';

@Component({
  selector: 'app-library-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    PageHeaderComponent,
  ],
  templateUrl: './library-settings.component.html',
})
export class LibrarySettingsComponent implements OnInit, OnDestroy {

  private readonly _destroy$ = new Subject<void>();

  // ── State ──────────────────────────────────────────────────────────────────
  form!: FormGroup;
  schools: SchoolDto[] = [];
  isLoading  = true;
  isSaving   = false;
  formSubmitted = false;
  settings: LibrarySettingsDto | null = null;

  // ── Breadcrumbs ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Settings' },
  ];

  // ── Getters ────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get selectedSchoolId(): string | null {
    return this.form?.get('schoolId')?.value ?? null;
  }

  get isDefaultSettings(): boolean {
    return this.settings?.id === '00000000-0000-0000-0000-000000000000';
  }

  constructor(
    private readonly _fb:               FormBuilder,
    private readonly _authService:      AuthService,
    private readonly _alertService:     AlertService,
    private readonly _schoolService:    SchoolService,
    private readonly _settingsService:  LibrarySettingsService,
    private readonly _cdr:              ChangeDetectorRef,
  ) {}

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._buildForm();
    if (this.isSuperAdmin) {
      this._loadSchoolsThenSettings();
    } else {
      this._loadSettings();
    }
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ── Form ───────────────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId:            [null, this.isSuperAdmin ? [Validators.required] : []],
      maxBooksPerStudent:  [2,    [Validators.required, Validators.min(1), Validators.max(50)]],
      maxBooksPerTeacher:  [5,    [Validators.required, Validators.min(1), Validators.max(50)]],
      borrowDaysStudent:   [7,    [Validators.required, Validators.min(1), Validators.max(365)]],
      borrowDaysTeacher:   [14,   [Validators.required, Validators.min(1), Validators.max(365)]],
      finePerDay:          [10,   [Validators.required, Validators.min(0), Validators.max(10000)]],
      allowBookReservation:[true, [Validators.required]],
    });

    // SuperAdmin: reload settings when school changes
    if (this.isSuperAdmin) {
      this.form.get('schoolId')!.valueChanges
        .pipe(takeUntil(this._destroy$))
        .subscribe(schoolId => {
          if (schoolId) this._loadSettings(schoolId);
        });
    }
  }

  private _patch(s: LibrarySettingsDto): void {
    this.form.patchValue({
      maxBooksPerStudent:   s.maxBooksPerStudent,
      maxBooksPerTeacher:   s.maxBooksPerTeacher,
      borrowDaysStudent:    s.borrowDaysStudent,
      borrowDaysTeacher:    s.borrowDaysTeacher,
      finePerDay:           s.finePerDay,
      allowBookReservation: s.allowBookReservation,
    }, { emitEvent: false });
    this._cdr.markForCheck();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  private _loadSchoolsThenSettings(): void {
    this.isLoading = true;
    this._schoolService.getAll()
      .pipe(
        takeUntil(this._destroy$),
        catchError(() => of({ success: false, data: [] as SchoolDto[] })),
        finalize(() => { this.isLoading = false; this._cdr.markForCheck(); }),
      )
      .subscribe(res => {
        this.schools = (res as any).data ?? [];
        this._cdr.markForCheck();
      });
  }

  private _loadSettings(schoolId?: string): void {
    this.isLoading = true;
    this._cdr.markForCheck();

    this._settingsService.get(schoolId)
      .pipe(
        takeUntil(this._destroy$),
        finalize(() => { this.isLoading = false; this._cdr.markForCheck(); }),
      )
      .subscribe({
        next: res => {
          if (res.success && res.data) {
            this.settings = res.data;
            this._patch(res.data);
          } else {
            this._alertService.error(res.message || 'Failed to load settings');
          }
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to load library settings');
        },
      });
  }

  // ── Save ───────────────────────────────────────────────────────────────────
  onSave(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this._cdr.markForCheck();
      return;
    }

    const raw = this.form.value;

    const payload: UpsertLibrarySettingsRequest = {
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      maxBooksPerStudent:   +raw.maxBooksPerStudent,
      maxBooksPerTeacher:   +raw.maxBooksPerTeacher,
      borrowDaysStudent:    +raw.borrowDaysStudent,
      borrowDaysTeacher:    +raw.borrowDaysTeacher,
      finePerDay:           +raw.finePerDay,
      allowBookReservation:  raw.allowBookReservation,
    };

    this.isSaving = true;
    this._cdr.markForCheck();

    this._settingsService.upsert(payload)
      .pipe(
        takeUntil(this._destroy$),
        finalize(() => { this.isSaving = false; this._cdr.markForCheck(); }),
      )
      .subscribe({
        next: res => {
          if (res.success && res.data) {
            this.settings = res.data;
            this._alertService.success('Library settings saved successfully');
            this.formSubmitted = false;
          } else {
            this._alertService.error(res.message || 'Failed to save settings');
          }
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to save library settings');
        },
      });
  }

  onReset(): void {
    if (this.settings) {
      this._patch(this.settings);
      this.formSubmitted = false;
      this._alertService.info?.('Changes reset to last saved values') ;
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('min'))       return `Minimum value is ${c.getError('min').min}`;
    if (c.hasError('max'))       return `Maximum value is ${c.getError('max').max}`;
    return 'Invalid value';
  }

  get isFormDirty(): boolean {
    return this.form.dirty;
  }
}