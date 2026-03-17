import {
  Component, OnInit, OnDestroy, Inject, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogRef, MAT_DIALOG_DATA, MatDialogModule,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LibraryBranchService } from 'app/core/DevKenService/Library/library-branch.service';
import { LibraryBranchDto, CreateLibraryBranchRequest, UpdateLibraryBranchRequest } from 'app/Library/library-branch/Types/library-branch.types';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';

export interface CreateEditLibraryBranchDialogData {
  mode: 'create' | 'edit';
  branch?: LibraryBranchDto;
}

@Component({
  selector: 'app-create-edit-library-branch-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatSnackBarModule, MatTooltipModule,
  ],
  templateUrl: './create-edit-library-branch-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `]
})
export class CreateEditLibraryBranchDialogComponent
  extends BaseFormDialog<CreateLibraryBranchRequest, UpdateLibraryBranchRequest, LibraryBranchDto, CreateEditLibraryBranchDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  schools: SchoolDto[] = [];
  isLoading = false;
  formSubmitted = false;

  get isEditMode(): boolean  { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }
  get dialogTitle(): string  { return this.isEditMode ? 'Edit Library Branch' : 'Add New Branch'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating "${this.data.branch?.name || 'branch'}" details`
      : 'Create a new library branch for your school';
  }
  get locationLength(): number { return this.form.get('location')?.value?.length || 0; }

  constructor(
    fb: FormBuilder,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditLibraryBranchDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateEditLibraryBranchDialogData,
    private readonly _authService: AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _cdr: ChangeDetectorRef,
    branchService: LibraryBranchService,
  ) {
    super(fb, branchService, snackBar, dialogRef, data);
    dialogRef.addPanelClass(['library-branch-dialog', 'responsive-dialog']);
  }

  ngOnInit(): void  { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    return this.fb.group({
      schoolId: [null, this.isSuperAdmin ? [Validators.required] : []],
      name:     ['',   [Validators.required, Validators.maxLength(150)]],
      location: ['',   [Validators.maxLength(300)]],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();
    this._loadSchools();
  }

  protected override patchForEdit(item: LibraryBranchDto): void {
    this.form.patchValue({
      schoolId: item.schoolId || null,
      name:     item.name     || '',
      location: item.location || '',
    });
    this._cdr.detectChanges();
  }

  private _loadSchools(): void {
    if (!this.isSuperAdmin) {
      if (this.isEditMode && this.data.branch) this.patchForEdit(this.data.branch);
      return;
    }

    this.isLoading = true;
    this._schoolService.getAll()
      .pipe(
        catchError(() => of({ success: false, data: [] })),
        takeUntil(this._unsubscribe),
        finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
      )
      .subscribe((res: any) => {
        this.schools = res.data || [];
        if (this.isEditMode && this.data.branch) this.patchForEdit(this.data.branch);
      });
  }

  onSubmit(): void {
    this.formSubmitted = true;

    const createMapper = (raw: any): CreateLibraryBranchRequest => ({
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      name:     raw.name?.trim(),
      location: raw.location?.trim() || undefined,
    });

    const updateMapper = (raw: any): UpdateLibraryBranchRequest => ({
      name:     raw.name?.trim(),
      location: raw.location?.trim() || undefined,
    });

    this.save(createMapper, updateMapper, () => this.data.branch!.id);
  }

  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}