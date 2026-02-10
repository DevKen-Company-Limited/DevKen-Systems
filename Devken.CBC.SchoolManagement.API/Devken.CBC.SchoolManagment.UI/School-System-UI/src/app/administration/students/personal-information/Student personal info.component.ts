import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { EnumService, EnumItemDto } from 'app/core/DevKenService/common/enum.service';

@Component({
  selector: 'app-student-personal-info',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './Student personal info.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss', './Student personal info.component.scss'],
})
export class StudentPersonalInfoComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  private destroyRef = inject(DestroyRef);
  private enumService = inject(EnumService);

  form!: FormGroup;
  photoPreview: string | null = null;

  // Enum Observables
  genders$: Observable<EnumItemDto[]> = of([]);
  studentStatuses$: Observable<EnumItemDto[]> = of([]);
  cbcLevels$: Observable<EnumItemDto[]> = of([]);

  readonly MAX_FILE_SIZE = 2 * 1024 * 1024; // 2MB
  readonly ACCEPTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

  ngOnInit(): void {
    this.buildForm();
    this.setupFormListeners();
    this.loadEnums();
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
      firstName: [this.formData?.firstName ?? '', [Validators.required, Validators.minLength(2)]],
      lastName: [this.formData?.lastName ?? '', [Validators.required, Validators.minLength(2)]],
      middleName: [this.formData?.middleName ?? ''],
      gender: [this.formData?.gender ?? '', Validators.required],
      admissionNumber: [this.formData?.admissionNumber ?? '', Validators.required],
      nemisNumber: [this.formData?.nemisNumber ? Number(this.formData.nemisNumber) : null],
      birthCertificateNumber: [this.formData?.birthCertificateNumber ?? ''],
      dateOfBirth: [this.formData?.dateOfBirth ?? '', Validators.required],
      religion: [this.formData?.religion ?? ''],
      nationality: [this.formData?.nationality ?? 'Kenyan'],
      dateOfAdmission: [this.formData?.dateOfAdmission ?? this.today(), Validators.required],
      photoUrl: [this.formData?.photoUrl ?? ''],
      notes: [this.formData?.notes ?? ''],
      isActive: [this.formData?.isActive ?? true],
      studentStatus: [this.formData?.studentStatus ?? '', Validators.required],
      cbcLevel: [this.formData?.cbcLevel ?? '', Validators.required],
    });

    if (this.formData?.photoUrl) this.photoPreview = this.formData.photoUrl;
  }

  private setupFormListeners(): void {
    this.form.valueChanges
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        map(value => this.prepareFormValue(value))
      )
      .subscribe(value => {
        this.formChanged.emit(value);
        this.formValid.emit(this.form.valid);
      });
  }

  /** Convert numeric strings to numbers before sending */
  private prepareFormValue(value: any): any {
    return {
      ...value,
      nemisNumber: value.nemisNumber !== null && value.nemisNumber !== '' ? Number(value.nemisNumber) : null,
      gender: Number(value.gender),           // ensure numeric value
      studentStatus: Number(value.studentStatus),
      cbcLevel: Number(value.cbcLevel),
    };
  }

  /** Load enums from backend */
  private loadEnums(): void {
    this.genders$ = this.enumService.getGenders().pipe(
      catchError(err => {
        console.error('Failed to load Gender enum', err);
        return of([]);
      })
    );

    this.studentStatuses$ = this.enumService.getStudentStatuses().pipe(
      catchError(err => {
        console.error('Failed to load StudentStatus enum', err);
        return of([]);
      })
    );

    this.cbcLevels$ = this.enumService.getCBCLevels().pipe(
      catchError(err => {
        console.error('Failed to load CBCLevel enum', err);
        return of([]);
      })
    );
  }

  /** Form validation helpers */
  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.form.get(field);
    if (!control || !control.errors) return '';
    if (control.errors['required']) return `${this.getFieldLabel(field)} is required`;
    if (control.errors['minlength']) {
      const minLength = control.errors['minlength'].requiredLength;
      return `Minimum ${minLength} characters required`;
    }
    return 'Invalid value';
  }

  private getFieldLabel(field: string): string {
    const labels: Record<string, string> = {
      firstName: 'First name',
      lastName: 'Last name',
      gender: 'Gender',
      admissionNumber: 'Admission number',
      dateOfBirth: 'Date of birth',
      dateOfAdmission: 'Date of admission',
      studentStatus: 'Student status',
      cbcLevel: 'CBC level',
    };
    return labels[field] || field;
  }

  /** Calculate age from dateOfBirth */
  calculateAge(dob: string): string {
    if (!dob) return '';
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) age--;
    return `${age} year${age !== 1 ? 's' : ''} old`;
  }

  /** Photo uploader */
  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!this.ACCEPTED_IMAGE_TYPES.includes(file.type)) {
      this.showPhotoError('Please select a valid image file (JPG, PNG, or WebP)');
      input.value = '';
      return;
    }

    if (file.size > this.MAX_FILE_SIZE) {
      this.showPhotoError('Image size must be less than 2MB');
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = e => {
      this.photoPreview = e.target?.result as string;
      this.form.patchValue({ photoUrl: this.photoPreview });
    };
    reader.onerror = () => {
      this.showPhotoError('Failed to read image file');
      input.value = '';
    };
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoPreview = null;
    this.form.patchValue({ photoUrl: '' });
    const input = document.getElementById('photoInput') as HTMLInputElement;
    if (input) input.value = '';
  }

  private showPhotoError(message: string): void {
    console.error(message);
    alert(message);
  }

  private today(): string {
    return new Date().toISOString().split('T')[0];
  }

  get isMobileView(): boolean {
    return window.innerWidth < 768;
  }
}
