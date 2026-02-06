import { FormBuilder, FormGroup } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialogRef } from '@angular/material/dialog';
import { take } from 'rxjs/operators';
import { ICrudService } from '../Services/ICrudService';
import { ApiResponse } from 'app/Tenant/types/school';


/**
 * BaseFormDialog
 *
 * Usage:
 * - Derived component must call super(...) in constructor
 * - Implement buildForm(): FormGroup
 * - Call this.init() in ngOnInit()
 * - Optionally override patchForEdit(item)
 */
export abstract class BaseFormDialog<TCreate, TUpdate, TDto, TDialogData = any> {
  form!: FormGroup;
  isSaving = false;

  constructor(
    protected fb: FormBuilder,
    protected service: ICrudService<TCreate, TUpdate, TDto>,
    protected snackBar: MatSnackBar,
    protected dialogRef: MatDialogRef<any>,
    public data: TDialogData
  ) {}

  protected abstract buildForm(): FormGroup;

  /** Optional hook to patch an incoming item for edit */
  protected patchForEdit(item: any) {
    if (item) {
      // default: patch all matching controls
      this.form.patchValue(item);
    }
  }

  /** Call this from derived component ngOnInit */
  protected init() {
    this.form = this.buildForm();
    // If edit mode and item present, patch form
    if ((this.data as any)?.mode === 'edit' && (this.data as any)?.school) {
      this.patchForEdit((this.data as any).school);
    }
  }

  /**
   * Save handler
   * - createMapper maps raw form value -> TCreate
   * - updateMapper maps raw form value -> TUpdate
   * - getId extracts id (string) for update calls
   */
  protected save(
    createMapper: (raw: any) => TCreate,
    updateMapper: (raw: any) => TUpdate,
    getId: () => string
  ) {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.value;
    this.isSaving = true;

    if ((this.data as any)?.mode === 'create') {
      const payload = createMapper(raw);
      this.service.create(payload)
        .pipe(take(1))
        .subscribe({
          next: (res: ApiResponse<any>) => {
            this.isSaving = false;
            if (res.success) {
              this.dialogRef.close({ success: true, message: res.message, data: res.data });
            } else {
              this.snackBar.open(res.message || 'Failed to create', 'Close', { duration: 3000 });
            }
          },
          error: (err) => {
            this.isSaving = false;
            this.snackBar.open(err?.error?.message || err.message || 'Error', 'Close', { duration: 4000 });
          }
        });
    } else {
      const id = getId();
      const payload = updateMapper(raw);
      this.service.update(id, payload)
        .pipe(take(1))
        .subscribe({
          next: (res: ApiResponse<any>) => {
            this.isSaving = false;
            if (res.success) {
              this.dialogRef.close({ success: true, message: res.message, data: res.data });
            } else {
              this.snackBar.open(res.message || 'Failed to update', 'Close', { duration: 3000 });
            }
          },
          error: (err) => {
            this.isSaving = false;
            this.snackBar.open(err?.error?.message || err.message || 'Error', 'Close', { duration: 4000 });
          }
        });
    }
  }

  protected close(result: any = { success: false }) {
    this.dialogRef.close(result);
  }
}