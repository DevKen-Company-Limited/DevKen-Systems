import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from 'app/core/auth/auth.service';

@Component({
  selector: 'app-student-academic',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
  ],
  templateUrl: './student-academic.component.html',
})
export class StudentAcademicComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() schools: any[] = [];
  @Input() classes: any[] = [];
  @Input() academicYears: any[] = [];
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  form!: FormGroup;

  get isSuperAdmin(): boolean {
    return this.authService.authUser?.isSuperAdmin ?? false;
  }

  ngOnInit(): void {
    this.buildForm();
    this.setupFormListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
    }
  }

  private buildForm(): void {
    const formConfig: any = {
      currentLevel: [this.formData?.currentLevel ?? '', Validators.required],
      currentClassId: [this.formData?.currentClassId ?? ''],
      currentAcademicYearId: [this.formData?.currentAcademicYearId ?? ''],
      previousSchool: [this.formData?.previousSchool ?? ''],
      status: [this.formData?.status ?? ''],
    };

    // Add schoolId field for SuperAdmin
    if (this.isSuperAdmin) {
      formConfig.schoolId = [this.formData?.schoolId ?? '', Validators.required];
    }

    this.form = this.fb.group(formConfig);
  }

  private setupFormListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(this.form.valid);
    });
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
}