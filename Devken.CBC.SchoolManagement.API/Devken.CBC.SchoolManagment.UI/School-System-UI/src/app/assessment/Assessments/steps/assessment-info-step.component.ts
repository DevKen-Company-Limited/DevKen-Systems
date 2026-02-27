// ═══════════════════════════════════════════════════════════════════
// steps/assessment-info-step.component.ts
//
// FIXES applied in this version:
//
//  FIX 1 – Edit mode fields not populated (school, class, teacher…):
//   Root cause: Angular processes @Input() changes in a single CD pass
//   but the ORDER in which parent binds [formData] vs [schools]/
//   [classes]/etc. is not guaranteed. If formData arrives first,
//   mat-select writes data.schoolId before filtered.schools exists,
//   so the option lookup fails silently → blank selection.
//
//   Solution: in ngOnChanges, when formData changes we defer the
//   actual data copy by ONE tick (setTimeout(0)) so that ALL sibling
//   @Input changes — including the arrays — are applied first. By the
//   time our callback runs, filtered.schools already holds the school
//   option and mat-select can match it.
//
//  FIX 2 – Super Admin school not pre-selected on edit:
//   When editing an existing assessment as SuperAdmin, the parent
//   passes formData.schoolId but the school dropdown was blank because
//   filtered.schools hadn't been populated yet (same race as above).
//   The deferred copy from FIX 1 already resolves this. Additionally,
//   we now call _ensureSchoolOption() after the deferred copy runs to
//   guarantee the school option is present in the filtered list even
//   when the school appears in the full schools[] array but the
//   filtered list was rebuilt before schools[] arrived.
//
//  FIX 3 – Dependent dropdowns blank after school pre-select on edit:
//   When editing, we should NOT clear dependent fields on load
//   (onSchoolChange clears them). We only clear when the user actively
//   changes the school. The deferred data copy already handles this
//   correctly by copying formData wholesale without triggering
//   onSchoolChange.
// ═══════════════════════════════════════════════════════════════════

import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, OnDestroy, SimpleChanges,
} from '@angular/core';
import { CommonModule }         from '@angular/common';
import { FormsModule }          from '@angular/forms';
import { MatFormFieldModule }   from '@angular/material/form-field';
import { MatInputModule }       from '@angular/material/input';
import { MatSelectModule }      from '@angular/material/select';
import { MatDatepickerModule }  from '@angular/material/datepicker';
import { MatNativeDateModule }  from '@angular/material/core';
import { MatIconModule }        from '@angular/material/icon';
import { MatCardModule }        from '@angular/material/card';
import { MatDividerModule }     from '@angular/material/divider';
import { MatTooltipModule }     from '@angular/material/tooltip';
import { FuseAlertComponent }   from '@fuse/components/alert';
import { Subject }              from 'rxjs';

