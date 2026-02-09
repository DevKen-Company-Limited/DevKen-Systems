import { Component, OnInit, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, debounceTime } from 'rxjs';
import { 
  CreateSchoolRequest, 
  SchoolDto, 
  UpdateSchoolRequest 
} from 'app/Tenant/types/school';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';

@Component({
  selector: 'app-create-edit-school-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSnackBarModule,
    MatSelectModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTooltipModule
  ],
  templateUrl: './create-edit-school-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `]
})
export class CreateEditSchoolDialogComponent
  extends BaseFormDialog<CreateSchoolRequest, UpdateSchoolRequest, SchoolDto, { mode: 'create' | 'edit'; school?: SchoolDto }>
  implements OnInit, OnDestroy
{
  private _unsubscribe = new Subject<void>();
  private _slugGenerated = false;

  constructor(
    fb: FormBuilder,
    protected service: SchoolService,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditSchoolDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public override data: { mode: 'create' | 'edit'; school?: SchoolDto }
  ) {
    super(fb, service, snackBar, dialogRef, data);
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      slugName: ['', [
        Validators.required,
        Validators.pattern(/^[a-z0-9-]+$/),
        Validators.minLength(3),
        Validators.maxLength(50)
      ]],
      name: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(100)
      ]],
      address: ['', Validators.maxLength(200)],
      phoneNumber: ['', Validators.pattern(/^[\d\s\-\+\(\)]{10,}$/)],
      email: ['', [Validators.email, Validators.maxLength(100)]],
      website: ['', Validators.pattern(/^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$/)],
      logoUrl: ['', Validators.pattern(/^(https?:\/\/.*\.(?:png|jpg|jpeg|gif|svg))$/i)],
      description: ['', Validators.maxLength(500)],
      maxUsers: [100, [Validators.min(1), Validators.max(10000)]],
      isActive: [true],
      timeZone: ['UTC'],
      language: ['en'],
      currency: ['USD']
    });
  }

  ngOnInit(): void {
    this.init();
    this.setupSlugGeneration();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  /** Auto-generate slug from name when in create mode */
  private setupSlugGeneration(): void {
    if (this.data.mode === 'create') {
      this.form.get('name')?.valueChanges
        .pipe(
          debounceTime(300),
          takeUntil(this._unsubscribe)
        )
        .subscribe(name => {
          // Only auto-generate if user hasn't manually modified slug
          if (name && !this._slugGenerated) {
            const slug = this.service.generateSlug(name);
            this.form.patchValue({ slugName: slug }, { emitEvent: false });
          }
        });

      // Track manual slug modifications
      this.form.get('slugName')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(() => {
          this._slugGenerated = true;
        });
    }
  }

  /** Manual slug generation button */
  generateSlug(): void {
    const name = this.form.get('name')?.value;
    if (name) {
      const slug = this.service.generateSlug(name);
      this.form.patchValue({ slugName: slug });
      this._slugGenerated = true;
    }
  }

  /** Override to add validation before save */
  submit() {
    // Mark all fields as touched to trigger validation display
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      // Scroll to first error
      const firstError = Object.keys(this.form.controls)
        .find(key => this.form.get(key)?.invalid);
      
      if (firstError) {
        const element = document.querySelector(`[formControlName="${firstError}"]`);
        element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
      
      this.snackBar.open('Please fix the errors in the form', 'Close', {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'top'
      });
      return;
    }

    this.save(
      // createMapper
      (raw) => ({
        slugName: (raw.slugName ?? '').trim(),
        name: (raw.name ?? '').trim(),
        address: raw.address?.trim() || undefined,
        phoneNumber: raw.phoneNumber?.trim() || undefined,
        email: raw.email?.trim() || undefined,
        website: raw.website?.trim() || undefined,
        logoUrl: raw.logoUrl?.trim() || undefined,
        description: raw.description?.trim() || undefined,
        maxUsers: raw.maxUsers || 100,
        isActive: raw.isActive ?? true,
        timeZone: raw.timeZone || 'UTC',
        language: raw.language || 'en',
        currency: raw.currency || 'USD'
      } as CreateSchoolRequest),

      // updateMapper
      (raw) => ({
        slugName: (raw.slugName ?? '').trim(),
        name: (raw.name ?? '').trim(),
        address: raw.address?.trim() || undefined,
        phoneNumber: raw.phoneNumber?.trim() || undefined,
        email: raw.email?.trim() || undefined,
        website: raw.website?.trim() || undefined,
        logoUrl: raw.logoUrl?.trim() || undefined,
        description: raw.description?.trim() || undefined,
        maxUsers: raw.maxUsers || 100,
        isActive: raw.isActive ?? true,
        timeZone: raw.timeZone || 'UTC',
        language: raw.language || 'en',
        currency: raw.currency || 'USD'
      } as UpdateSchoolRequest),

      // getId
      () => this.data.school?.id ?? ''
    );
  }

  /** Cancel with confirmation if form has changes */
  cancel(): void {
    if (this.form.dirty && !confirm('You have unsaved changes. Are you sure you want to cancel?')) {
      return;
    }
    this.close({ success: false });
  }

  /** Check if a field has an error */
  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return field ? field.invalid && (field.touched || field.dirty) : false;
  }

  /** Get error message for a field */
  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (!field?.errors) return '';

    if (field.hasError('required')) {
      return 'This field is required';
    }
    if (field.hasError('email')) {
      return 'Please enter a valid email address';
    }
    if (field.hasError('pattern')) {
      switch (fieldName) {
        case 'slugName':
          return 'Only lowercase letters, numbers, and hyphens allowed';
        case 'phoneNumber':
          return 'Please enter a valid phone number';
        case 'website':
          return 'Please enter a valid website URL';
        case 'logoUrl':
          return 'Please enter a valid image URL (PNG, JPG, GIF, SVG)';
        default:
          return 'Invalid format';
      }
    }
    if (field.hasError('minlength')) {
      return `Minimum ${field.errors['minlength'].requiredLength} characters required`;
    }
    if (field.hasError('maxlength')) {
      return `Maximum ${field.errors['maxlength'].requiredLength} characters allowed`;
    }
    if (field.hasError('min')) {
      return `Minimum value is ${field.errors['min'].min}`;
    }
    if (field.hasError('max')) {
      return `Maximum value is ${field.errors['max'].max}`;
    }

    return 'Invalid value';
  }
}