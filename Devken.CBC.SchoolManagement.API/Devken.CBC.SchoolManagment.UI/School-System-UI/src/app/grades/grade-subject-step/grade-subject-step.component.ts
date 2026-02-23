// grade-subject-step/grade-subject-step.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule }           from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }     from '@angular/material/form-field';
import { MatInputModule }         from '@angular/material/input';
import { MatSelectModule }        from '@angular/material/select';
import { MatIconModule }          from '@angular/material/icon';
import { MatCardModule }          from '@angular/material/card';
import { FuseAlertComponent }     from '@fuse/components/alert';
import { SchoolDto }              from 'app/Tenant/types/school';

@Component({
  selector: 'app-grade-subject-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatCardModule, FuseAlertComponent,
  ],
  templateUrl: './grade-subject-step.component.html',
})
export class GradeSubjectStepComponent implements OnInit, OnChanges {
  @Input() formData:    any       = {};
  @Input() schools:     SchoolDto[] = [];
  @Input() isEditMode   = false;
  @Input() isSuperAdmin = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this._buildForm();
    this._setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
    }
  }

  private _buildForm(): void {
    const cfg: any = {
      studentId:    [this.formData?.studentId    ?? '', Validators.required],
      subjectId:    [this.formData?.subjectId    ?? '', Validators.required],
      termId:       [this.formData?.termId       ?? null],
      assessmentId: [this.formData?.assessmentId ?? null],
    };

    if (this.isSuperAdmin) {
      cfg['tenantId'] = [this.formData?.tenantId ?? '', Validators.required];
    }

    this.form = this.fb.group(cfg);

    if (this.isEditMode) {
      this.form.get('studentId')?.disable();
      this.form.get('subjectId')?.disable();
    }
  }

  private _setupListeners(): void {
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
    if (c.errors['required']) return `${this._label(field)} is required`;
    return 'Invalid value';
  }

  private _label(field: string): string {
    const map: Record<string, string> = {
      studentId: 'Student', subjectId: 'Subject', tenantId: 'School',
    };
    return map[field] ?? field;
  }
}