@Component({
  selector: 'app-assessment-info-step',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatDatepickerModule, MatNativeDateModule,
    MatIconModule, MatCardModule, MatDividerModule,
    MatTooltipModule, FuseAlertComponent,
  ],
  template: `
<div class="max-w-3xl mx-auto">

  <!-- ── Section Header ────────────────────────────────────────── -->
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Basic Information</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">
      Enter core details for this assessment.
    </p>
  </div>

  <!-- ── SuperAdmin: School selector first ─────────────────────── -->
  <mat-card *ngIf="isSuperAdmin" class="mb-6 shadow-sm">
    <mat-card-header>
      <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
        <mat-icon class="text-red-500 icon-size-4">apartment</mat-icon>
        School Selection
        <span class="ml-auto text-xs font-medium px-2 py-0.5
                     bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded">
          Required first
        </span>
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <fuse-alert type="info" appearance="soft" [showIcon]="true" class="mb-4">
        As a Super Admin, please select a school first. All other dropdowns will
        be filtered to that school's data.
      </fuse-alert>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>School <span class="text-red-500">*</span></mat-label>
        <mat-select [(ngModel)]="data.schoolId" (ngModelChange)="onSchoolChange()">
          <!-- Embedded search -->
          <mat-option disabled class="!h-auto !px-0 !py-0">
            <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
              <input
                class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                       bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                       focus:ring-2 focus:ring-indigo-500"
                placeholder="Type to search schools…"
                (keydown.Space)="$event.stopPropagation()"
                [(ngModel)]="filters.school"
                (ngModelChange)="filterList('school')" />
            </div>
          </mat-option>
          <mat-option value="">— Select School —</mat-option>
          <mat-option *ngFor="let s of filtered.schools" [value]="s.id">{{ s.name }}</mat-option>
        </mat-select>
        <mat-icon matPrefix class="text-gray-400">apartment</mat-icon>
        <mat-error *ngIf="touched && isSuperAdmin && !data.schoolId">
          School is required for Super Admin users
        </mat-error>
      </mat-form-field>
    </mat-card-content>
  </mat-card>

  <!-- ── Main Info Card ─────────────────────────────────────────── -->
  <mat-card class="shadow-sm mb-6">
    <mat-card-header>
      <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
        <mat-icon class="text-indigo-600 icon-size-4">info</mat-icon>
        Assessment Details
        <span class="ml-auto text-xs font-medium px-2 py-0.5
                     bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded">
          Required
        </span>
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4 space-y-4">

      <!-- Title -->
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Title <span class="text-red-500">*</span></mat-label>
        <input matInput [(ngModel)]="data.title" (ngModelChange)="onChange()"
          placeholder="e.g. End of Term Mathematics Examination" />
        <mat-icon matPrefix class="text-gray-400">title</mat-icon>
        <mat-error *ngIf="touched && !data.title?.trim()">Title is required</mat-error>
      </mat-form-field>

      <!-- Description -->
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Description</mat-label>
        <textarea matInput [(ngModel)]="data.description" (ngModelChange)="onChange()"
          rows="3" placeholder="Brief description of this assessment…"></textarea>
        <mat-icon matPrefix class="text-gray-400">description</mat-icon>
        <mat-hint>Optional — max 500 characters</mat-hint>
      </mat-form-field>

      <!-- Assessment Date -->
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Assessment Date <span class="text-red-500">*</span></mat-label>
        <input matInput [matDatepicker]="datePicker"
          [(ngModel)]="data.assessmentDate" (ngModelChange)="onChange()"
          placeholder="Pick a date" />
        <mat-datepicker-toggle matSuffix [for]="datePicker"></mat-datepicker-toggle>
        <mat-datepicker #datePicker></mat-datepicker>
        <mat-icon matPrefix class="text-gray-400">event</mat-icon>
        <mat-error *ngIf="touched && !data.assessmentDate">Date is required</mat-error>
      </mat-form-field>

    </mat-card-content>
  </mat-card>

  <!-- ── Class, Subject, Teacher, Term, Academic Year ──────────── -->
  <mat-card class="shadow-sm mb-6"
    [class.opacity-50]="isSuperAdmin && !data.schoolId"
    [matTooltip]="isSuperAdmin && !data.schoolId ? 'Please select a school first' : ''">
    <mat-card-header>
      <mat-card-title class="!text-sm !font-semibold flex items-center gap-2">
        <mat-icon class="text-indigo-600 icon-size-4">school</mat-icon>
        Class, Subject &amp; Teacher
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

        <!-- Class -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Class</mat-label>
          <mat-select [(ngModel)]="data.classId" (ngModelChange)="onChange()"
            [disabled]="isSuperAdmin && !data.schoolId">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                              bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                              focus:ring-2 focus:ring-indigo-500"
                  placeholder="Search classes…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="filters.class"
                  (ngModelChange)="filterList('class')" />
              </div>
            </mat-option>
            <mat-option value="">— Select Class —</mat-option>
            <mat-option *ngFor="let c of filtered.classes" [value]="c.id">{{ c.name }}</mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">class</mat-icon>
        </mat-form-field>

        <!-- Subject -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Subject</mat-label>
          <mat-select [(ngModel)]="data.subjectId" (ngModelChange)="onChange()"
            [disabled]="isSuperAdmin && !data.schoolId">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                              bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                              focus:ring-2 focus:ring-indigo-500"
                  placeholder="Search subjects…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="filters.subject"
                  (ngModelChange)="filterList('subject')" />
              </div>
            </mat-option>
            <mat-option value="">— Select Subject —</mat-option>
            <mat-option *ngFor="let s of filtered.subjects" [value]="s.id">{{ s.name }}</mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">menu_book</mat-icon>
        </mat-form-field>

        <!-- Teacher -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Teacher</mat-label>
          <mat-select [(ngModel)]="data.teacherId" (ngModelChange)="onChange()"
            [disabled]="isSuperAdmin && !data.schoolId">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                              bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                              focus:ring-2 focus:ring-indigo-500"
                  placeholder="Search teachers…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="filters.teacher"
                  (ngModelChange)="filterList('teacher')" />
              </div>
            </mat-option>
            <mat-option value="">— Select Teacher —</mat-option>
            <mat-option *ngFor="let t of filtered.teachers" [value]="t.id">
              {{ t.firstName }} {{ t.lastName }}
            </mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">person</mat-icon>
        </mat-form-field>

        <!-- Term -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Term</mat-label>
          <mat-select [(ngModel)]="data.termId" (ngModelChange)="onChange()"
            [disabled]="isSuperAdmin && !data.schoolId">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                              bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                              focus:ring-2 focus:ring-indigo-500"
                  placeholder="Search terms…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="filters.term"
                  (ngModelChange)="filterList('term')" />
              </div>
            </mat-option>
            <mat-option value="">— Select Term —</mat-option>
            <mat-option *ngFor="let t of filtered.terms" [value]="t.id">{{ t.name }}</mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">date_range</mat-icon>
        </mat-form-field>

        <!-- Academic Year -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Academic Year</mat-label>
          <mat-select [(ngModel)]="data.academicYearId" (ngModelChange)="onChange()"
            [disabled]="isSuperAdmin && !data.schoolId">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input class="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm
                              bg-white dark:bg-gray-700 text-gray-900 dark:text-white outline-none
                              focus:ring-2 focus:ring-indigo-500"
                  placeholder="Search academic years…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="filters.academicYear"
                  (ngModelChange)="filterList('academicYear')" />
              </div>
            </mat-option>
            <mat-option value="">— Select Academic Year —</mat-option>
            <mat-option *ngFor="let y of filtered.academicYears" [value]="y.id">{{ y.name }}</mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">calendar_today</mat-icon>
        </mat-form-field>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Validation summary ──────────────────────────────────────── -->
  <div *ngIf="touched && !isValid()"
    class="flex items-center gap-3 p-4 bg-amber-50 dark:bg-amber-900/20
           border border-amber-200 dark:border-amber-800 rounded-xl">
    <mat-icon class="text-amber-600 flex-shrink-0">warning</mat-icon>
    <p class="text-sm text-amber-700 dark:text-amber-400 font-medium">
      Please fill in all required fields marked with <span class="text-red-500">*</span>
    </p>
  </div>

</div>
  `,
})
export class AssessmentInfoStepComponent implements OnInit, OnChanges, OnDestroy {

