// steps/student-guardians/student-guardians.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-guardians',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './student-guardians.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss', './student-guardians.component.scss'],
})
export class StudentGuardiansComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form!: FormGroup;
  showSecondary = false;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      primaryGuardianName:             [this.formData?.primaryGuardianName             ?? '', Validators.required],
      primaryGuardianRelationship:     [this.formData?.primaryGuardianRelationship     ?? '', Validators.required],
      primaryGuardianPhone:            [this.formData?.primaryGuardianPhone            ?? '', Validators.required],
      primaryGuardianEmail:            [this.formData?.primaryGuardianEmail            ?? ''],
      primaryGuardianOccupation:       [this.formData?.primaryGuardianOccupation       ?? ''],
      primaryGuardianAddress:          [this.formData?.primaryGuardianAddress          ?? ''],
      secondaryGuardianName:           [this.formData?.secondaryGuardianName           ?? ''],
      secondaryGuardianRelationship:   [this.formData?.secondaryGuardianRelationship   ?? ''],
      secondaryGuardianPhone:          [this.formData?.secondaryGuardianPhone          ?? ''],
      secondaryGuardianEmail:          [this.formData?.secondaryGuardianEmail          ?? ''],
      secondaryGuardianOccupation:     [this.formData?.secondaryGuardianOccupation     ?? ''],
      emergencyContactName:            [this.formData?.emergencyContactName            ?? ''],
      emergencyContactPhone:           [this.formData?.emergencyContactPhone           ?? ''],
      emergencyContactRelationship:    [this.formData?.emergencyContactRelationship    ?? ''],
    });

    // Prefill secondary section visibility
    if (this.formData?.secondaryGuardianName) this.showSecondary = true;

    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(this.form.valid);
    });
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
      if (this.formData?.secondaryGuardianName) this.showSecondary = true;
    }
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }
}