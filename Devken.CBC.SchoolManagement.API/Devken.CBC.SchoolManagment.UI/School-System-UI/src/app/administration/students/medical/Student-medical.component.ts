// steps/student-medical/student-medical.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-medical',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './student-medical.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss'],
})
export class StudentMedicalComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form!: FormGroup;
  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      bloodGroup:             [this.formData?.bloodGroup             ?? ''],
      medicalConditions:      [this.formData?.medicalConditions      ?? ''],
      allergies:              [this.formData?.allergies              ?? ''],
      specialNeeds:           [this.formData?.specialNeeds           ?? ''],
      requiresSpecialSupport: [this.formData?.requiresSpecialSupport ?? false],
    });
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(true); // all optional
    });
    this.formValid.emit(true);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) this.form.patchValue(this.formData, { emitEvent: false });
  }
}