import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
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
import { GenderOptions, StudentStatusOptions, CBCLevelOptions, normalizeStudentEnums, StudentStatus } from '../../types/Enums';



@Component({
  selector: 'app-student-personal-info',
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
  templateUrl: './student-personal-info.component.html',
})
export class StudentPersonalInfoComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);

  form!: FormGroup;

  // Date constraints
  today = new Date();
  minDateOfBirth = new Date(this.today.getFullYear() - 25, this.today.getMonth(), this.today.getDate());
  maxDateOfBirth = new Date(this.today.getFullYear() - 2, this.today.getMonth(), this.today.getDate());
  minDateOfAdmission = new Date(2000, 0, 1);
  maxDateOfAdmission = new Date(this.today.getFullYear() + 1, 11, 31);

  // Use centralized enum options
  genders = GenderOptions;
  studentStatuses = StudentStatusOptions;
  cbcLevels = CBCLevelOptions;

  ngOnInit(): void {
    this.buildForm();
    this.setupFormListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      // Use centralized normalization utility
      const patchData = normalizeStudentEnums(this.formData);
      
      // Convert dates
      if (patchData.dateOfBirth) {
        patchData.dateOfBirth = new Date(patchData.dateOfBirth);
      }
      if (patchData.dateOfAdmission) {
        patchData.dateOfAdmission = new Date(patchData.dateOfAdmission);
      }
      
      console.log('[Personal Info] Normalized data:', patchData);
      this.form.patchValue(patchData, { emitEvent: false });
    }
  }

  private buildForm(): void {
    // Use centralized normalization
    const normalizedData = normalizeStudentEnums(this.formData);
    
    // Parse dates
    const dateOfBirth = normalizedData?.dateOfBirth ? new Date(normalizedData.dateOfBirth) : null;
    const dateOfAdmission = normalizedData?.dateOfAdmission ? new Date(normalizedData.dateOfAdmission) : new Date();
    
    this.form = this.fb.group({
      firstName: [normalizedData?.firstName ?? '', [Validators.required, Validators.maxLength(100)]],
      middleName: [normalizedData?.middleName ?? '', Validators.maxLength(100)],
      lastName: [normalizedData?.lastName ?? '', [Validators.required, Validators.maxLength(100)]],
      admissionNumber: [normalizedData?.admissionNumber ?? ''], 
      nemisNumber: [normalizedData?.nemisNumber ?? '', Validators.maxLength(50)],
      birthCertificateNumber: [normalizedData?.birthCertificateNumber ?? '', Validators.maxLength(50)],
      dateOfBirth: [dateOfBirth, Validators.required],
      dateOfAdmission: [dateOfAdmission],
      gender: [normalizedData?.gender ?? '', Validators.required],
      religion: [normalizedData?.religion ?? '', Validators.maxLength(50)],
      nationality: [normalizedData?.nationality ?? 'Kenyan', Validators.maxLength(50)],
      photoUrl: [normalizedData?.photoUrl ?? ''],
      studentStatus: [normalizedData?.studentStatus ?? StudentStatus.Active], // Default: Active (1)
      cbcLevel: [normalizedData?.cbcLevel ?? '', Validators.required],
    });
  }

  private setupFormListeners(): void {
    this.form.valueChanges.subscribe(value => {
      // Convert enum values to numbers for backend
      const emitValue = {
        ...value,
        gender: value.gender !== null && value.gender !== '' ? Number(value.gender) : null,
        studentStatus: value.studentStatus !== null && value.studentStatus !== '' ? Number(value.studentStatus) : null,
        cbcLevel: value.cbcLevel !== null && value.cbcLevel !== '' ? Number(value.cbcLevel) : null,
      };
      
      console.log('[Personal Info] Emitting:', emitValue);
      this.formChanged.emit(emitValue);
      this.formValid.emit(this.form.valid);
    });
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.form.get(field);
    if (!control || !control.errors) return '';

    if (control.errors['required']) return `${this.getFieldLabel(field)} is required`;
    if (control.errors['maxlength']) return `${this.getFieldLabel(field)} is too long`;
    if (control.errors['matDatepickerMin']) return 'Date is too far in the past (max 25 years ago)';
    if (control.errors['matDatepickerMax']) return 'Date is too recent (student must be at least 2 years old)';

    return 'Invalid value';
  }

  private getFieldLabel(field: string): string {
    const labels: { [key: string]: string } = {
      firstName: 'First name',
      lastName: 'Last name',
      dateOfBirth: 'Date of birth',
      gender: 'Gender',
      cbcLevel: 'CBC Level',
    };
    return labels[field] || field;
  }
}