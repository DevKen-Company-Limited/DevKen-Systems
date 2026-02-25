import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, OnDestroy, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule }                                            from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }  from '@angular/material/form-field';
import { MatInputModule }      from '@angular/material/input';
import { MatSelectModule }     from '@angular/material/select';
import { MatIconModule }       from '@angular/material/icon';
import { MatCardModule }       from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent }  from '@fuse/components/alert';
import { Subject }             from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';
import { of, forkJoin }        from 'rxjs';

import { SchoolDto }      from 'app/Tenant/types/school';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { TermService }    from 'app/core/DevKenService/TermService/term.service';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AssessmentType } from 'app/assessment/types/AssessmentDtos';


/** Lightweight option for a dropdown item */
export interface DropdownOption {
  id:       string;
  label:    string;
  subLabel?: string;
}

@Component({
  selector: 'app-grade-subject-step',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatCardModule, MatProgressSpinnerModule,
    FuseAlertComponent,
  ],
  templateUrl: './grade-subject-step.component.html',
})
export class GradeSubjectStepComponent implements OnInit, OnChanges, OnDestroy {

  @Input() formData:    any        = {};
  @Input() schools:     SchoolDto[] = [];
  @Input() isEditMode   = false;
  @Input() isSuperAdmin = false;
  @Output() formChanged       = new EventEmitter<any>();
  @Output() formValid         = new EventEmitter<boolean>();
  @Output() displayNamesChanged = new EventEmitter<{
    studentName?: string;
    studentAdmNo?: string;
    subjectName?: string;
    subjectCode?: string;
    termName?: string;
    assessmentName?: string;
    assessmentType?: string;
  }>();

  // ─── Injected services ──────────────────────────────────────────────────
  private fb               = inject(FormBuilder);
  private _studentSvc      = inject(StudentService);
  private _subjectSvc      = inject(SubjectService);
  private _termSvc         = inject(TermService);
  private _assessmentSvc   = inject(AssessmentService);
  private _destroy$        = new Subject<void>();

  // ─── Dropdown data ──────────────────────────────────────────────────────
  students:    DropdownOption[] = [];
  subjects:    DropdownOption[] = [];
  terms:       DropdownOption[] = [];
  assessments: DropdownOption[] = [];

  isLoadingStudents    = false;
  isLoadingSubjects    = false;
  isLoadingTerms       = false;
  isLoadingAssessments = false;

  form!: FormGroup;

