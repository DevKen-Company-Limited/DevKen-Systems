import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule }     from '@angular/material/input';
import { MatSelectModule }    from '@angular/material/select';
import { MatIconModule }      from '@angular/material/icon';
import { MatCardModule }      from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { ParentRelationship } from 'app/Academics/Parents/Types/Parent.types';
import { SchoolDto }          from 'app/Tenant/types/school';

@Component({
  selector: 'app-parent-basic',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './parent-basic.component.html',
})
export class ParentBasicComponent implements OnInit, OnChanges {

  @Input() formData:     any       = {};
  @Input() isEditMode:   boolean   = false;
  @Input() isSuperAdmin: boolean   = false;
  @Input() schools:      SchoolDto[] = [];

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  relationships = [
    { value: ParentRelationship.Father,      label: 'Father'      },
    { value: ParentRelationship.Mother,      label: 'Mother'      },
    { value: ParentRelationship.Guardian,    label: 'Guardian'    },
    { value: ParentRelationship.Sibling,     label: 'Sibling'     },
    { value: ParentRelationship.Grandparent, label: 'Grandparent' },
    { value: ParentRelationship.Uncle,       label: 'Uncle'       },
    { value: ParentRelationship.Aunt,        label: 'Aunt'        },
    { value: ParentRelationship.Other,       label: 'Other'       },
  ];

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        tenantId:     this.formData?.tenantId     ?? '',
        firstName:    this.formData?.firstName    ?? '',
        middleName:   this.formData?.middleName   ?? '',
        lastName:     this.formData?.lastName     ?? '',
        relationship: this.formData?.relationship ?? null,
      }, { emitEvent: false });
    }

    // If isSuperAdmin changes after form is built, add/remove the control
    if (changes['isSuperAdmin'] && this.form) {
      this.applyTenantValidator();
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      tenantId:     [this.formData?.tenantId     ?? ''],
      firstName:    [this.formData?.firstName    ?? '', [Validators.required, Validators.maxLength(100)]],
      middleName:   [this.formData?.middleName   ?? '', Validators.maxLength(100)],
      lastName:     [this.formData?.lastName     ?? '', [Validators.required, Validators.maxLength(100)]],
      relationship: [this.formData?.relationship ?? null, Validators.required],
    });
    this.applyTenantValidator();
  }

  /** Add Required validator on tenantId only for SuperAdmin */
  private applyTenantValidator(): void {
    const ctrl = this.form.get('tenantId');
    if (!ctrl) return;
    if (this.isSuperAdmin) {
      ctrl.setValidators([Validators.required]);
    } else {
      ctrl.clearValidators();
    }
    ctrl.updateValueAndValidity({ emitEvent: false });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(() => {
      this.formChanged.emit(this.form.getRawValue());
      this.formValid.emit(this.form.valid);
    });
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required'])  return `${this.label(field)} is required`;
    if (c.errors['maxlength']) return `${this.label(field)} is too long`;
    return 'Invalid value';
  }

  private label(field: string): string {
    const map: Record<string, string> = {
      firstName:    'First name',
      middleName:   'Middle name',
      lastName:     'Last name',
      relationship: 'Relationship',
      tenantId:     'School',
    };
    return map[field] ?? field;
  }
}