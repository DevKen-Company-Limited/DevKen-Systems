import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-student-guardians',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
  ],
  templateUrl: './student-guardians.component.html',
})
export class StudentGuardiansComponent implements OnInit, OnChanges {

  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);

  form!: FormGroup;
  showSecondary = false;

  ngOnInit(): void {

    this.form = this.fb.group({
      primaryGuardianName:           [this.formData?.primaryGuardianName ?? '', Validators.required],
      primaryGuardianRelationship:   [this.formData?.primaryGuardianRelationship ?? '', Validators.required],
      primaryGuardianPhone:          [this.formData?.primaryGuardianPhone ?? '', Validators.required],
      primaryGuardianEmail:          [this.formData?.primaryGuardianEmail ?? ''],
      primaryGuardianOccupation:     [this.formData?.primaryGuardianOccupation ?? ''],
      primaryGuardianAddress:        [this.formData?.primaryGuardianAddress ?? ''],

      secondaryGuardianName:         [this.formData?.secondaryGuardianName ?? ''],
      secondaryGuardianRelationship: [this.formData?.secondaryGuardianRelationship ?? ''],
      secondaryGuardianPhone:        [this.formData?.secondaryGuardianPhone ?? ''],
      secondaryGuardianEmail:        [this.formData?.secondaryGuardianEmail ?? null],
      secondaryGuardianOccupation:   [this.formData?.secondaryGuardianOccupation ?? ''],

      emergencyContactName:          [this.formData?.emergencyContactName ?? ''],
      emergencyContactPhone:         [this.formData?.emergencyContactPhone ?? ''],
      emergencyContactRelationship:  [this.formData?.emergencyContactRelationship ?? ''],
    });

    // Prefill secondary section visibility
    this.showSecondary = !!this.formData?.secondaryGuardianEmail;

    // Listen to email changes to auto-toggle section
    this.form.get('secondaryGuardianEmail')?.valueChanges.subscribe(value => {
      this.showSecondary = !!value?.trim();
    });

    // Clean & Emit form values
    this.form.valueChanges.subscribe(v => {

      const cleaned = { ...v };

      // Trim secondary email safely
      const secondaryEmail = cleaned.secondaryGuardianEmail?.trim();

      // If secondary email is empty → remove all secondary fields
      if (!secondaryEmail) {
        delete cleaned.secondaryGuardianName;
        delete cleaned.secondaryGuardianRelationship;
        delete cleaned.secondaryGuardianPhone;
        delete cleaned.secondaryGuardianEmail;
        delete cleaned.secondaryGuardianOccupation;
      } else {
        cleaned.secondaryGuardianEmail = secondaryEmail;
      }

      this.formChanged.emit(cleaned);
      this.formValid.emit(this.form.valid);
    });

    // Initial validity emit
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
      this.showSecondary = !!this.formData?.secondaryGuardianEmail;
    }
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
}
