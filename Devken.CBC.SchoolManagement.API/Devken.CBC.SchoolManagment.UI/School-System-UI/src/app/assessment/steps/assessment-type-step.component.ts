// assessment-type-step/assessment-type-step.component.ts
import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule }  from '@angular/material/icon';
import { MatCardModule }  from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { AssessmentTypeOptions, AssessmentType } from '../types/AssessmentDtos';


@Component({
  selector: 'app-assessment-type-step',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatCardModule, FuseAlertComponent],
  templateUrl: './assessment-type-step.component.html',
})
export class AssessmentTypeStepComponent implements OnInit, OnChanges {

  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  typeOptions = AssessmentTypeOptions;

  readonly typeFeatures: Record<number, string[]> = {
    [AssessmentType.Formative]:  ['Ongoing monitoring', 'Learning outcome mapping', 'Rubric support', 'Feedback templates'],
    [AssessmentType.Summative]:  ['Theory + practical splits', 'Pass mark tracking', 'Class ranking', 'Exam duration'],
    [AssessmentType.Competency]: ['Observation-based', 'CBC strand alignment', 'Rating scales', 'Evidence collection'],
  };

  get selectedType(): AssessmentType | null {
    return this.form?.get('assessmentType')?.value ?? null;
  }

  get selectedOption() {
    return this.typeOptions.find(o => o.value === this.selectedType) ?? null;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      assessmentType: [this.formData?.assessmentType ?? null, Validators.required],
    });
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit({ assessmentType: v.assessmentType });
      this.formValid.emit(this.form.valid);
    });
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) {
      this.form.patchValue({ assessmentType: this.formData?.assessmentType ?? null }, { emitEvent: false });
    }
  }

  selectType(type: AssessmentType): void {
    if (this.isEditMode) return;       // lock type in edit mode
    this.form.get('assessmentType')!.setValue(type);
    this.form.get('assessmentType')!.markAsDirty();
  }

  isInvalid(): boolean {
    const c = this.form.get('assessmentType');
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getColorClasses(color: string, selected: boolean): string {
    const map: Record<string, { ring: string; bg: string; text: string; icon: string }> = {
      indigo: { ring: 'ring-indigo-500 border-indigo-500', bg: 'bg-indigo-50 dark:bg-indigo-900/20', text: 'text-indigo-700 dark:text-indigo-300', icon: 'text-indigo-600' },
      violet: { ring: 'ring-violet-500 border-violet-500', bg: 'bg-violet-50 dark:bg-violet-900/20', text: 'text-violet-700 dark:text-violet-300', icon: 'text-violet-600' },
      teal:   { ring: 'ring-teal-500 border-teal-500',     bg: 'bg-teal-50 dark:bg-teal-900/20',     text: 'text-teal-700 dark:text-teal-300',     icon: 'text-teal-600'   },
    };
    const c = map[color] ?? map['indigo'];
    return selected ? `${c.ring} ${c.bg} ring-2 shadow-md` : 'border-gray-200 dark:border-gray-700 hover:border-gray-300';
  }

  getIconClass(color: string, selected: boolean): string {
    const map: Record<string, string> = { indigo: 'text-indigo-600', violet: 'text-violet-600', teal: 'text-teal-600' };
    return selected ? (map[color] ?? 'text-indigo-600') : 'text-gray-400';
  }
}