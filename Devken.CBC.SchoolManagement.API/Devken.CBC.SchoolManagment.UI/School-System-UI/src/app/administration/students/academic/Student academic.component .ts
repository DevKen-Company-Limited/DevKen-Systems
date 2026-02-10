// steps/student-academic/student-academic.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-academic',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './Student academic.component .html',
  styleUrls: ['../../../shared/scss/shared-step.scss', './Student academic.component.scss'],
})
export class StudentAcademicComponent implements OnInit, OnChanges {
  @Input() formData: any     = {};
  @Input() schools: any[]    = [];
  @Input() classes: any[]    = [];
  @Input() academicYears: any[] = [];
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form!: FormGroup;
  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      currentLevel:          [this.formData?.currentLevel          ?? '', Validators.required],
      currentClassId:        [this.formData?.currentClassId        ?? ''],
      currentAcademicYearId: [this.formData?.currentAcademicYearId ?? ''],
      status:                [this.formData?.status                ?? 'Active'],
      previousSchool:        [this.formData?.previousSchool        ?? ''],
    });
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(this.form.valid);
    });
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) this.form.patchValue(this.formData, { emitEvent: false });
  }

  isInvalid(f: string): boolean {
    const c = this.form.get(f);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }
}