  @Input() formData:      any   = {};
  @Input() classes:       any[] = [];
  @Input() teachers:      any[] = [];
  @Input() subjects:      any[] = [];
  @Input() terms:         any[] = [];
  @Input() academicYears: any[] = [];
  @Input() schools:       any[] = [];
  @Input() isEditMode           = false;
  @Input() isSuperAdmin         = false;

  @Output() formChanged   = new EventEmitter<any>();
  @Output() formValid     = new EventEmitter<boolean>();
  @Output() schoolChanged = new EventEmitter<string>();

  data:    any = {};
  touched  = false;

  private _destroy$       = new Subject<void>();
  /** Tracks whether a deferred formData timer is pending. */
  private _pendingTimer: any = null;
  /**
   * Remembers the last formData reference so that when lookup arrays
   * arrive after formData (inverse order), we can re-apply the data
   * now that the options exist.
   */
  private _lastFormData: any = null;

  // ── Search filter state ─────────────────────────────────────────
  filters: Record<string, string> = {
    school: '', class: '', subject: '', teacher: '', term: '', academicYear: '',
  };

  filtered: Record<string, any[]> = {
    schools: [], classes: [], subjects: [], teachers: [], terms: [], academicYears: [],
  };

  // ── Lifecycle ──────────────────────────────────────────────────

  ngOnInit(): void {
    this.data = { ...this.formData };
    this._lastFormData = this.formData;
    this._resetFiltered();
    this._emitValid();
  }

