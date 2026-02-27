// grade-score-step/grade-score-step.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule }         from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }   from '@angular/material/form-field';
import { MatInputModule }       from '@angular/material/input';
import { MatSelectModule }      from '@angular/material/select';
import { MatIconModule }        from '@angular/material/icon';
import { MatCardModule }        from '@angular/material/card';
import { FuseAlertComponent }   from '@fuse/components/alert';
import { GradeLetterOptions, GradeTypeOptions } from '../types/GradeEnums';

@Component({
  selector: 'app-grade-score-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatCardModule, FuseAlertComponent,
  ],
  templateUrl: './grade-score-step.component.html',
})
export class GradeScoreStepComponent implements OnInit, OnChanges {
  @Input() formData:    any = {};
  @Input() isEditMode   = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  gradeLetterOptions = GradeLetterOptions;
  gradeTypeOptions   = GradeTypeOptions;

  // Descriptions per grade type
  readonly typeDescriptions: Record<string, string> = {
    '0': 'Formative assessments track ongoing learning progress.',
    '1': 'Summative assessments evaluate learning at the end of a period.',
    '2': 'Competency assessments measure mastery of specific skills.',
  };

  get selectedTypeDescription(): string {
    const t = this.form?.get('gradeType')?.value;
    return t !== null && t !== undefined ? (this.typeDescriptions[String(t)] ?? '') : '';
  }

  get computedPercentage(): number | null {
    const score    = Number(this.form?.get('score')?.value);
    const maxScore = Number(this.form?.get('maximumScore')?.value);
    if (!score || !maxScore || maxScore === 0) return null;
    return Math.round((score / maxScore) * 10000) / 100;
  }

  ngOnInit(): void {
    this._buildForm();
    this._setupListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        score:        this.formData?.score        ?? null,
        maximumScore: this.formData?.maximumScore ?? null,
        gradeLetter:  this.formData?.gradeLetter  ?? null,
        gradeType:    this.formData?.gradeType    ?? null,
      }, { emitEvent: false });
    }
  }

  private _buildForm(): void {
    this.form = this.fb.group({
      score:        [this.formData?.score        ?? null, [Validators.min(0)]],
      maximumScore: [this.formData?.maximumScore ?? null, [Validators.min(0)]],
      gradeLetter:  [this.formData?.gradeLetter  ?? null],
      gradeType:    [this.formData?.gradeType    ?? null],
    });
  }

  private _setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit({
        score:        value.score        !== null ? Number(value.score)       : null,
        maximumScore: value.maximumScore !== null ? Number(value.maximumScore): null,
        gradeLetter:  value.gradeLetter  !== null ? Number(value.gradeLetter)  : null,
        gradeType:    value.gradeType    !== null ? Number(value.gradeType)    : null,
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
    if (c.errors['min']) return `${field === 'score' ? 'Score' : 'Maximum score'} must be 0 or more`;
    return 'Invalid value';
  }
}