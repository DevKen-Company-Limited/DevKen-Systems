// ═══════════════════════════════════════════════════════════════════
// steps/assessment-review-step.component.ts
// Full summary review — resolves enum numbers to readable labels.
// NO debug "(enum: N)" suffixes in this final version.
// ═══════════════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FuseAlertComponent } from '@fuse/components/alert';
import { AssessmentType } from 'app/assessment/types/assessments';

@Component({
  selector: 'app-assessment-review-step',
  standalone: true,
  imports: [
    CommonModule, MatIconModule, MatButtonModule,
    MatCardModule, MatDividerModule, MatTooltipModule,
    FuseAlertComponent,
  ],
  template: `
<div class="max-w-3xl mx-auto">

  <!-- ── Section Header ───────────────────────────────────────── -->
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Review &amp; Save</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Confirm all details before saving.</p>
  </div>

  <div class="space-y-4">

    <!-- ── Basic Info Card ──────────────────────────────────────── -->
    <mat-card class="shadow-sm">
      <mat-card-header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-full bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center">
              <mat-icon class="text-indigo-600 icon-size-4">info</mat-icon>
            </div>
            <mat-card-title class="!text-base">Basic Information</mat-card-title>
          </div>
          <button mat-stroked-button (click)="editSection.emit(0)" class="text-sm">
            <mat-icon class="icon-size-4">edit</mat-icon>
            <span class="ml-1">Edit</span>
          </button>
        </div>
      </mat-card-header>
      <mat-card-content class="!pt-2">
        <div class="grid grid-cols-2 gap-x-6 gap-y-4 pt-2">

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Title</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ info.title || '—' }}</p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Date</p>
            <p class="font-medium text-gray-900 dark:text-white">
              {{ info.assessmentDate | date:'mediumDate' }}
            </p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Class</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getClassName() || '—' }}</p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Subject</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getSubjectName() || '—' }}</p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Teacher</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getTeacherName() || '—' }}</p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Term</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getTermName() || '—' }}</p>
          </div>

          <div>
            <p class="text-xs text-gray-400 mb-0.5">Academic Year</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getAcademicYearName() || '—' }}</p>
          </div>

          <div *ngIf="info.schoolId" class="col-span-2">
            <p class="text-xs text-gray-400 mb-0.5">School</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ getSchoolName() || '—' }}</p>
          </div>

          <div *ngIf="info.description" class="col-span-2">
            <p class="text-xs text-gray-400 mb-0.5">Description</p>
            <p class="font-medium text-gray-900 dark:text-white">{{ info.description }}</p>
          </div>

        </div>
      </mat-card-content>
    </mat-card>

    <!-- ── Assessment Details Card ──────────────────────────────── -->
    <mat-card class="shadow-sm">
      <mat-card-header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-full flex items-center justify-center"
              [ngClass]="{
                'bg-indigo-100 dark:bg-indigo-900/30': details.assessmentType === formativeType,
                'bg-violet-100 dark:bg-violet-900/30': details.assessmentType === summativeType,
                'bg-teal-100   dark:bg-teal-900/30':   details.assessmentType === competencyType
              }">
              <mat-icon class="icon-size-4"
                [ngClass]="{
                  'text-indigo-600': details.assessmentType === formativeType,
                  'text-violet-600': details.assessmentType === summativeType,
                  'text-teal-600':   details.assessmentType === competencyType
                }">tune</mat-icon>
            </div>
            <mat-card-title class="!text-base">Assessment Details</mat-card-title>
          </div>
          <button mat-stroked-button (click)="editSection.emit(1)" class="text-sm">
            <mat-icon class="icon-size-4">edit</mat-icon>
            <span class="ml-1">Edit</span>
          </button>
        </div>
      </mat-card-header>
      <mat-card-content class="!pt-2">

        <!-- Shared: Type + Max Score -->
        <div class="grid grid-cols-2 gap-x-6 gap-y-4 pt-2 mb-4">
          <div>
            <p class="text-xs text-gray-400 mb-0.5">Type</p>
            <span class="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-bold border"
              [ngClass]="{
                'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-900/30 dark:text-indigo-300': details.assessmentType === formativeType,
                'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-900/30 dark:text-violet-300': details.assessmentType === summativeType,
                'bg-teal-50   text-teal-700   border-teal-200   dark:bg-teal-900/30   dark:text-teal-300':   details.assessmentType === competencyType
              }">
              {{ typeLabel }}
            </span>
          </div>
          <div>
            <p class="text-xs text-gray-400 mb-0.5">Maximum Score</p>
            <p class="font-bold text-gray-900 dark:text-white text-xl">{{ details.maximumScore || '—' }}</p>
          </div>
        </div>

        <mat-divider class="my-3"></mat-divider>

        <!-- ── FORMATIVE fields ──────────────────────────────────── -->
        <ng-container *ngIf="details.assessmentType === formativeType">
          <div class="grid grid-cols-2 gap-x-6 gap-y-3">

            <div *ngIf="details.formativeType">
              <p class="text-xs text-gray-400 mb-0.5">Formative Type</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.formativeType }}</p>
            </div>

            <div *ngIf="details.assessmentWeight != null">
              <p class="text-xs text-gray-400 mb-0.5">Weight (%)</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.assessmentWeight }}%</p>
            </div>

            <div *ngIf="details.competencyArea">
              <p class="text-xs text-gray-400 mb-0.5">Competency Area</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyArea }}</p>
            </div>

            <div>
              <p class="text-xs text-gray-400 mb-0.5">Requires Rubric</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                {{ details.requiresRubric ? 'Yes' : 'No' }}
              </p>
            </div>

            <div *ngIf="details.criteria" class="col-span-2">
              <p class="text-xs text-gray-400 mb-0.5">Criteria</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.criteria }}</p>
            </div>

          </div>
        </ng-container>

        <!-- ── SUMMATIVE fields ──────────────────────────────────── -->
        <ng-container *ngIf="details.assessmentType === summativeType">
          <div class="grid grid-cols-2 gap-x-6 gap-y-3">

            <div *ngIf="details.examType">
              <p class="text-xs text-gray-400 mb-0.5">Exam Type</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.examType }}</p>
            </div>

            <div *ngIf="details.passMark != null">
              <p class="text-xs text-gray-400 mb-0.5">Pass Mark</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.passMark }}%</p>
            </div>

            <div *ngIf="details.duration">
              <p class="text-xs text-gray-400 mb-0.5">Duration</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.duration }}</p>
            </div>

            <div *ngIf="details.numberOfQuestions">
              <p class="text-xs text-gray-400 mb-0.5">Questions</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.numberOfQuestions }}</p>
            </div>

            <div *ngIf="details.theoryWeight != null">
              <p class="text-xs text-gray-400 mb-0.5">Theory Weight</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.theoryWeight }}%</p>
            </div>

            <div>
              <p class="text-xs text-gray-400 mb-0.5">Has Practical</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                {{ details.hasPracticalComponent
                   ? 'Yes (' + (details.practicalWeight || 0) + '%)'
                   : 'No' }}
              </p>
            </div>

          </div>
        </ng-container>

        <!-- ── COMPETENCY fields ─────────────────────────────────── -->
        <ng-container *ngIf="details.assessmentType === competencyType">
          <div class="grid grid-cols-2 gap-x-6 gap-y-3">

            <div class="col-span-2" *ngIf="details.competencyName">
              <p class="text-xs text-gray-400 mb-0.5">Competency Name</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyName }}</p>
            </div>

            <div *ngIf="details.competencyStrand">
              <p class="text-xs text-gray-400 mb-0.5">Strand</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencyStrand }}</p>
            </div>

            <div *ngIf="details.competencySubStrand">
              <p class="text-xs text-gray-400 mb-0.5">Sub-Strand</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.competencySubStrand }}</p>
            </div>

            <!-- CBC Level: numeric enum → readable label only (no debug suffix) -->
            <div *ngIf="details.targetLevel != null">
              <p class="text-xs text-gray-400 mb-0.5">CBC Level</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                {{ targetLevelLabel(details.targetLevel) }}
              </p>
            </div>

            <!-- Assessment Method: numeric enum → readable label only -->
            <div *ngIf="details.assessmentMethod != null">
              <p class="text-xs text-gray-400 mb-0.5">Assessment Method</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                {{ assessmentMethodLabel(details.assessmentMethod) }}
              </p>
            </div>

            <div *ngIf="details.ratingScale">
              <p class="text-xs text-gray-400 mb-0.5">Rating Scale</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.ratingScale }}</p>
            </div>

            <div>
              <p class="text-xs text-gray-400 mb-0.5">Observation Based</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                {{ details.isObservationBased ? 'Yes' : 'No' }}
              </p>
            </div>

            <div *ngIf="details.toolsRequired" class="col-span-2">
              <p class="text-xs text-gray-400 mb-0.5">Tools Required</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.toolsRequired }}</p>
            </div>

            <div *ngIf="details.performanceIndicators" class="col-span-2">
              <p class="text-xs text-gray-400 mb-0.5">Performance Indicators</p>
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ details.performanceIndicators }}</p>
            </div>

          </div>
        </ng-container>

      </mat-card-content>
    </mat-card>

    <!-- ── Validation Indicator ──────────────────────────────────── -->
    <fuse-alert *ngIf="allValid()" type="success" appearance="soft" [showIcon]="true">
      All required fields are filled. Ready to save.
    </fuse-alert>

    <fuse-alert *ngIf="!allValid()" type="warning" appearance="soft" [showIcon]="true">
      Some required fields are missing. Please go back and complete all required fields
      before submitting.
    </fuse-alert>

  </div>
</div>
  `,
})
export class AssessmentReviewStepComponent {