  ngOnChanges(c: SimpleChanges): void {
    // ─────────────────────────────────────────────────────────────────
    // CRITICAL FIX (order-of-input race):
    //
    // mat-select requires option items to exist in the DOM at the exact
    // moment its ngModel value is written. When the parent populates
    // [formData] and [schools]/[classes]/… in the same CD pass, Angular
    // applies @Input() bindings in template declaration order — the
    // arrays may not have updated filtered.* yet when formData arrives.
    //
    // Solution A (formData first): defer the data copy by one macrotask
    // so all array @Inputs — and _resetFiltered() — run first. By the
    // time setTimeout fires, filtered.* holds the correct options.
    //
    // Solution B (arrays first): when lookup arrays change we call
    // _resetFiltered() immediately AND, if we already have a formData
    // reference (_lastFormData), we re-apply it so the selects can
    // now match their values against the newly available options.
    // ─────────────────────────────────────────────────────────────────

    if (c['formData']) {
      this._lastFormData = this.formData;

      // Cancel any previous pending timer to avoid double-apply
      if (this._pendingTimer) clearTimeout(this._pendingTimer);

      this._pendingTimer = setTimeout(() => {
        this._pendingTimer = null;
        this.data = { ...this.formData };
        // Ensure the school's option is present in filtered.schools
        // (covers the edge case where schools[] changed after formData)
        this._ensureSchoolOption();
        this._emitValid();
      }, 0);
    }

    // When lookup arrays arrive, refresh filtered copies immediately.
    if (c['classes']  || c['teachers'] || c['subjects'] ||
        c['terms']    || c['academicYears'] || c['schools']) {
      this._resetFiltered();

      // FIX: If the deferred timer has already fired (data was applied
      // before arrays arrived), force a re-evaluation so mat-select
      // can match the now-available options. We do this by reassigning
      // data to a new object reference — Angular's CD will pick up the
      // change and mat-select will re-check option matches.
      if (!this._pendingTimer && this._lastFormData) {
        // Only re-apply if we're in edit mode and data has been set
        if (this.isEditMode && this.data && Object.keys(this.data).length > 0) {
          this.data = { ...this.data };
          this._ensureSchoolOption();
        }
      }
    }
  }

  ngOnDestroy(): void {
    if (this._pendingTimer) clearTimeout(this._pendingTimer);
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ── Filtering ──────────────────────────────────────────────────

  private _resetFiltered(): void {
    this.filtered.schools       = [...this.schools];
    this.filtered.classes       = [...this.classes];
    this.filtered.subjects      = [...this.subjects];
    this.filtered.teachers      = [...this.teachers];
    this.filtered.terms         = [...this.terms];
    this.filtered.academicYears = [...this.academicYears];
  }

  /**
   * Guarantees that the school matching data.schoolId is present in
   * filtered.schools even when the user has typed something in the
   * school search box. This is important on edit-mode load: the
   * selected school must be visible as an option regardless of the
   * current search text.
   */
  private _ensureSchoolOption(): void {
    if (!this.data?.schoolId || !this.isSuperAdmin) return;
    const alreadyPresent = this.filtered.schools.some(s => s.id === this.data.schoolId);
    if (!alreadyPresent) {
      const match = this.schools.find(s => s.id === this.data.schoolId);
      if (match) {
        this.filtered.schools = [match, ...this.filtered.schools];
      }
    }
  }

  filterList(key: string): void {
    const q = (this.filters[key] || '').toLowerCase();

    const getName = (item: any): string =>
      item.firstName
        ? `${item.firstName} ${item.lastName}`.toLowerCase()
        : (item.name || '').toLowerCase();

    const sourceMap: Record<string, any[]> = {
      school:       this.schools,
      class:        this.classes,
      subject:      this.subjects,
      teacher:      this.teachers,
      term:         this.terms,
      academicYear: this.academicYears,
    };

    const destKey = key === 'class'        ? 'classes'
                  : key === 'academicYear' ? 'academicYears'
                  : `${key}s`;

    const src = sourceMap[key] ?? [];
    this.filtered[destKey] = q ? src.filter(i => getName(i).includes(q)) : [...src];

    // After filtering the school list, always keep the currently selected
    // school visible so the mat-select label doesn't go blank.
    if (key === 'school') {
      this._ensureSchoolOption();
    }
  }

  // ── School change — cascades all dependent dropdowns ───────────
  // NOTE: this is only called when the USER actively changes the school
  // via the UI. It is NOT called during the deferred data copy on edit
  // mode load, so existing dependent selections are preserved.

  onSchoolChange(): void {
    // Clear dependent selections so stale data isn't submitted
    this.data.classId        = '';
    this.data.subjectId      = '';
    this.data.teacherId      = '';
    this.data.termId         = '';
    this.data.academicYearId = '';
    // Notify parent; parent will reload scoped lookups
    this.schoolChanged.emit(this.data.schoolId ?? '');
    this.onChange();
  }

  // ── Form events ────────────────────────────────────────────────

  onChange(): void {
    this.touched = true;
    this.formChanged.emit({ ...this.data });
    this._emitValid();
  }

  isValid(): boolean {
    const hasRequired = !!(this.data.title?.trim() && this.data.assessmentDate);
    const schoolOk    = !this.isSuperAdmin || !!this.data.schoolId;
    return hasRequired && schoolOk;
  }

  private _emitValid(): void { this.formValid.emit(this.isValid()); }
}