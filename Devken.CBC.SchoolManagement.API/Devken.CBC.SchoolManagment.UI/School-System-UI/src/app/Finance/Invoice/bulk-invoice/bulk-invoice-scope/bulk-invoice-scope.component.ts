import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { SchoolDto } from 'app/Tenant/types/school';
import { InvoiceLookupItem } from '../../invoice-details/invoice-details.component';
import { ClassDto } from '../../Types/bulk-invoice.types';

@Component({
  selector: 'app-bulk-invoice-scope',
  standalone: true,
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
  templateUrl: './bulk-invoice-scope.component.html',
})
export class BulkInvoiceScopeComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() isSuperAdmin = false;
  @Input() schools: SchoolDto[] = [];
  @Input() classes: ClassDto[] = [];
  @Input() academicYears: InvoiceLookupItem[] = [];
  @Input() terms: InvoiceLookupItem[] = [];

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();
  @Output() schoolIdChanged = new EventEmitter<string>();
  @Output() classIdChanged = new EventEmitter<string>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        tenantId:       this.formData?.tenantId       ?? '',
        classId:        this.formData?.classId        ?? '',
        academicYearId: this.formData?.academicYearId ?? '',
        termId:         this.formData?.termId         ?? '',
        invoiceDate:    this.formData?.invoiceDate    ?? new Date(),
        dueDate:        this.formData?.dueDate        ?? '',
        description:    this.formData?.description    ?? '',
      }, { emitEvent: false });
    }
    if (changes['isSuperAdmin'] && this.form) {
      this.applyTenantValidator();
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      tenantId:       [this.formData?.tenantId       ?? ''],
      classId:        [this.formData?.classId        ?? '', Validators.required],
      academicYearId: [this.formData?.academicYearId ?? '', Validators.required],
      termId:         [this.formData?.termId         ?? ''],
      invoiceDate:    [this.formData?.invoiceDate    ?? new Date(), Validators.required],
      dueDate:        [this.formData?.dueDate        ?? '', Validators.required],
      description:    [this.formData?.description    ?? ''],
    });
    this.applyTenantValidator();
  }

  private applyTenantValidator(): void {
    const ctrl = this.form?.get('tenantId');
    if (!ctrl) return;
    ctrl.setValidators(this.isSuperAdmin ? [Validators.required] : []);
    ctrl.updateValueAndValidity({ emitEvent: false });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(this.form.valid);
    });
    this.form.get('tenantId')?.valueChanges.subscribe(id => {
      if (id) this.schoolIdChanged.emit(id);
    });
    this.form.get('classId')?.valueChanges.subscribe(id => {
      if (id) this.classIdChanged.emit(id);
    });
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors || !c.touched) return '';
    if (c.errors['required']) return `${this.fieldLabel(field)} is required`;
    return 'Invalid value';
  }

  private fieldLabel(field: string): string {
    const map: Record<string, string> = {
      tenantId: 'School', classId: 'Class',
      academicYearId: 'Academic Year', invoiceDate: 'Invoice date', dueDate: 'Due date',
    };
    return map[field] ?? field;
  }
}