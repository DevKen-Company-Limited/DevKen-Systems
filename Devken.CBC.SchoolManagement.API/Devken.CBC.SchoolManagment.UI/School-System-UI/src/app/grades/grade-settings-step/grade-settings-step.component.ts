// grade-settings-step/grade-settings-step.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule }          from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSlideToggleModule }  from '@angular/material/slide-toggle';
import { MatFormFieldModule }    from '@angular/material/form-field';
import { MatInputModule }        from '@angular/material/input';
import { MatDatepickerModule }   from '@angular/material/datepicker';
import { MatNativeDateModule }   from '@angular/material/core';
import { MatIconModule }         from '@angular/material/icon';
import { MatCardModule }         from '@angular/material/card';
import { FuseAlertComponent }    from '@fuse/components/alert';

@Component({
  selector: 'app-grade-settings-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatSlideToggleModule, MatFormFieldModule, MatInputModule,
    MatDatepickerModule, MatNativeDateModule,
    MatIconModule, MatCardModule, FuseAlertComponent,
  ],
  templateUrl: './grade-settings-step.component.html',
})
export class GradeSettingsStepComponent implements OnInit, OnChanges {
  @Input() formData:    any = {};
  @Input() isEditMode   = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
      assessmentDate: [
        this.formData?.assessmentDate ? new Date(this.formData.assessmentDate) : new Date(),
        Validators.required,
      ],
      remarks:     [this.formData?.remarks     ?? '', Validators.maxLength(500)],
      isFinalized: [this.formData?.isFinalized ?? false],
    });

    this.form.valueChanges.subscribe(value => {
      const dateVal = value.assessmentDate instanceof Date
        ? value.assessmentDate.toISOString()
        : value.assessmentDate;
      this.formChanged.emit({ ...value, assessmentDate: dateVal });
      this.formValid.emit(this.form.valid);
    });

    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        assessmentDate: this.formData?.assessmentDate
          ? new Date(this.formData.assessmentDate) : new Date(),
        remarks:     this.formData?.remarks     ?? '',
        isFinalized: this.formData?.isFinalized ?? false,
      }, { emitEvent: false });
    }
  }

  get isFinalized(): boolean { return !!this.form?.get('isFinalized')?.value; }
}