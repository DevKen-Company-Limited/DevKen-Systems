import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }  from '@angular/material/form-field';
import { MatInputModule }      from '@angular/material/input';
import { MatSelectModule }     from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule }       from '@angular/material/icon';
import { MatCardModule }       from '@angular/material/card';
import { FuseAlertComponent }  from '@fuse/components/alert';
import { SchoolDto } from 'app/Tenant/types/school';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { Subject, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';

// ── Shared lookup type (also imported by invoice-enrollment) ──────────────────
export interface InvoiceLookupItem {
  id:   string;
  name: string;
}

@Component({
  selector:    'app-invoice-details',
  standalone:  true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './invoice-details.component.html',
})
export class InvoiceDetailsComponent implements OnInit, OnChanges {

  // ── Scalar inputs ────────────────────────────────────────────────────────
  @Input() formData:    any     = {};
  @Input() isEditMode:  boolean = false;
  // @Input() isSuperAdmin: boolean = false;

  // ── Lookup arrays — populated by invoice-enrollment.component.ts ─────────
  @Input() students:      InvoiceLookupItem[] = [];
  @Input() academicYears: InvoiceLookupItem[] = [];
  @Input() terms:         InvoiceLookupItem[] = [];
  @Input() parents:       InvoiceLookupItem[] = [];
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private destroy$ = new Subject<void>();
  private alertService= inject (AlertService);
  schools: SchoolDto[] = [];
   // ✅ SuperAdmin getter
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }
  // ── Outputs ──────────────────────────────────────────────────────────────
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  // ────────────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Patch form when parent passes back restored / edit-mode data
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        tenantId:       this.formData?.tenantId       ?? '',
        studentId:      this.formData?.studentId      ?? '',
        academicYearId: this.formData?.academicYearId ?? '',
        termId:         this.formData?.termId         ?? '',
        parentId:       this.formData?.parentId       ?? '',
        invoiceDate:    this.formData?.invoiceDate    ?? new Date(),
        dueDate:        this.formData?.dueDate        ?? '',
        description:    this.formData?.description    ?? '',
      }, { emitEvent: false });
    }

    // When isSuperAdmin changes after form is built, update tenantId validator
    if (changes['isSuperAdmin'] && this.form) {
      this.applyTenantValidator();
    }
  }

  // ────────────────────────────────────────────────────────────────────────
  private buildForm(): void {
    this.form = this.fb.group({
      tenantId:       [this.formData?.tenantId       ?? ''],
      studentId:      [this.formData?.studentId      ?? '', Validators.required],
      academicYearId: [this.formData?.academicYearId ?? '', Validators.required],
      termId:         [this.formData?.termId         ?? ''],
      parentId:       [this.formData?.parentId       ?? ''],
      invoiceDate:    [this.formData?.invoiceDate    ?? new Date(), Validators.required],
      dueDate:        [this.formData?.dueDate        ?? '',         Validators.required],
      description:    [this.formData?.description    ?? '', Validators.maxLength(500)],
    });
    this.applyTenantValidator();
  }

  /** tenantId is only required when SuperAdmin is creating/editing */
  private applyTenantValidator(): void {
    const ctrl = this.form?.get('tenantId');
    if (!ctrl) return;
    if (this.isSuperAdmin) {
      ctrl.setValidators([Validators.required]);
    } else {
      ctrl.clearValidators();
    }
    ctrl.updateValueAndValidity({ emitEvent: false });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(this.form.valid);
    });
  }

  // ── Template helpers ─────────────────────────────────────────────────────
  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required'])  return `${this.fieldLabel(field)} is required`;
    if (c.errors['maxlength']) return `${this.fieldLabel(field)} is too long`;
    return 'Invalid value';
  }

  private fieldLabel(field: string): string {
    const map: Record<string, string> = {
      tenantId:       'School',
      studentId:      'Student',
      academicYearId: 'Academic Year',
      invoiceDate:    'Invoice date',
      dueDate:        'Due date',
    };
    return map[field] ?? field;
  }

   private loadSchools(): void {
    this._schoolService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.schools = res.data ?? [];
        },
        error: (err) => {
          console.error('Failed to load schools', err);
          this.alertService.error('Could not load schools. Please refresh.');
        }
      });
  }
}