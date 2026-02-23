// assessment-identity-step/assessment-identity-step.component.ts
import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }   from '@angular/material/form-field';
import { MatInputModule }       from '@angular/material/input';
import { MatSelectModule }      from '@angular/material/select';
import { MatDatepickerModule }  from '@angular/material/datepicker';
import { MatIconModule }        from '@angular/material/icon';
import { MatCardModule }        from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AssessmentService }    from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import {
  AssessmentType,
  getAssessmentTypeLabel,
  getAssessmentTypeColor,
} from '../types/AssessmentDtos';

@Component({
  selector: 'app-assessment-identity-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatDatepickerModule, MatIconModule, MatCardModule,
    MatSlideToggleModule, MatProgressSpinnerModule,
  ],
  templateUrl: './assessment-identity-step.component.html',
})
export class AssessmentIdentityStepComponent implements OnInit, OnChanges {

  @Input() formData:        any           = {};
  @Input() assessmentType!: AssessmentType;
  @Input() isEditMode       = false;
  @Input() isSuperAdmin     = false;
  @Output() formChanged     = new EventEmitter<any>();
  @Output() formValid       = new EventEmitter<boolean>();

  private fb      = inject(FormBuilder);
  private service = inject(AssessmentService);

  form!: FormGroup;

  // Lookup data
  schools:       any[] = [];
  teachers:      any[] = [];
  subjects:      any[] = [];
  classes:       any[] = [];
  terms:         any[] = [];
  academicYears: any[] = [];

  // Loading states
  loadingSchools  = false;
  loadingLookups  = false;

  getTypeName  = getAssessmentTypeLabel;
  getTypeColor = getAssessmentTypeColor;

  get typeLabel(): string { return getAssessmentTypeLabel(this.assessmentType); }

  ngOnInit(): void {
    this._buildForm();

    // If SuperAdmin → load schools first; else load lookups directly
    if (this.isSuperAdmin) {
      this._loadSchools();
      // If editing and schoolId already set, load lookups for that school
      if (this.formData?.schoolId) {
        this._loadLookups(this.formData.schoolId);
      }
    } else {
      this._loadLookups();
    }

    // React to school selection changes (SuperAdmin only)
    this.form.get('schoolId')?.valueChanges.subscribe(schoolId => {
      if (!schoolId) return;
      // Clear dependent fields when school changes
      this.form.patchValue({
        teacherId: null, subjectId: null,
        classId: null, termId: null, academicYearId: null,
      }, { emitEvent: false });
      this.teachers = [];
      this.subjects = [];
      this.classes  = [];
      this.terms    = [];
      this.academicYears = [];
      this._loadLookups(schoolId);
    });

    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(this._mapOut(v));
      this.formValid.emit(this.form.valid);
    });

    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(c: SimpleChanges): void {
    if (c['formData'] && this.form) {
      this.form.patchValue(this._mapIn(this.formData), { emitEvent: false });
    }
  }

  // ── Form Build ─────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this.fb.group({
      // SuperAdmin-only field
      schoolId:      [
        this.formData?.schoolId ?? null,
        this.isSuperAdmin ? Validators.required : [],
      ],
      title:         [this.formData?.title         ?? '', [Validators.required, Validators.maxLength(300)]],
      description:   [this.formData?.description   ?? ''],
      teacherId:     [this.formData?.teacherId      ?? null, Validators.required],
      subjectId:     [this.formData?.subjectId      ?? null, Validators.required],
      classId:       [this.formData?.classId        ?? null, Validators.required],
      termId:        [this.formData?.termId         ?? null, Validators.required],
      academicYearId:[this.formData?.academicYearId ?? null, Validators.required],
      assessmentDate:[this.formData?.assessmentDate ?? null, Validators.required],
      maximumScore:  [this.formData?.maximumScore   ?? 100,  [Validators.required, Validators.min(1)]],
      isPublished:   [this.formData?.isPublished    ?? false],
    });
  }

  // ── Data Mapping ───────────────────────────────────────────────────────
  private _mapIn(d: any) {
    return {
      schoolId:       d?.schoolId       ?? null,
      title:          d?.title          ?? '',
      description:    d?.description    ?? '',
      teacherId:      d?.teacherId      ?? null,
      subjectId:      d?.subjectId      ?? null,
      classId:        d?.classId        ?? null,
      termId:         d?.termId         ?? null,
      academicYearId: d?.academicYearId ?? null,
      assessmentDate: d?.assessmentDate ? new Date(d.assessmentDate) : null,
      maximumScore:   d?.maximumScore   ?? 100,
      isPublished:    d?.isPublished    ?? false,
    };
  }

  private _mapOut(v: any) {
    return {
      ...v,
      assessmentDate: v.assessmentDate
        ? (v.assessmentDate instanceof Date
            ? v.assessmentDate.toISOString().split('T')[0]
            : v.assessmentDate)
        : null,
      maximumScore: Number(v.maximumScore),
    };
  }

  // ── Lookups ────────────────────────────────────────────────────────────
  /** Load school list (SuperAdmin only) */
  private _loadSchools(): void {
    this.loadingSchools = true;
    this.service.getSchools().subscribe({
      next:     r => { this.schools = r; this.loadingSchools = false; },
      error:    () => { this.loadingSchools = false; },
    });
  }

  /**
   * Load teachers / subjects / classes / terms / academic years.
   * When schoolId is provided (SuperAdmin), the service filters by school.
   * Without schoolId the service uses the tenant context from the auth token.
   */
  private _loadLookups(schoolId?: string): void {
    this.loadingLookups = true;
    this.service.getTeachers(schoolId).subscribe(r      => this.teachers      = r);
    this.service.getSubjects(schoolId).subscribe(r      => this.subjects      = r);
    this.service.getClasses(schoolId).subscribe(r       => this.classes       = r);
    this.service.getTerms(schoolId).subscribe(r         => this.terms         = r);
    this.service.getAcademicYears(schoolId).subscribe({ next: r => { this.academicYears = r; this.loadingLookups = false; }, error: () => { this.loadingLookups = false; } });
  }

  // ── Helpers ────────────────────────────────────────────────────────────
  get selectedSchoolName(): string {
    const s = this.schools.find(s => s.id === this.form.get('schoolId')?.value);
    return s?.name ?? '';
  }

  get schoolSelected(): boolean {
    return !this.isSuperAdmin || !!this.form.get('schoolId')?.value;
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    const labels: Record<string, string> = {
      schoolId: 'School', title: 'Title', teacherId: 'Teacher',
      subjectId: 'Subject', classId: 'Class', termId: 'Term',
      academicYearId: 'Academic year', assessmentDate: 'Assessment date',
      maximumScore: 'Maximum score',
    };
    if (c.errors['required'])   return `${labels[field] ?? field} is required`;
    if (c.errors['maxlength'])  return `${labels[field] ?? field} is too long`;
    if (c.errors['min'])        return 'Score must be at least 1';
    return 'Invalid value';
  }
}