  readonly formativeType  = AssessmentType.Formative;
  readonly summativeType  = AssessmentType.Summative;
  readonly competencyType = AssessmentType.Competency;

  @Input() formSections:    Record<string, any> = {};
  @Input() classes:         any[] = [];
  @Input() teachers:        any[] = [];
  @Input() subjects:        any[] = [];
  @Input() terms:           any[] = [];
  @Input() academicYears:   any[] = [];
  @Input() schools:         any[] = [];
  @Input() steps:           any[] = [];
  @Input() completedSteps!: Set<number>;

  @Output() editSection = new EventEmitter<number>();

  get info():    any { return this.formSections['info']    ?? {}; }
  get details(): any { return this.formSections['details'] ?? {}; }

  get typeLabel(): string {
    switch (this.details.assessmentType) {
      case AssessmentType.Formative:  return 'Formative';
      case AssessmentType.Summative:  return 'Summative';
      case AssessmentType.Competency: return 'Competency';
      default: return '—';
    }
  }

  // ── Lookup resolution ──────────────────────────────────────────
  getClassName():        string { return this.classes.find(c  => c.id === this.info.classId)?.name         ?? ''; }
  getSubjectName():      string { return this.subjects.find(s => s.id === this.info.subjectId)?.name       ?? ''; }
  getTermName():         string { return this.terms.find(t    => t.id === this.info.termId)?.name          ?? ''; }
  getAcademicYearName(): string { return this.academicYears.find(y => y.id === this.info.academicYearId)?.name ?? ''; }
  getSchoolName():       string { return this.schools.find(s  => s.id === this.info.schoolId)?.name        ?? ''; }

