import {
  Component,
  Inject,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
  AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import {
  AssessmentDto,
  CreateAssessmentRequest,
  UpdateAssessmentRequest,
  TeacherLookup,
  SubjectLookup,
  ClassLookup,
  TermLookup,
  AcademicYearLookup
} from 'app/assessment/types/AssessmentDtos';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { SchoolDto } from 'app/Tenant/types/school';
import { TermService } from 'app/core/DevKenService/TermService/term.service';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { ClassService } from 'app/core/DevKenService/ClassService';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';

// ── Tab definition ────────────────────────────────────────────────────────────

type TabId = 'basic' | 'assignment';

interface TabConfig {
  id: TabId;
  label: string;
  icon: string;
  fields: string[];
}

// ── Dialog data contract ──────────────────────────────────────────────────────

export interface CreateEditAssessmentDialogData {
  mode: 'create' | 'edit';
  assessment?: AssessmentDto;
  schools: SchoolDto[];
}

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-create-edit-assessment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
  ],
  templateUrl: './create-edit-assessment-dialog.component.html',
  styleUrls:  ['./create-edit-assessment-dialog.component.scss'],
})
export class CreateEditAssessmentDialogComponent
  extends BaseFormDialog<
    CreateAssessmentRequest,
    UpdateAssessmentRequest,
    AssessmentDto,
    CreateEditAssessmentDialogData
  >
  implements OnInit, AfterViewInit, OnDestroy
{
  private readonly _unsubscribe      = new Subject<void>();
  private readonly _cdr              = inject(ChangeDetectorRef);
  private readonly _auth             = inject(AuthService);
  private readonly _academicYearSvc  = inject(AcademicYearService);
  private readonly _termSvc          = inject(TermService);
  private readonly _classSvc         = inject(ClassService);
  private readonly _teacherSvc       = inject(TeacherService);

  // ── Lookup data ──────────────────────────────────────────────────────────
  teachers:      TeacherLookup[]      = [];
  subjects:      SubjectLookup[]      = [];
  classes:       ClassLookup[]        = [];
  terms:         TermLookup[]         = [];
  academicYears: AcademicYearLookup[] = [];

  // ── Component state ───────────────────────────────────────────────────────
  isLookupsLoading = true;
  formSubmitted    = false;
  activeTabId: TabId = 'basic';

  // ── Tab configuration ─────────────────────────────────────────────────────
  readonly tabs: TabConfig[] = [
    {
      id: 'basic',
      label: 'Basic Info',
      icon: 'info',
      fields: ['title', 'assessmentType', 'maximumScore', 'assessmentDate', 'schoolId'],
    },
    {
      id: 'assignment',
      label: 'Assignment',
      icon: 'group_work',
      fields: ['academicYearId', 'termId', 'classId', 'subjectId', 'teacherId'],
    },
  ];

  // ── Auth helpers ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._auth.authUser?.isSuperAdmin ?? false; }
  get isEditMode():   boolean { return this.data.mode === 'edit'; }

  // ── Tab helpers ───────────────────────────────────────────────────────────
  get currentTabIndex(): number { return this.tabs.findIndex(t => t.id === this.activeTabId); }
  get isFirstTab():      boolean { return this.currentTabIndex === 0; }
  get isLastTab():       boolean { return this.currentTabIndex === this.tabs.length - 1; }

  // ── Form readiness — drives the submit button disabled state ─────────────
  /**
   * Returns true only when:
   *  1. Lookups have finished loading (so selects are populated)
   *  2. The reactive form is valid (all required fields filled, no validation errors)
   *  3. A save operation is not already in flight
   */
  get isFormReady(): boolean {
    return !this.isLookupsLoading && this.form?.valid === true && !this.isSaving;
  }

  /**
   * Count of invalid required fields across ALL tabs so the footer can
   * display a helpful "X fields remaining" hint.
   */
  get invalidFieldCount(): number {
    if (!this.form) return 0;
    return Object.keys(this.form.controls).filter(k => this.form.get(k)?.invalid).length;
  }

  // ── Summary card getters ──────────────────────────────────────────────────
  get hasAssignmentSummary(): boolean {
    const v = this.form?.value;
    return !!(v?.academicYearId || v?.termId || v?.classId || v?.subjectId || v?.teacherId);
  }

  get selectedAcademicYearName(): string | null {
    const id = this.form?.value?.academicYearId;
    return id ? (this.academicYears.find(y => y.id === id)?.name ?? null) : null;
  }

  get selectedTermName(): string | null {
    const id = this.form?.value?.termId;
    return id ? (this.terms.find(t => t.id === id)?.name ?? null) : null;
  }

  get selectedClassName(): string | null {
    const id = this.form?.value?.classId;
    return id ? (this.classes.find(c => c.id === id)?.name ?? null) : null;
  }

  get selectedSubjectName(): string | null {
    const id = this.form?.value?.subjectId;
    return id ? (this.subjects.find(s => s.id === id)?.name ?? null) : null;
  }

  get selectedTeacherName(): string | null {
    const id = this.form?.value?.teacherId;
    return id ? (this.teachers.find(t => t.id === id)?.fullName ?? null) : null;
  }

  // ── Constructor ───────────────────────────────────────────────────────────
  constructor(
    fb: FormBuilder,
    service: AssessmentService,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditAssessmentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public override data: CreateEditAssessmentDialogData
  ) {
    super(fb, service, snackBar, dialogRef, data);
    dialogRef.addPanelClass(['assessment-dialog', 'no-padding-dialog']);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.init();          // BaseFormDialog.init() builds the form via buildForm()
    this._loadLookups();
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── BaseFormDialog abstract implementations ───────────────────────────────
  protected override buildForm(): FormGroup {
    // schoolId validator is conditional on isSuperAdmin
    const schoolIdValidators = this.isSuperAdmin ? [Validators.required] : [];

    return this.fb.group({
      schoolId:       [null,  schoolIdValidators],
      title:          ['',    [Validators.required, Validators.maxLength(200)]],
      description:    ['',    [Validators.maxLength(500)]],
      assessmentType: [null,  [Validators.required]],
      maximumScore:   [null,  [Validators.required, Validators.min(0.01)]],
      assessmentDate: [null,  [Validators.required]],
      academicYearId: [null,  [Validators.required]],
      termId:         [null,  [Validators.required]],
      classId:        [null,  [Validators.required]],
      subjectId:      [null,  [Validators.required]],
      teacherId:      [null,  [Validators.required]],
    });
  }

  protected override patchForEdit(item: AssessmentDto): void {
    // Guard: do not patch before the form exists
    if (!this.form) return;

    this.form.patchValue({
      schoolId:       item.schoolId       ?? null,
      title:          item.title          ?? '',
      description:    item.description    ?? '',
      assessmentType: item.assessmentType ?? null,
      maximumScore:   item.maximumScore   ?? null,
      // Ensure the date is a proper Date object so the datepicker renders correctly
      assessmentDate: item.assessmentDate ? new Date(item.assessmentDate) : null,
      academicYearId: item.academicYearId ?? null,
      termId:         item.termId         ?? null,
      classId:        item.classId        ?? null,
      subjectId:      item.subjectId      ?? null,
      teacherId:      item.teacherId      ?? null,
    });

    // Mark the form as pristine after patching so dirty-state logic
    // (e.g. unsaved-changes guards) reflects the initial loaded state.
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  // ── Lookup loading ────────────────────────────────────────────────────────
  private _loadLookups(): void {
    this.isLookupsLoading = true;

    // For SuperAdmin in edit mode, scope lookups to the assessment's school
    const schoolId = this.isSuperAdmin
      ? (this.data.assessment?.schoolId ?? undefined)
      : undefined;

    forkJoin({
      academicYears: this._academicYearSvc.getAll(schoolId).pipe(
        map(r => r.data.map(y => ({ id: y.id, name: y.name } as AcademicYearLookup))),
        catchError(() => of<AcademicYearLookup[]>([]))
      ),
      terms: this._termSvc.getAll(schoolId).pipe(
        map(r => r.data.map(t => ({ id: t.id, name: t.name } as TermLookup))),
        catchError(() => of<TermLookup[]>([]))
      ),
      classes: this._classSvc.getAll(schoolId).pipe(
        map(r => r.data.map(c => ({ id: c.id, name: c.name } as ClassLookup))),
        catchError(() => of<ClassLookup[]>([]))
      ),
      teachers: this._teacherSvc.getAll(schoolId).pipe(
        map(r => r.data.map(t => ({
          id:       t.id,
          fullName: t.fullName,
        } as TeacherLookup))),
        catchError(() => of<TeacherLookup[]>([]))
      ),
      subjects: of<SubjectLookup[]>([]),  // ⚠ replace with _subjectSvc.getAll(schoolId) when ready
    }).pipe(
      takeUntil(this._unsubscribe),
      catchError(() => of({
        academicYears: [] as AcademicYearLookup[],
        terms:         [] as TermLookup[],
        classes:       [] as ClassLookup[],
        teachers:      [] as TeacherLookup[],
        subjects:      [] as SubjectLookup[],
      })),
      finalize(() => {
        this.isLookupsLoading = false;
        this._cdr.detectChanges();
      })
    ).subscribe(results => {
      this.academicYears = results.academicYears;
      this.terms         = results.terms;
      this.classes       = results.classes;
      this.teachers      = results.teachers;
      this.subjects      = results.subjects;

      // ── Patch AFTER lookups so mat-select options are present ──────────
      // This is critical: patching before options are loaded causes Angular
      // Material selects to silently discard the value.
      if (this.isEditMode && this.data.assessment) {
        this.patchForEdit(this.data.assessment);
      }

      this._cdr.detectChanges();
    });

    // Safety valve — release loading state after 12 s if forkJoin stalls
    setTimeout(() => {
      if (this.isLookupsLoading) {
        this.isLookupsLoading = false;
        this._cdr.detectChanges();
      }
    }, 12_000);
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    // Touch every control so mat-error messages appear immediately
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      // Navigate to the first tab that contains an invalid field
      for (const tab of this.tabs) {
        if (tab.fields.some(f => this.form.get(f)?.invalid)) {
          this.activeTabId = tab.id;
          break;
        }
      }
      this._cdr.detectChanges();
      return;
    }

    this.save(
      (raw): CreateAssessmentRequest => ({
        title:          raw.title?.trim(),
        description:    raw.description?.trim() || null,
        assessmentType: raw.assessmentType,
        maximumScore:   raw.maximumScore,
        assessmentDate: this._toIso(raw.assessmentDate)!,
        academicYearId: raw.academicYearId,
        termId:         raw.termId,
        classId:        raw.classId,
        subjectId:      raw.subjectId,
        teacherId:      raw.teacherId,
        schoolId:       this.isSuperAdmin ? raw.schoolId : null,
      }),
      (raw): UpdateAssessmentRequest => ({
        title:          raw.title?.trim(),
        description:    raw.description?.trim() || null,
        assessmentType: raw.assessmentType,
        maximumScore:   raw.maximumScore,
        assessmentDate: this._toIso(raw.assessmentDate)!,
        academicYearId: raw.academicYearId,
        termId:         raw.termId,
        classId:        raw.classId,
        subjectId:      raw.subjectId,
        teacherId:      raw.teacherId,
      }),
      () => this.data.assessment!.id
    );
  }

  // ── Tab navigation ────────────────────────────────────────────────────────
  setTab(id: TabId): void { this.activeTabId = id; }

  nextTab(): void {
    const idx = this.currentTabIndex;
    if (idx < this.tabs.length - 1) this.activeTabId = this.tabs[idx + 1].id;
  }

  prevTab(): void {
    const idx = this.currentTabIndex;
    if (idx > 0) this.activeTabId = this.tabs[idx - 1].id;
  }

  // ── Validation helpers ────────────────────────────────────────────────────
  tabHasErrors(tab: TabConfig): boolean {
    return this.formSubmitted && tab.fields.some(f => this.form.get(f)?.invalid);
  }

  /**
   * Returns an inline error message for a given field.
   * Shows after the field is touched OR after the first submit attempt.
   */
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))   return 'This field is required';
    if (c.hasError('email'))      return 'Enter a valid email address';
    if (c.hasError('min'))        return `Value must be at least ${c.getError('min').min}`;
    if (c.hasError('maxlength'))  return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }

  // ── Utility ───────────────────────────────────────────────────────────────
  private _toIso(val: unknown): string | null {
    if (!val) return null;
    const d = val instanceof Date ? val : new Date(val as string);
    return isNaN(d.getTime()) ? null : d.toISOString();
  }
}