  // ─── Lifecycle ──────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._buildForm();
    this._setupListeners();
    this._loadDropdownData();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['schools'] && !changes['schools'].firstChange && this.form) {
      const tenantId = this.form.get('tenantId')?.value;
      if (tenantId) this._reloadForSchool(tenantId);
    }
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
    }
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ─── Form ───────────────────────────────────────────────────────────────
  private _buildForm(): void {
    const cfg: any = {
      studentId:    [this.formData?.studentId    ?? null, Validators.required],
      subjectId:    [this.formData?.subjectId    ?? null, Validators.required],
      termId:       [this.formData?.termId       ?? null],
      assessmentId: [this.formData?.assessmentId ?? null],
    };

    if (this.isSuperAdmin) {
      cfg['tenantId'] = [this.formData?.tenantId ?? null, Validators.required];
    }

    this.form = this.fb.group(cfg);

    if (this.isEditMode) {
      this.form.get('studentId')?.disable();
      this.form.get('subjectId')?.disable();
    }

    // When SuperAdmin changes school → reload everything for that school
    if (this.isSuperAdmin) {
      this.form.get('tenantId')?.valueChanges
        .pipe(takeUntil(this._destroy$))
        .subscribe(schoolId => {
          if (schoolId) this._reloadForSchool(schoolId);
        });
    }
  }

  private _setupListeners(): void {
    this.form.valueChanges
      .pipe(takeUntil(this._destroy$))
      .subscribe(() => {
        this.formChanged.emit(this.form.getRawValue());
        this.formValid.emit(this.form.valid);
        this._emitDisplayNames();
      });
  }

  private _emitDisplayNames(): void {
    const raw        = this.form.getRawValue();
    const student    = this.students.find(s  => s.id === raw.studentId);
    const subject    = this.subjects.find(s  => s.id === raw.subjectId);
    const term       = this.terms.find(t    => t.id === raw.termId);
    const assessment = this.assessments.find(a => a.id === raw.assessmentId);

    this.displayNamesChanged.emit({
      studentName:    student?.label,
      studentAdmNo:   student?.subLabel,
      subjectName:    subject?.label,
      subjectCode:    subject?.subLabel,
      termName:       term?.label,
      assessmentName: assessment?.label,
      assessmentType: assessment?.subLabel,
    });
  }

  // ─── Data loading ────────────────────────────────────────────────────────
  private _loadDropdownData(schoolId?: string): void {
    this._loadStudents(schoolId);
    this._loadSubjects(schoolId);
    this._loadTerms(schoolId);
    this._loadAssessments();
  }

  private _reloadForSchool(schoolId: string): void {
    this.form.patchValue(
      { studentId: null, subjectId: null, termId: null, assessmentId: null },
      { emitEvent: false }
    );
    this._loadDropdownData(schoolId);
  }

  private _loadStudents(schoolId?: string): void {
    this.isLoadingStudents = true;
    this._studentSvc.getAll(schoolId)
      .pipe(
        takeUntil(this._destroy$),
        catchError(() => of([])),
        finalize(() => { this.isLoadingStudents = false; }),
      )
      .subscribe(res => {
        const list = Array.isArray(res) ? res : (res as any)?.data ?? [];
        this.students = list.map((s: any) => ({
          id:       s.id,
          label:    s.fullName ?? `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
          subLabel: s.admissionNumber ?? s.nemisNumber ?? '',
        }));
      });
  }

  private _loadSubjects(schoolId?: string): void {
    this.isLoadingSubjects = true;
    this._subjectSvc.getAll(schoolId)
      .pipe(
        takeUntil(this._destroy$),
        catchError(() => of([])),
        finalize(() => { this.isLoadingSubjects = false; }),
      )
      .subscribe(res => {
        const list = Array.isArray(res) ? res : (res as any)?.data ?? [];
        this.subjects = list.map((s: any) => ({
          id:       s.id,
          label:    s.name,
          subLabel: s.code ?? '',
        }));
      });
  }

  private _loadTerms(schoolId?: string): void {
    this.isLoadingTerms = true;
    this._termSvc.getAll(schoolId)
      .pipe(
        takeUntil(this._destroy$),
        catchError(() => of({ success: true, data: [] })),
        finalize(() => { this.isLoadingTerms = false; }),
      )
      .subscribe(res => {
        const list: any[] = (res as any)?.data ?? (Array.isArray(res) ? res : []);
        this.terms = list.map((t: any) => ({
          id:       t.id,
          label:    t.name,
          subLabel: t.academicYearName ?? '',
        }));
      });
  }

  /** Loads all assessments (Formative + Summative + Competency) and merges them,
   *  exactly as AssessmentsComponent.loadAll() does. */
  private _loadAssessments(): void {
    this.isLoadingAssessments = true;

    forkJoin({
      formative:  this._assessmentSvc.getAll(AssessmentType.Formative).pipe(catchError(() => of([]))),
      summative:  this._assessmentSvc.getAll(AssessmentType.Summative).pipe(catchError(() => of([]))),
      competency: this._assessmentSvc.getAll(AssessmentType.Competency).pipe(catchError(() => of([]))),
    })
    .pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isLoadingAssessments = false; }),
    )
    .subscribe({
      next: ({ formative, summative, competency }) => {
        const all: any[] = [...(formative as any[]), ...(summative as any[]), ...(competency as any[])];
        this.assessments = all.map(a => ({
          id:       a.id,
          label:    a.title,
          subLabel: a.assessmentType !== undefined
            ? this._assessmentTypeLabel(a.assessmentType)
            : '',
        }));
      },
      error: () => { this.assessments = []; },
    });
  }

  private _assessmentTypeLabel(type: AssessmentType | number): string {
    switch (Number(type)) {
      case AssessmentType.Formative:  return 'Formative';
      case AssessmentType.Summative:  return 'Summative';
      case AssessmentType.Competency: return 'Competency';
      default: return '';
    }
  }

  // ─── Template helpers ────────────────────────────────────────────────────
  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return `${this._label(field)} is required`;
    return 'Invalid value';
  }

  private _label(field: string): string {
    const map: Record<string, string> = {
      studentId: 'Student', subjectId: 'Subject', tenantId: 'School', termId: 'Term',
    };
    return map[field] ?? field;
  }

  selectedLabel(options: DropdownOption[], id: string | null): string {
    if (!id) return '—';
    return options.find(o => o.id === id)?.label ?? id;
  }

  selectedSubLabel(options: DropdownOption[], id: string | null): string {
    if (!id) return '';
    return options.find(o => o.id === id)?.subLabel ?? '';
  }

  get isAnyLoading(): boolean {
    return this.isLoadingStudents || this.isLoadingSubjects
        || this.isLoadingTerms    || this.isLoadingAssessments;
  }
}