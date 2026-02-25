// subject-identity/subject-identity.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { SchoolDto } from 'app/Tenant/types/school';

@Component({
  selector: 'app-subject-identity',
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
  templateUrl: './subject-identity.component.html',
})
export class SubjectIdentityComponent implements OnInit, OnChanges {

  @Input() formData: any = {};
  @Input() schools: SchoolDto[] = [];
  @Input() isEditMode = false;
  @Input() isSuperAdmin = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
    }
  }

  private buildForm(): void {
    const config: any = {
      name:        [this.formData?.name        ?? '', [Validators.required, Validators.maxLength(200)]],
      code:        [{ value: this.formData?.code ?? '', disabled: true }],
      description: [this.formData?.description ?? '', Validators.maxLength(500)],
    };

    if (this.isSuperAdmin) {
      config['schoolId'] = [this.formData?.schoolId ?? '', Validators.required];
    }

    this.form = this.fb.group(config);
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
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
      name: 'Subject name', schoolId: 'School', description: 'Description',
    };
    return map[field] ?? field;
  }
}