// assessment-details-step/assessment-details-step.component.ts
import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }  from '@angular/material/form-field';
import { MatInputModule }      from '@angular/material/input';
import { MatSelectModule }     from '@angular/material/select';
import { MatIconModule }       from '@angular/material/icon';
import { MatCardModule }       from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule }      from '@angular/material/chips';
import { FuseAlertComponent }  from '@fuse/components/alert';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AssessmentType, FormativeTypeOptions, ExamTypeOptions, AssessmentMethodOptions, RatingScaleOptions, getAssessmentTypeLabel, getAssessmentTypeColor } from '../types/AssessmentDtos';


@Component({
  selector: 'app-assessment-details-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatCardModule, MatSlideToggleModule, MatChipsModule,
    FuseAlertComponent,
  ],
  templateUrl: './assessment-details-step.component.html',
})
export class AssessmentDetailsStepComponent implements OnInit, OnChanges {

  @Input() formData: any          = {};
  @Input() assessmentType!:       AssessmentType;
  @Input() isEditMode             = false;
  @Output() formChanged           = new EventEmitter<any>();
  @Output() formValid             = new EventEmitter<boolean>();

  private fb      = inject(FormBuilder);
  private service = inject(AssessmentService);

  form!:      FormGroup;
  form2!:     FormGroup;   // competency second group
  outcomes:   any[] = [];

  readonly AssessmentType = AssessmentType;

  formativeTypes    = FormativeTypeOptions;
  examTypes         = ExamTypeOptions;
  assessmentMethods = AssessmentMethodOptions;
  ratingScales      = RatingScaleOptions;

  getTypeName  = getAssessmentTypeLabel;
  getTypeColor = getAssessmentTypeColor;

  get typeLabel(): string { return getAssessmentTypeLabel(this.assessmentType); }

  ngOnInit(): void {
    this._buildForm();
    this.service.getLearningOutcomes().subscribe(r => this.outcomes = r);
    this.form.valueChanges.subscribe(() => this._emit());
    this.formValid.emit(true); // details step is entirely optional
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) {
      this.form.patchValue(this.formData ?? {}, { emitEvent: false });
    }
    if (c['assessmentType'] && this.form) {
      this._emit();
    }
  }

  private _buildForm(): void {
    const d = this.formData ?? {};

    // Single form — only relevant fields are used by the template per type
    this.form = this.fb.group({
      // ── Formative ──────────────────────────────────────────────────────
      formativeType:        [d.formativeType        ?? null],
      competencyArea:       [d.competencyArea        ?? ''],
      learningOutcomeId:    [d.learningOutcomeId     ?? null],
      formativeStrand:      [d.formativeStrand        ?? ''],
      formativeSubStrand:   [d.formativeSubStrand     ?? ''],
      criteria:             [d.criteria               ?? ''],
      feedbackTemplate:     [d.feedbackTemplate       ?? ''],
      requiresRubric:       [d.requiresRubric         ?? false],
      assessmentWeight:     [d.assessmentWeight       ?? 100],
      formativeInstructions:[d.formativeInstructions  ?? ''],

      // ── Summative ──────────────────────────────────────────────────────
      examType:               [d.examType               ?? null],
      duration:               [d.duration               ?? ''],
      numberOfQuestions:      [d.numberOfQuestions      ?? null],
      passMark:               [d.passMark               ?? 50],
      hasPracticalComponent:  [d.hasPracticalComponent  ?? false],
      practicalWeight:        [d.practicalWeight        ?? 0],
      theoryWeight:           [d.theoryWeight           ?? 100],
      summativeInstructions:  [d.summativeInstructions  ?? ''],

      // ── Competency ─────────────────────────────────────────────────────
      competencyName:          [d.competencyName          ?? ''],
      competencyStrand:        [d.competencyStrand        ?? ''],
      competencySubStrand:     [d.competencySubStrand     ?? ''],
      performanceIndicators:   [d.performanceIndicators   ?? ''],
      assessmentMethod:        [d.assessmentMethod        ?? null],
      ratingScale:             [d.ratingScale             ?? null],
      isObservationBased:      [d.isObservationBased      ?? true],
      toolsRequired:           [d.toolsRequired           ?? ''],
      competencyInstructions:  [d.competencyInstructions  ?? ''],
      specificLearningOutcome: [d.specificLearningOutcome ?? ''],
    });
  }

  private _emit(): void {
    const raw = this.form.value;
    const payload: any = { assessmentType: this.assessmentType };

    if (this.assessmentType === AssessmentType.Formative) {
      payload.formativeType         = raw.formativeType;
      payload.competencyArea        = raw.competencyArea;
      payload.learningOutcomeId     = raw.learningOutcomeId;
      payload.formativeStrand       = raw.formativeStrand;
      payload.formativeSubStrand    = raw.formativeSubStrand;
      payload.criteria              = raw.criteria;
      payload.feedbackTemplate      = raw.feedbackTemplate;
      payload.requiresRubric        = raw.requiresRubric;
      payload.assessmentWeight      = Number(raw.assessmentWeight);
      payload.formativeInstructions = raw.formativeInstructions;
    }

    if (this.assessmentType === AssessmentType.Summative) {
      payload.examType              = raw.examType;
      payload.duration              = raw.duration;
      payload.numberOfQuestions     = raw.numberOfQuestions ? Number(raw.numberOfQuestions) : null;
      payload.passMark              = Number(raw.passMark);
      payload.hasPracticalComponent = raw.hasPracticalComponent;
      payload.practicalWeight       = Number(raw.practicalWeight);
      payload.theoryWeight          = Number(raw.theoryWeight);
      payload.summativeInstructions = raw.summativeInstructions;
    }

    if (this.assessmentType === AssessmentType.Competency) {
      payload.competencyName          = raw.competencyName;
      payload.competencyStrand        = raw.competencyStrand;
      payload.competencySubStrand     = raw.competencySubStrand;
      payload.performanceIndicators   = raw.performanceIndicators;
      payload.assessmentMethod        = raw.assessmentMethod;
      payload.ratingScale             = raw.ratingScale;
      payload.isObservationBased      = raw.isObservationBased;
      payload.toolsRequired           = raw.toolsRequired;
      payload.competencyInstructions  = raw.competencyInstructions;
      payload.specificLearningOutcome = raw.specificLearningOutcome;
    }

    this.formChanged.emit(payload);
    this.formValid.emit(true);
  }

  get hasPractical(): boolean {
    return this.form.get('hasPracticalComponent')?.value === true;
  }

  // Sync theory/practical weights so they sum to 100
  onPracticalWeightChange(val: number): void {
    const practical = Math.min(100, Math.max(0, Number(val)));
    this.form.patchValue({ practicalWeight: practical, theoryWeight: 100 - practical }, { emitEvent: false });
    this._emit();
  }
}