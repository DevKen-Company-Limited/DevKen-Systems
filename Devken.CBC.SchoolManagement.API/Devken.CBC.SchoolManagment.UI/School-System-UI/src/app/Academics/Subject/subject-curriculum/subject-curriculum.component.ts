// subject-curriculum/subject-curriculum.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { CBCLevelOptions, SubjectTypeOptions } from '../Types/SubjectEnums';


@Component({
  selector: 'app-subject-curriculum',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './subject-curriculum.component.html',
})
export class SubjectCurriculumComponent implements OnInit, OnChanges {

  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  cbcLevels    = CBCLevelOptions;
  subjectTypes = SubjectTypeOptions;

  // Descriptions for each subject type
  readonly typeDescriptions: Record<string, string> = {
    '1': 'Core subjects are mandatory for all students at this level.',
    '2': 'Optional subjects can be selected by students based on interest.',
    '3': 'Elective subjects provide specialisation opportunities.',
    '4': 'Co-curricular subjects support holistic development outside the classroom.',
  };

  get selectedTypeDescription(): string {
    const t = this.form?.get('subjectType')?.value;
    return t ? (this.typeDescriptions[String(t)] ?? '') : '';
  }

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        subjectType: this.formData?.subjectType ?? null,
        cbcLevel:    this.formData?.cbcLevel    ?? null,
      }, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      subjectType: [this.formData?.subjectType ?? null, Validators.required],
      cbcLevel:    [this.formData?.cbcLevel    ?? null, Validators.required],
    });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit({
        subjectType: value.subjectType !== null ? Number(value.subjectType) : null,
        cbcLevel:    value.cbcLevel    !== null ? Number(value.cbcLevel)    : null,
      });
      this.formValid.emit(this.form.valid);
    });
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return `${field === 'subjectType' ? 'Subject type' : 'CBC Level'} is required`;
    return 'Invalid value';
  }
}