import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-personal-info',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './Student personal info.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss'],
})
export class StudentPersonalInfoComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form!: FormGroup;
  photoPreview: string | null = null;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.buildForm();
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(this.form.valid);
    });
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
      if (this.formData?.photoUrl) this.photoPreview = this.formData.photoUrl;
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      firstName:            [this.formData?.firstName            ?? '', Validators.required],
      lastName:             [this.formData?.lastName             ?? '', Validators.required],
      middleName:           [this.formData?.middleName           ?? ''],
      gender:               [this.formData?.gender               ?? '', Validators.required],
      admissionNumber:      [this.formData?.admissionNumber      ?? '', Validators.required],
      nemisNumber:          [this.formData?.nemisNumber          ?? ''],
      birthCertificateNumber:[this.formData?.birthCertificateNumber ?? ''],
      dateOfBirth:          [this.formData?.dateOfBirth          ?? '', Validators.required],
      religion:             [this.formData?.religion             ?? ''],
      nationality:          [this.formData?.nationality          ?? 'Kenyan'],
      dateOfAdmission:      [this.formData?.dateOfAdmission      ?? this.today(), Validators.required],
      photoUrl:             [this.formData?.photoUrl             ?? ''],
      notes:                [this.formData?.notes               ?? ''],
      isActive:             [this.formData?.isActive             ?? true],
    });
    if (this.formData?.photoUrl) this.photoPreview = this.formData.photoUrl;
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  calculateAge(dob: string): string {
    if (!dob) return '';
    const diff = Date.now() - new Date(dob).getTime();
    const age  = Math.floor(diff / (365.25 * 24 * 60 * 60 * 1000));
    return `${age} year${age !== 1 ? 's' : ''} old`;
  }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.photoPreview = e.target?.result as string;
      this.form.patchValue({ photoUrl: this.photoPreview });
    };
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoPreview = null;
    this.form.patchValue({ photoUrl: '' });
  }

  private today(): string {
    return new Date().toISOString().split('T')[0];
  }
}