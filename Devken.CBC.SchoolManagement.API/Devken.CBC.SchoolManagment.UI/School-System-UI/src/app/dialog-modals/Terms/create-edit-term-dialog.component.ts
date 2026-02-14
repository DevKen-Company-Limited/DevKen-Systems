// create-edit-term-dialog.component.ts
import {
  Component,
  Inject,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
  MatDialogModule
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';
import { SchoolDto } from 'app/Tenant/types/school';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AcademicYearDto } from 'app/Academics/AcademicYear/Types/AcademicYear';
import { TermDto } from 'app/Academics/Terms/Types/types';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';

export interface CreateEditTermDialogData {
  mode: 'create' | 'edit';
  term?: TermDto;
}

export interface CreateEditTermDialogResult {
  formData: any;
}

@Component({
  selector: 'app-create-edit-term-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  templateUrl: './create-edit-term-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `]
})
export class CreateEditTermDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _authService = inject(AuthService);
  private readonly _schoolService = inject(SchoolService);
  private readonly _academicYearService = inject(AcademicYearService);

  // ── Form State ──────────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;

  // ── Dropdown State ──────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];
  academicYears: AcademicYearDto[] = [];
  termNumbers = [
    { value: 1, label: 'Term 1' },
    { value: 2, label: 'Term 2' },
    { value: 3, label: 'Term 3' },
  ];
  isLoading = true;

  // ── Getters ──────────────────────────────────────────────────────────────────
  get isEditMode(): boolean { 
    return this.data.mode === 'edit'; 
  }

  get isSuperAdmin(): boolean { 
    return this._authService.authUser?.isSuperAdmin ?? false; 
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Term' : 'Add New Term';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating ${this.data.term?.name || 'term'} details`
      : 'Fill in the details to create a new academic term';
  }

  get notesLength(): number {
    return this.form.get('notes')?.value?.length || 0;
  }

  // ── Constructor ──────────────────────────────────────────────────────────────
  constructor(
    private readonly _fb: FormBuilder,
    private readonly _dialogRef: MatDialogRef<CreateEditTermDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditTermDialogData,
    private readonly _cdr: ChangeDetectorRef
  ) {
    _dialogRef.addPanelClass(['term-dialog', 'responsive-dialog']);
  }

  ngOnInit(): void {
    this._buildForm();
    this._loadData();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Form Setup ───────────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId: [null, this.isSuperAdmin ? [Validators.required] : []],
      name: ['', [Validators.required, Validators.maxLength(50)]],
      termNumber: [null, [Validators.required]],
      academicYearId: [null, [Validators.required]],
      startDate: [null, [Validators.required]],
      endDate: [null, [Validators.required]],
      isCurrent: [false],
      isClosed: [false],
      notes: ['', [Validators.maxLength(1000)]],
    });

    // Set default term name when term number changes
    this.form.get('termNumber')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(value => {
        if (value && !this.isEditMode) {
          this.form.patchValue({ name: `Term ${value}` }, { emitEvent: false });
        }
      });

    // For SuperAdmin: manage academicYearId field state based on school selection
    if (this.isSuperAdmin) {
      this.form.get('schoolId')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(schoolId => {
          const academicYearControl = this.form.get('academicYearId');
          if (!schoolId) {
            academicYearControl?.setValue(null);
          }
        });
    }
  }

  // ── Data Loading ─────────────────────────────────────────────────────────────
  private _loadData(): void {
    this.isLoading = true;

    const requests: any = {
      academicYears: this._academicYearService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load academic years:', err);
          return of({ success: false, message: '', data: [] });
        })
      ),
    };

    // Add schools request only for SuperAdmin
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load schools:', err);
          return of({ success: false, message: '', data: [] });
        })
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => {
        this.isLoading = false;
        this._cdr.detectChanges();
      })
    ).subscribe({
      next: (results: any) => {
        this.academicYears = results.academicYears.data || [];
        
        if (results.schools) {
          this.schools = results.schools.data || [];
        }

        if (this.isEditMode && this.data.term) {
          this._patchFormWithTerm(this.data.term);
        }
      },
      error: (err) => {
        console.error('Failed to load data:', err);
        if (this.isEditMode && this.data.term) {
          this._patchFormWithTerm(this.data.term);
        }
      }
    });

    // Safety timeout
    setTimeout(() => {
      if (this.isLoading) {
        this.isLoading = false;
        this._cdr.detectChanges();
      }
    }, 12000);
  }

  // ── Form Population ──────────────────────────────────────────────────────────
  private _patchFormWithTerm(term: TermDto): void {
    this.form.patchValue({
      schoolId: term.schoolId || null,
      name: term.name || '',
      termNumber: term.termNumber,
      academicYearId: term.academicYearId || null,
      startDate: term.startDate ? new Date(term.startDate) : null,
      endDate: term.endDate ? new Date(term.endDate) : null,
      isCurrent: term.isCurrent || false,
      isClosed: term.isClosed || false,
      notes: term.notes || '',
    });

    this._cdr.detectChanges();
  }

  // ── Submit & Cancel ──────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      return;
    }

    const result: CreateEditTermDialogResult = {
      formData: this._buildPayload(),
    };

    this._dialogRef.close(result);
  }

  onCancel(): void {
    this._dialogRef.close(null);
  }

  private _buildPayload(): any {
    const v = this.form.getRawValue();

    const toIso = (val: any): string | null => {
      if (!val) return null;
      const d = val instanceof Date ? val : new Date(val);
      return isNaN(d.getTime()) ? null : d.toISOString();
    };

    const payload: any = {
      name: v.name?.trim() || null,
      termNumber: v.termNumber,
      academicYearId: v.academicYearId || null,
      startDate: toIso(v.startDate),
      endDate: toIso(v.endDate),
      isCurrent: v.isCurrent ?? false,
      isClosed: v.isClosed ?? false,
      notes: v.notes?.trim() || null,
    };

    // Add schoolId ONLY for SuperAdmins
    if (this.isSuperAdmin) {
      payload.schoolId = v.schoolId ?? null;
    }

    return payload;
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────
  hasError(field: string, error: string): boolean {
    const c = this.form.get(field);
    return !!(c && (this.formSubmitted || c.touched) && c.hasError(error));
  }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required')) return 'This field is required';
    if (c.hasError('maxlength')) {
      const max = c.getError('maxlength').requiredLength;
      return `Maximum ${max} characters allowed`;
    }
    return 'Invalid value';
  }

  // Get filtered academic years based on selected school (for SuperAdmin)
  get filteredAcademicYears(): AcademicYearDto[] {
    if (!this.isSuperAdmin) {
      return this.academicYears;
    }

    const schoolId = this.form.get('schoolId')?.value;
    if (!schoolId) {
      return [];
    }

    return this.academicYears.filter(ay => ay.schoolId === schoolId);
  }
}