  getTeacherName(): string {
    const t = this.teachers.find(t => t.id === this.info.teacherId);
    return t ? `${t.firstName} ${t.lastName}`.trim() : '';
  }

  // ── Enum → label resolvers (no debug suffix) ───────────────────

  /** AssessmentMethod: 0–5 → readable string */
  assessmentMethodLabel(value: number | string): string {
    const MAP: Record<number, string> = {
      0: 'Observation', 1: 'Written',   2: 'Oral',
      3: 'Practical',   4: 'Portfolio', 5: 'Peer Assessment',
    };
    return MAP[Number(value)] ?? String(value);
  }

  /** CBCLevel: 1–11 → readable grade label */
  targetLevelLabel(value: number | string): string {
    const MAP: Record<number, string> = {
      1: 'PP1',    2: 'PP2',    3: 'Grade 1', 4: 'Grade 2',
      5: 'Grade 3', 6: 'Grade 4', 7: 'Grade 5', 8: 'Grade 6',
      9: 'Grade 7 (JSS1)', 10: 'Grade 8 (JSS2)', 11: 'Grade 9 (JSS3)',
    };
    return MAP[Number(value)] ?? String(value);
  }

  // ── Validation ─────────────────────────────────────────────────
  allValid(): boolean {
    const infoOk     = !!(this.info.title?.trim() && this.info.assessmentDate);
    const detailsOk  = !!(this.details.assessmentType && this.details.maximumScore);
    const competencyOk = this.details.assessmentType !== AssessmentType.Competency
      || !!this.details.competencyName;
    return infoOk && detailsOk && competencyOk;
  }
}