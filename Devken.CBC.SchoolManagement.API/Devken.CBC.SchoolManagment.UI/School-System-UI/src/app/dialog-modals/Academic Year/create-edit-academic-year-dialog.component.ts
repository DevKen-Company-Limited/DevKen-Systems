import { Component, OnInit, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { SchoolDto } from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { UserService } from 'app/core/user/user.service';
import { AcademicYearDto, CreateAcademicYearRequest, UpdateAcademicYearRequest } from 'app/AcademicYear/Types/AcademicYear';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-create-edit-academic-year-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSelectModule,
    MatSnackBarModule,
    MatIconModule
  ],
  templateUrl: './create-edit-academic-year-dialog.component.html',
})
export class CreateEditAcademicYearDialogComponent implements OnInit {
  form!: FormGroup;
  isSuperAdmin = false;
  schools: SchoolDto[] = [];
  isEditMode = false;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<CreateEditAcademicYearDialogComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: { mode: 'create' | 'edit'; academicYear?: AcademicYearDto },
    private _schoolService: SchoolService,
    private _userService: UserService
  ) {
    this.isEditMode = data.mode === 'edit';
    this.checkUserRole();
    
    // Add custom class to dialog
    this.dialogRef.addPanelClass('academic-year-dialog');
  }

  ngOnInit(): void {
    this.buildForm();
    
    if (this.isSuperAdmin) {
      this.loadSchools();
    }

    if (this.isEditMode && this.data.academicYear) {
      this.patchForm(this.data.academicYear);
      // Disable schoolId in edit mode
      this.form.get('schoolId')?.disable();
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      schoolId: ['', this.isSuperAdmin ? [Validators.required] : []],
      name: ['', [Validators.required, Validators.maxLength(50)]],
      code: ['', [Validators.required, Validators.maxLength(9)]],
      startDate: [null, Validators.required],
      endDate: [null, Validators.required],
      isCurrent: [false],
      notes: ['', Validators.maxLength(1000)]
    }, { 
      validators: this.dateRangeValidator 
    });
  }

  private patchForm(academicYear: AcademicYearDto): void {
    this.form.patchValue({
      schoolId: academicYear.schoolId,
      name: academicYear.name,
      code: academicYear.code,
      startDate: new Date(academicYear.startDate),
      endDate: new Date(academicYear.endDate),
      isCurrent: academicYear.isCurrent,
      notes: academicYear.notes || ''
    });
  }

  private checkUserRole(): void {
    this._userService.user$.pipe(take(1)).subscribe((user: any) => {
      if (!user) {
        this.isSuperAdmin = false;
        return;
      }
      
      this.isSuperAdmin =
        user.roles?.some(
            (role: any) => role.roleName === 'SuperAdmin' || role === 'SuperAdmin'
        ) ||
        user.role === 'SuperAdmin' ||
        false;
    });
  }

  private loadSchools(): void {
    this._schoolService.getAll().subscribe({
      next: (response) => {
        if (response.success) {
          this.schools = response.data;
        }
      },
      error: (err) => console.error('Failed to load schools', err)
    });
  }

  private dateRangeValidator(control: AbstractControl): ValidationErrors | null {
    const start = control.get('startDate')?.value;
    const end = control.get('endDate')?.value;
    
    if (start && end && start >= end) {
      return { dateRange: true };
    }
    return null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Please fix validation errors', 'Close', { duration: 3000 });
      return;
    }

    const formValue = this.form.getRawValue(); // Use getRawValue to include disabled fields

    if (this.isEditMode) {
      const payload = this.mapToUpdateRequest(formValue);
      this.dialogRef.close(payload);
    } else {
      const payload = this.mapToCreateRequest(formValue);
      this.dialogRef.close(payload);
    }
  }

  private mapToCreateRequest(raw: any): CreateAcademicYearRequest {
    const startDate = raw.startDate instanceof Date 
      ? raw.startDate.toISOString() 
      : raw.startDate;
    const endDate = raw.endDate instanceof Date 
      ? raw.endDate.toISOString() 
      : raw.endDate;

    const payload: CreateAcademicYearRequest = {
      name: (raw.name ?? '').trim(),
      code: (raw.code ?? '').trim(),
      startDate: startDate ?? '',
      endDate: endDate ?? '',
      isCurrent: raw.isCurrent ?? false,
      notes: raw.notes?.trim() || null
    } as any;

    // Only include schoolId if user is SuperAdmin
    if (this.isSuperAdmin && raw.schoolId) {
      payload.schoolId = raw.schoolId.trim();
    }

    return payload;
  }

  private mapToUpdateRequest(raw: any): UpdateAcademicYearRequest {
    const startDate = raw.startDate instanceof Date 
      ? raw.startDate.toISOString() 
      : raw.startDate;
    const endDate = raw.endDate instanceof Date 
      ? raw.endDate.toISOString() 
      : raw.endDate;

    return {
      name: raw.name ? raw.name.trim() : null,
      code: raw.code ? raw.code.trim() : null,
      startDate: startDate ?? null,
      endDate: endDate ?? null,
      isCurrent: raw.isCurrent ?? null,
      notes: raw.notes?.trim() ?? null
    };
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}