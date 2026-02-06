import { Component, OnInit, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CreateSchoolRequest, SchoolDto, UpdateSchoolRequest } from 'app/Tenant/types/school';
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
    MatSnackBarModule
  ],
  templateUrl: './create-edit-school-dialog.component.html',
  styles: [`.dialog-actions { display:flex; justify-content:flex-end; gap:8px }`]
})
export class CreateEditSchoolDialogComponent
  extends BaseFormDialog<CreateSchoolRequest, UpdateSchoolRequest, SchoolDto, { mode: 'create' | 'edit'; school?: SchoolDto }>
  implements OnInit
{
  constructor(
    fb: FormBuilder,
    protected service: SchoolService, // properly typed, no cast
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditSchoolDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public override data: { mode: 'create' | 'edit'; school?: SchoolDto }
  ) {
    super(fb, service, snackBar, dialogRef, data);
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      slugName: ['', [Validators.required]],
      name: ['', [Validators.required]],
      address: [''],
      phoneNumber: [''],
      email: ['', Validators.email],
      logoUrl: [''],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.init(); // BaseFormDialog.init() will patch form if edit mode
  }

  /** Called from template (submit) */
  submit() {
    this.save(
      // createMapper
      (raw) =>
        ({
          slugName: (raw.slugName ?? '').trim(),
          name: (raw.name ?? '').trim(),
          address: raw.address?.trim() ?? null,
          phoneNumber: raw.phoneNumber?.trim() ?? null,
          email: raw.email?.trim() ?? null,
          logoUrl: raw.logoUrl?.trim() ?? null,
          isActive: raw.isActive ?? true
        } as CreateSchoolRequest),

      // updateMapper
      (raw) =>
        ({
          slugName: (raw.slugName ?? '').trim(),
          name: (raw.name ?? '').trim(),
          address: raw.address?.trim() ?? null,
          phoneNumber: raw.phoneNumber?.trim() ?? null,
          email: raw.email?.trim() ?? null,
          logoUrl: raw.logoUrl?.trim() ?? null,
          isActive: raw.isActive ?? true
        } as UpdateSchoolRequest),

      // getId
      () => this.data.school?.id ?? ''
    );
  }

  /** Small override so template stays consistent with earlier naming */
  cancel() {
    this.close({ success: false });
  }
}
