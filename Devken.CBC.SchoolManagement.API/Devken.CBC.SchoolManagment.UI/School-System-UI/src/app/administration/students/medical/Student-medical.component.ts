import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-student-medical',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
    MatSlideToggleModule,
    FuseAlertComponent,
  ],
  templateUrl: './student-medical.component.html',
})
export class StudentMedicalComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);

  form!: FormGroup;

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