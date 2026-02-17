import {
  Component, OnInit, OnDestroy, AfterViewInit,
  Inject, ViewChild, ElementRef, inject, ChangeDetectorRef
} from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { CommonModule, AsyncPipe } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, Validators, FormGroup
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { Subject, takeUntil, debounceTime, finalize } from 'rxjs';
import {
  CreateSchoolRequest,
  SchoolDto,
  UpdateSchoolRequest,
  SchoolType,
  SchoolCategory
} from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { API_BASE_URL } from 'app/app.config';
import { SecureImagePipe } from 'app/Tenant/pipe/SecureImagePipe';


@Component({
  selector: 'app-create-edit-school-dialog',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDialogModule,
    MatSelectModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatTabsModule,
    SecureImagePipe
  ],
  templateUrl: './create-edit-school-dialog.component.html',
  styleUrls: ['./create-edit-school-dialog.component.scss']
})
export class CreateEditSchoolDialogComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  private readonly _alertService = inject(AlertService);
  private readonly _apiBaseUrl   = inject(API_BASE_URL);

  form!: FormGroup;
  isSaving        = false;
  isUploadingLogo = false;
  isDraggingOver  = false;

  logoLocalPreview: string | null = null;
  logoServerUrl:    string | null = null;
  logoFile:         File   | null = null;

  readonly schoolTypeOptions = [
    { value: SchoolType.Public,        label: 'Public School (Government Funded)' },
    { value: SchoolType.Private,       label: 'Private School' },
    { value: SchoolType.International, label: 'International Curriculum School' },
    { value: SchoolType.NGO,           label: 'NGO / Mission Sponsored School' }
  ];

  readonly categoryOptions = [
    { value: SchoolCategory.Day,      label: 'Day School' },
    { value: SchoolCategory.Boarding, label: 'Boarding School' },
    { value: SchoolCategory.Mixed,    label: 'Mixed (Day & Boarding)' }
  ];

  readonly kenyanCounties = [
    'Baringo','Bomet','Bungoma','Busia','Elgeyo-Marakwet','Embu','Garissa',
    'Homa Bay','Isiolo','Kajiado','Kakamega','Kericho','Kiambu','Kilifi',
    'Kirinyaga','Kisii','Kisumu','Kitui','Kwale','Laikipia','Lamu','Machakos',
    'Makueni','Mandera','Marsabit','Meru','Migori','Mombasa','Murang\'a',
    'Nairobi','Nakuru','Nandi','Narok','Nyamira','Nyandarua','Nyeri','Samburu',
    'Siaya','Taita-Taveta','Tana River','Tharaka-Nithi','Trans Nzoia','Turkana',
    'Uasin Gishu','Vihiga','Wajir','West Pokot'
  ];

  private _unsubscribe        = new Subject<void>();
  private _slugManuallyEdited = false;

  constructor(
    private fb:        FormBuilder,
    private service:   SchoolService,
    private cdr:       ChangeDetectorRef,          // ← fixes NG0100
    public  dialogRef: MatDialogRef<CreateEditSchoolDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { mode: 'create' | 'edit'; school?: SchoolDto }
  ) {
    // Strips Material's default dialog padding so our header/footer are flush
    dialogRef.addPanelClass(['school-dialog', 'responsive-dialog']);
  }

  get isEdit(): boolean { return this.data.mode === 'edit'; }

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.buildForm();
    this.setupSlugAutoGeneration();
    if (this.isEdit && this.data.school) this.patchForm(this.data.school);
  }

  // Runs after mat-tab-group sets its initial selectedIndex.
  // detectChanges() tells Angular "yes, this change is intentional" and
  // prevents the NG0100 ExpressionChangedAfterItHasBeenChecked error.
  ngAfterViewInit(): void {
    this.cdr.detectChanges();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ─── Form ─────────────────────────────────────────────────────────────────

  private buildForm(): void {
    this.form = this.fb.group({
      name:               ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
      slugName:           ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/), Validators.minLength(3), Validators.maxLength(100)]],
      schoolType:         [SchoolType.Public,  Validators.required],
      category:           [SchoolCategory.Day, Validators.required],
      email:              ['', [Validators.email, Validators.maxLength(150)]],
      phoneNumber:        ['', Validators.maxLength(20)],
      address:            ['', Validators.maxLength(500)],
      county:             [''],
      subCounty:          ['', Validators.maxLength(100)],
      registrationNumber: ['', Validators.maxLength(50)],
      knecCenterCode:     ['', Validators.maxLength(50)],
      kraPin:             ['', Validators.maxLength(50)],
      logoUrl:            [''],
      isActive:           [true]
    });
  }

  private patchForm(school: SchoolDto): void {
    this.form.patchValue({
      name:               school.name,
      slugName:           school.slugName,
      schoolType:         school.schoolType,
      category:           school.category,
      email:              school.email              ?? '',
      phoneNumber:        school.phoneNumber        ?? '',
      address:            school.address            ?? '',
      county:             school.county             ?? '',
      subCounty:          school.subCounty          ?? '',
      registrationNumber: school.registrationNumber ?? '',
      knecCenterCode:     school.knecCenterCode     ?? '',
      kraPin:             school.kraPin             ?? '',
      logoUrl:            school.logoUrl            ?? '',
      isActive:           school.isActive
    });

    if (school.logoUrl) {
      const raw  = school.logoUrl;
      const base = this._apiBaseUrl.replace(/\/$/, '');
      this.logoServerUrl = (raw.startsWith('http://') || raw.startsWith('https://'))
        ? raw
        : `${base}${raw.startsWith('/') ? raw : '/' + raw}`;
    }

    this._slugManuallyEdited = true;
  }

  private setupSlugAutoGeneration(): void {
    if (this.isEdit) return;

    this.form.get('name')!.valueChanges
      .pipe(debounceTime(300), takeUntil(this._unsubscribe))
      .subscribe(name => {
        if (name && !this._slugManuallyEdited) {
          this.form.patchValue({ slugName: this.generateSlug(name) }, { emitEvent: false });
        }
      });

    this.form.get('slugName')!.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(() => { this._slugManuallyEdited = true; });
  }

  generateSlugFromName(): void {
    const name = this.form.get('name')?.value;
    if (name) {
      this.form.patchValue({ slugName: this.generateSlug(name) });
      this._slugManuallyEdited = true;
    }
  }

  private generateSlug(name: string): string {
    return name.toLowerCase()
      .replace(/[^\w\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
  }

  // ─── Logo ─────────────────────────────────────────────────────────────────

  get hasLogo(): boolean { return !!(this.logoLocalPreview || this.logoServerUrl); }

  openFilePicker(): void { this.fileInput.nativeElement.click(); }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.handleLogoFile(input.files[0]);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault(); event.stopPropagation();
    this.isDraggingOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault(); event.stopPropagation();
    this.isDraggingOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.handleLogoFile(file);
  }

  private handleLogoFile(file: File): void {
    const allowed = ['image/jpeg','image/jpg','image/png','image/gif','image/webp'];
    if (!allowed.includes(file.type)) {
      this._alertService.error('Only image files (JPG, PNG, GIF, WebP) are allowed.', 'Invalid File');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this._alertService.error('File size must be under 5 MB.', 'File Too Large');
      return;
    }
    this.logoFile      = file;
    this.logoServerUrl = null;
    const reader       = new FileReader();
    reader.onload      = (e) => { this.logoLocalPreview = e.target?.result as string; };
    reader.readAsDataURL(file);
    this.form.patchValue({ logoUrl: '' });
  }

  removeLogo(): void {
    this.logoFile         = null;
    this.logoLocalPreview = null;
    this.logoServerUrl    = null;
    this.form.patchValue({ logoUrl: '' });
    if (this.fileInput) this.fileInput.nativeElement.value = '';
  }

  // ─── Submit ───────────────────────────────────────────────────────────────

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.scrollToFirstError();
      this._alertService.error('Please fix the errors before saving.', 'Validation Error');
      return;
    }

    this.isSaving = true;
    const raw     = this.form.getRawValue();

    const payload: CreateSchoolRequest | UpdateSchoolRequest = {
      slugName:           raw.slugName.trim(),
      name:               raw.name.trim(),
      schoolType:         raw.schoolType,
      category:           raw.category,
      email:              raw.email?.trim()              || undefined,
      phoneNumber:        raw.phoneNumber?.trim()        || undefined,
      address:            raw.address?.trim()            || undefined,
      county:             raw.county?.trim()             || undefined,
      subCounty:          raw.subCounty?.trim()          || undefined,
      registrationNumber: raw.registrationNumber?.trim() || undefined,
      knecCenterCode:     raw.knecCenterCode?.trim()     || undefined,
      kraPin:             raw.kraPin?.trim()             || undefined,
      logoUrl:            raw.logoUrl?.trim()            || undefined,
      isActive:           raw.isActive
    };

    const save$ = this.isEdit
      ? this.service.update(this.data.school!.id, payload as UpdateSchoolRequest)
      : this.service.create(payload as CreateSchoolRequest);

    save$.pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isSaving = false; })
    ).subscribe({
      next: (response) => {
        if (response?.success && response.data) {
          if (this.logoFile) {
            this.uploadLogoFile(response.data.id);
          } else {
            this._alertService.success(
              this.isEdit ? 'School updated successfully.' : 'School created successfully.',
              this.isEdit ? 'Updated' : 'Created'
            );
            this.dialogRef.close({ success: true, data: response.data });
          }
        } else {
          this._alertService.error(response?.message || 'Operation failed.', 'Error');
        }
      },
      error: () => this._alertService.error('An unexpected error occurred.', 'Error')
    });
  }

  private uploadLogoFile(schoolId: string): void {
    if (!this.logoFile) return;
    this.isUploadingLogo = true;

    this.service.uploadLogo(schoolId, this.logoFile).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isUploadingLogo = false; this.isSaving = false; })
    ).subscribe({
      next: (response) => {
        const label = this.isEdit ? 'updated' : 'created';
        if (response?.success) {
          this._alertService.success(`School ${label} with logo successfully.`, 'Success');
        } else {
          this._alertService.warning(`School ${label}, but logo upload failed: ${response?.message}`, 'Partial Success');
        }
        this.dialogRef.close({ success: true });
      },
      error: () => {
        this._alertService.warning('School saved, but logo upload failed.', 'Partial Success');
        this.dialogRef.close({ success: true });
      }
    });
  }

  // ─── Cancel ───────────────────────────────────────────────────────────────

  cancel(): void {
    if (!this.form.dirty) { this.dialogRef.close({ success: false }); return; }
    this._alertService.confirm({
      title:       'Discard Changes',
      message:     'You have unsaved changes. Are you sure you want to discard them?',
      confirmText: 'Discard',
      cancelText:  'Keep Editing',
      onConfirm:   () => this.dialogRef.close({ success: false })
    });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  hasError(field: string): boolean {
    const c = this.form.get(field);
    return !!c && c.invalid && (c.touched || c.dirty);
  }

  getError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.errors) return '';
    if (ctrl.hasError('required'))  return 'This field is required.';
    if (ctrl.hasError('email'))     return 'Enter a valid email address.';
    if (ctrl.hasError('minlength')) return `Minimum ${ctrl.errors['minlength'].requiredLength} characters.`;
    if (ctrl.hasError('maxlength')) return `Maximum ${ctrl.errors['maxlength'].requiredLength} characters.`;
    if (ctrl.hasError('pattern')) {
      if (field === 'slugName') return 'Only lowercase letters, numbers, and hyphens.';
      return 'Invalid format.';
    }
    return 'Invalid value.';
  }

  private scrollToFirstError(): void {
    const firstInvalid = Object.keys(this.form.controls).find(k => this.form.get(k)?.invalid);
    if (firstInvalid) {
      document.querySelector(`[formControlName="${firstInvalid}"]`)
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}