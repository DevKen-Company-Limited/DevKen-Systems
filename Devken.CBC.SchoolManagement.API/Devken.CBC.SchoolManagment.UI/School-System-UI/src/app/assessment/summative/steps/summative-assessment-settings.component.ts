// ═══════════════════════════════════════════════════════════════════
// steps/summative-assessment-settings.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';

@Component({
  selector: 'app-summative-assessment-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Exam Settings</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Configure pass marks, weightings and exam logistics.</p>
  </div>

  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 sm:p-8 space-y-6">

    <!-- Pass Mark + Questions -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          Pass Mark (%) <span class="text-red-500">*</span>
        </label>
        <input type="number" [(ngModel)]="data.passMark" (ngModelChange)="onChange()"
          min="0" max="100" placeholder="50"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-violet-500 outline-none transition" />
        <p class="text-xs text-gray-400 mt-1">Minimum percentage to pass</p>
      </div>

      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Number of Questions</label>
        <input type="number" [(ngModel)]="data.numberOfQuestions" (ngModelChange)="onChange()"
          min="0" placeholder="0"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-violet-500 outline-none transition" />
      </div>
    </div>

    <!-- Duration -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Duration (HH:MM:SS)</label>
        <input type="text" [(ngModel)]="data.duration" (ngModelChange)="onChange()"
          placeholder="02:30:00"
          pattern="^([0-9]{2}):([0-5][0-9]):([0-5][0-9])$"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-violet-500 outline-none transition" />
        <p class="text-xs text-gray-400 mt-1">Leave empty if not time-limited</p>
      </div>
    </div>

    <hr class="border-gray-200 dark:border-gray-700">

    <!-- Practical Component toggle -->
    <div class="flex items-start gap-4 p-4 rounded-xl bg-teal-50 dark:bg-teal-900/20
                border border-teal-200 dark:border-teal-800">
      <div class="flex items-center h-5 mt-1">
        <input type="checkbox" [(ngModel)]="data.hasPracticalComponent" (ngModelChange)="onPracticalToggle()"
          id="hasPractical"
          class="w-4 h-4 rounded text-teal-600 border-gray-300 focus:ring-teal-500" />
      </div>
      <div>
        <label for="hasPractical" class="text-sm font-semibold text-gray-900 dark:text-white cursor-pointer">
          Has Practical Component
        </label>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
          Enable if this exam has a separate practical/lab component with its own score.
        </p>
      </div>
    </div>

    <!-- Weight sliders (shown when practical is enabled) -->
    <div *ngIf="data.hasPracticalComponent" class="space-y-4 p-4 bg-gray-50 dark:bg-gray-700/30 rounded-xl">
      <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Score Weightings</h3>
      <p class="text-xs text-gray-400">Theory + Practical must total 100%</p>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            Theory Weight: <strong class="text-violet-600">{{ data.theoryWeight }}%</strong>
          </label>
          <input type="range" [(ngModel)]="data.theoryWeight" (ngModelChange)="onTheoryWeightChange()"
            min="0" max="100" step="5"
            class="w-full accent-violet-600" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            Practical Weight: <strong class="text-teal-600">{{ data.practicalWeight }}%</strong>
          </label>
          <input type="range" [(ngModel)]="data.practicalWeight" (ngModelChange)="onPracticalWeightChange()"
            min="0" max="100" step="5"
            class="w-full accent-teal-600" />
        </div>
      </div>

      <div class="flex gap-3 mt-2">
        <div class="flex-1 h-3 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-600">
          <div class="h-full bg-violet-500 rounded-full transition-all" [style.width.%]="data.theoryWeight"></div>
        </div>
        <div class="flex-1 h-3 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-600">
          <div class="h-full bg-teal-500 rounded-full transition-all" [style.width.%]="data.practicalWeight"></div>
        </div>
      </div>

      <p *ngIf="weightsInvalid" class="text-red-500 text-xs">Theory + Practical weights must equal 100%</p>
    </div>

    <hr class="border-gray-200 dark:border-gray-700">

    <!-- Instructions -->
    <div>
      <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Instructions</label>
      <textarea [(ngModel)]="data.instructions" (ngModelChange)="onChange()"
        rows="4"
        placeholder="Enter exam instructions for students and invigilators..."
        class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
               bg-white dark:bg-gray-700 text-gray-900 dark:text-white
               focus:ring-2 focus:ring-violet-500 outline-none transition resize-none">
      </textarea>
    </div>

  </div>
</div>
  `,
})
export class SummativeAssessmentSettingsComponent implements OnInit, OnChanges {
  @Input() formData:  any = {};
  @Input() isEditMode = false;

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  data: any = { passMark: 50, theoryWeight: 100, practicalWeight: 0, hasPracticalComponent: false, numberOfQuestions: 0 };

  get weightsInvalid(): boolean {
    return this.data.hasPracticalComponent &&
           (+this.data.theoryWeight + +this.data.practicalWeight) !== 100;
  }

  ngOnInit():                    void { this.data = { ...this.data, ...this.formData }; this.emitValid(); }
  ngOnChanges(c: SimpleChanges): void { if (c['formData']) { this.data = { ...this.data, ...this.formData }; this.emitValid(); } }

  onChange(): void { this.formChanged.emit({ ...this.data }); this.emitValid(); }

  onPracticalToggle(): void {
    if (!this.data.hasPracticalComponent) {
      this.data.theoryWeight    = 100;
      this.data.practicalWeight = 0;
    } else {
      this.data.theoryWeight    = 70;
      this.data.practicalWeight = 30;
    }
    this.onChange();
  }

  onTheoryWeightChange():    void { this.data.practicalWeight = 100 - this.data.theoryWeight;    this.onChange(); }
  onPracticalWeightChange(): void { this.data.theoryWeight    = 100 - this.data.practicalWeight; this.onChange(); }

  private emitValid(): void {
    this.formValid.emit(!this.weightsInvalid && !!this.data.passMark);
  }
}


// ═══════════════════════════════════════════════════════════════════
// steps/summative-assessment-review.component.ts
// ═══════════════════════════════════════════════════════════════════

import { Component as ReviewComponent, Input as ReviewInput, Output as ReviewOutput, EventEmitter as ReviewEventEmitter } from '@angular/core';
import { CommonModule as ReviewCommonModule } from '@angular/common';
import { MatIconModule as ReviewMatIconModule } from '@angular/material/icon';
import { MatButtonModule as ReviewMatButtonModule } from '@angular/material/button';

@ReviewComponent({
  selector: 'app-summative-assessment-review',
  standalone: true,
  imports: [ReviewCommonModule, ReviewMatIconModule, ReviewMatButtonModule],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Review & Save</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Confirm all details before creating this assessment.</p>
  </div>

  <!-- Review cards -->
  <div class="space-y-4">

    <!-- Basic Info card -->
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-full bg-violet-100 dark:bg-violet-900/30 flex items-center justify-center">
            <mat-icon class="text-violet-600 icon-size-4">info</mat-icon>
          </div>
          <h3 class="font-semibold text-gray-900 dark:text-white">Basic Information</h3>
        </div>
        <button mat-stroked-button (click)="editSection.emit(0)" class="text-sm">Edit</button>
      </div>
      <div class="p-6 grid grid-cols-2 gap-4">
        <div><p class="text-xs text-gray-400">Title</p><p class="font-medium text-gray-900 dark:text-white">{{ info.title || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Exam Type</p><p class="font-medium text-gray-900 dark:text-white">{{ info.examType || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Date</p><p class="font-medium text-gray-900 dark:text-white">{{ info.assessmentDate | date:'mediumDate' }}</p></div>
        <div><p class="text-xs text-gray-400">Max Score</p><p class="font-medium text-gray-900 dark:text-white">{{ info.maximumScore || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Class</p><p class="font-medium text-gray-900 dark:text-white">{{ getClassName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Subject</p><p class="font-medium text-gray-900 dark:text-white">{{ getSubjectName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Teacher</p><p class="font-medium text-gray-900 dark:text-white">{{ getTeacherName() || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Term</p><p class="font-medium text-gray-900 dark:text-white">{{ getTermName() || '—' }}</p></div>
      </div>
    </div>

    <!-- Settings card -->
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-full bg-teal-100 dark:bg-teal-900/30 flex items-center justify-center">
            <mat-icon class="text-teal-600 icon-size-4">settings</mat-icon>
          </div>
          <h3 class="font-semibold text-gray-900 dark:text-white">Exam Settings</h3>
        </div>
        <button mat-stroked-button (click)="editSection.emit(1)" class="text-sm">Edit</button>
      </div>
      <div class="p-6 grid grid-cols-2 gap-4">
        <div><p class="text-xs text-gray-400">Pass Mark</p><p class="font-medium text-gray-900 dark:text-white">{{ settings.passMark }}%</p></div>
        <div><p class="text-xs text-gray-400">No. of Questions</p><p class="font-medium text-gray-900 dark:text-white">{{ settings.numberOfQuestions || '—' }}</p></div>
        <div><p class="text-xs text-gray-400">Duration</p><p class="font-medium text-gray-900 dark:text-white">{{ settings.duration || 'No limit' }}</p></div>
        <div><p class="text-xs text-gray-400">Practical</p>
          <p class="font-medium" [class.text-teal-600]="settings.hasPracticalComponent" [class.text-gray-400]="!settings.hasPracticalComponent">
            {{ settings.hasPracticalComponent ? 'Yes — T:' + settings.theoryWeight + '% P:' + settings.practicalWeight + '%' : 'No' }}
          </p>
        </div>
      </div>
      <div *ngIf="settings.instructions" class="px-6 pb-6">
        <p class="text-xs text-gray-400 mb-1">Instructions</p>
        <p class="text-sm text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-700 rounded-lg p-3">{{ settings.instructions }}</p>
      </div>
    </div>

    <!-- Completion indicator -->
    <div class="flex items-center gap-3 p-4 bg-green-50 dark:bg-green-900/20
                border border-green-200 dark:border-green-800 rounded-xl">
      <mat-icon class="text-green-600">check_circle</mat-icon>
      <p class="text-sm text-green-700 dark:text-green-400 font-medium">
        All required fields are filled. Ready to save.
      </p>
    </div>

  </div>
</div>
  `,
})
export class SummativeAssessmentReviewComponent {
  @ReviewInput() formSections:  Record<string, any> = {};
  @ReviewInput() classes:       any[] = [];
  @ReviewInput() teachers:      any[] = [];
  @ReviewInput() subjects:      any[] = [];
  @ReviewInput() terms:         any[] = [];
  @ReviewInput() academicYears: any[] = [];
  @ReviewInput() steps:         any[] = [];
  @ReviewInput() completedSteps!: Set<number>;

  @ReviewOutput() editSection = new ReviewEventEmitter<number>();

  get info():     any { return this.formSections['info']     ?? {}; }
  get settings(): any { return this.formSections['settings'] ?? {}; }

  getClassName():   string { return this.classes.find(c => c.id === this.info.classId)?.name       ?? ''; }
  getSubjectName(): string { return this.subjects.find(s => s.id === this.info.subjectId)?.name    ?? ''; }
  getTeacherName(): string { const t = this.teachers.find(t => t.id === this.info.teacherId); return t ? `${t.firstName} ${t.lastName}` : ''; }
  getTermName():    string { return this.terms.find(t => t.id === this.info.termId)?.name          ?? ''; }
}