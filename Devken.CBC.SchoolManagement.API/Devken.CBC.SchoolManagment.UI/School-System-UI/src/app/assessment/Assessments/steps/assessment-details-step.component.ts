// ═══════════════════════════════════════════════════════════════════
// steps/assessment-details-step.component.ts
// Includes ALL type-specific fields as per GET /api/assessments/schema/{type}
// ═══════════════════════════════════════════════════════════════════

import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-assessment-details-step',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  template: `
<div class="max-w-3xl mx-auto">
  <div class="mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Assessment Details</h2>
    <p class="text-gray-500 dark:text-gray-400 mt-1">Set the type, scoring and advanced configuration.</p>
  </div>

  <!-- ── Type Selection ─────────────────────────────────────────── -->
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 sm:p-8 mb-6">
    <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4 uppercase tracking-wide">
      Assessment Type <span class="text-red-500">*</span>
    </h3>
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
      <button *ngFor="let type of assessmentTypes" type="button"
        (click)="data.assessmentType = type; onChange()"
        class="relative flex flex-col items-start gap-2 px-5 py-5 rounded-xl border-2 transition-all text-left"
        [ngClass]="{
          'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 shadow-md': data.assessmentType === type,
          'border-gray-200 dark:border-gray-600 hover:border-indigo-300 hover:bg-gray-50 dark:hover:bg-gray-700/30': data.assessmentType !== type
        }">
        <div class="flex items-center gap-2 w-full">
          <mat-icon class="icon-size-5 flex-shrink-0"
            [ngClass]="{
              'text-indigo-600 dark:text-indigo-400': data.assessmentType === type,
              'text-gray-400': data.assessmentType !== type
            }">
            <ng-container *ngIf="type === 'Formative'">edit_note</ng-container>
            <ng-container *ngIf="type === 'Summative'">fact_check</ng-container>
            <ng-container *ngIf="type === 'Competency'">verified_user</ng-container>
          </mat-icon>
          <span class="text-sm font-bold"
            [class.text-indigo-700]="data.assessmentType === type"
            [class.dark:text-indigo-300]="data.assessmentType === type"
            [class.text-gray-700]="data.assessmentType !== type"
            [class.dark:text-gray-300]="data.assessmentType !== type">
            {{ type }}
          </span>
          <div *ngIf="data.assessmentType === type" class="ml-auto w-5 h-5 rounded-full bg-indigo-600 flex items-center justify-center flex-shrink-0">
            <mat-icon class="!w-3 !h-3 !text-xs text-white">check</mat-icon>
          </div>
        </div>
        <span class="text-xs pl-7"
          [class.text-indigo-500]="data.assessmentType === type"
          [class.dark:text-indigo-400]="data.assessmentType === type"
          [class.text-gray-400]="data.assessmentType !== type">
          <ng-container *ngIf="type === 'Formative'">Ongoing classroom assessments</ng-container>
          <ng-container *ngIf="type === 'Summative'">End-of-period evaluations</ng-container>
          <ng-container *ngIf="type === 'Competency'">CBC skill-based assessment</ng-container>
        </span>
      </button>
    </div>
  </div>

  <!-- ── Shared Scoring ─────────────────────────────────────────── -->
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 sm:p-8 mb-6">
    <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4 uppercase tracking-wide">Scoring</h3>
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
          Maximum Score <span class="text-red-500">*</span>
        </label>
        <input type="number" [(ngModel)]="data.maximumScore" (ngModelChange)="onChange()"
          min="0.01" max="9999.99" step="0.01" placeholder="100"
          class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600
                 bg-white dark:bg-gray-700 text-gray-900 dark:text-white
                 focus:ring-2 focus:ring-indigo-500 outline-none transition"
          [class.border-red-400]="touched && !data.maximumScore" />
        <p *ngIf="touched && !data.maximumScore" class="text-red-500 text-xs mt-1">Maximum score is required (0.01–9999.99)</p>
      </div>
    </div>
  </div>

  <!-- ── Formative-specific Fields ─────────────────────────────── -->
  <ng-container *ngIf="data.assessmentType === 'Formative'">
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-indigo-200 dark:border-indigo-800 p-6 sm:p-8 mb-6">
      <div class="flex items-center gap-2 mb-5">
        <div class="w-7 h-7 rounded-lg bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center">
          <mat-icon class="text-indigo-600 dark:text-indigo-400 icon-size-4">edit_note</mat-icon>
        </div>
        <h3 class="text-sm font-semibold text-indigo-700 dark:text-indigo-300 uppercase tracking-wide">Formative Options</h3>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <!-- Formative Type -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Formative Type</label>
          <select [(ngModel)]="data.formativeType" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition">
            <option value="">Select type</option>
            <option value="Quiz">Quiz</option>
            <option value="Homework">Homework</option>
            <option value="ClassActivity">Class Activity</option>
            <option value="Project">Project</option>
            <option value="Observation">Observation</option>
            <option value="Portfolio">Portfolio</option>
            <option value="PeerAssessment">Peer Assessment</option>
          </select>
        </div>
        <!-- Competency Area -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Competency Area</label>
          <input type="text" [(ngModel)]="data.competencyArea" (ngModelChange)="onChange()"
            placeholder="e.g. Communication"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition" />
        </div>
        <!-- Strand -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Strand</label>
          <select [(ngModel)]="data.strandId" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition">
            <option value="">Select strand</option>
            <option *ngFor="let s of strands" [value]="s.id">{{ s.name }}</option>
          </select>
        </div>
        <!-- Sub-Strand -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Sub-Strand</label>
          <select [(ngModel)]="data.subStrandId" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition">
            <option value="">Select sub-strand</option>
            <option *ngFor="let s of subStrands" [value]="s.id">{{ s.name }}</option>
          </select>
        </div>
        <!-- Learning Outcome -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Learning Outcome</label>
          <select [(ngModel)]="data.learningOutcomeId" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition">
            <option value="">Select learning outcome</option>
            <option *ngFor="let l of learningOutcomes" [value]="l.id">{{ l.name }}</option>
          </select>
        </div>
        <!-- Criteria -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Assessment Criteria</label>
          <textarea [(ngModel)]="data.criteria" (ngModelChange)="onChange()" rows="2"
            placeholder="Describe the assessment criteria…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition resize-none">
          </textarea>
        </div>
        <!-- Feedback Template -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Feedback Template</label>
          <textarea [(ngModel)]="data.feedbackTemplate" (ngModelChange)="onChange()" rows="2"
            placeholder="Template for student feedback…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition resize-none">
          </textarea>
        </div>
        <!-- Assessment Weight + Requires Rubric -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Weight (%)</label>
          <input type="number" [(ngModel)]="data.assessmentWeight" (ngModelChange)="onChange()"
            min="0" max="100" placeholder="100"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition" />
        </div>
        <div class="flex items-center gap-3 self-end pb-3">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" [(ngModel)]="data.requiresRubric" (ngModelChange)="onChange()" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 dark:peer-focus:ring-indigo-800 rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all dark:border-gray-600 peer-checked:bg-indigo-600"></div>
            <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Requires Rubric</span>
          </label>
        </div>
        <!-- Instructions -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Instructions</label>
          <textarea [(ngModel)]="data.formativeInstructions" (ngModelChange)="onChange()" rows="3"
            placeholder="Assessment instructions for teachers / students…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-indigo-500 outline-none transition resize-none">
          </textarea>
        </div>
      </div>
    </div>
  </ng-container>

  <!-- ── Summative-specific Fields ─────────────────────────────── -->
  <ng-container *ngIf="data.assessmentType === 'Summative'">
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-violet-200 dark:border-violet-800 p-6 sm:p-8 mb-6">
      <div class="flex items-center gap-2 mb-5">
        <div class="w-7 h-7 rounded-lg bg-violet-100 dark:bg-violet-900/40 flex items-center justify-center">
          <mat-icon class="text-violet-600 dark:text-violet-400 icon-size-4">fact_check</mat-icon>
        </div>
        <h3 class="text-sm font-semibold text-violet-700 dark:text-violet-300 uppercase tracking-wide">Summative Options</h3>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <!-- Exam Type -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Exam Type</label>
          <select [(ngModel)]="data.examType" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition">
            <option value="">Select exam type</option>
            <option value="MidTerm">Mid-Term</option>
            <option value="EndTerm">End-Term</option>
            <option value="Mock">Mock</option>
            <option value="CAT">CAT</option>
            <option value="Final">Final</option>
          </select>
        </div>
        <!-- Duration -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Duration (HH:mm:ss)</label>
          <input type="text" [(ngModel)]="data.duration" (ngModelChange)="onChange()"
            placeholder="02:00:00"
            pattern="[0-9]{2}:[0-9]{2}:[0-9]{2}"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition" />
        </div>
        <!-- Number of Questions -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Number of Questions</label>
          <input type="number" [(ngModel)]="data.numberOfQuestions" (ngModelChange)="onChange()"
            min="0" placeholder="50"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition" />
        </div>
        <!-- Pass Mark -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Pass Mark (%)</label>
          <input type="number" [(ngModel)]="data.passMark" (ngModelChange)="onChange()"
            min="0" max="100" placeholder="50"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition" />
        </div>
        <!-- Theory Weight -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Theory Weight (%)</label>
          <input type="number" [(ngModel)]="data.theoryWeight" (ngModelChange)="onChange()"
            min="0" max="100" placeholder="100"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition" />
        </div>
        <!-- Practical Weight -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Practical Weight (%)</label>
          <input type="number" [(ngModel)]="data.practicalWeight" (ngModelChange)="onChange()"
            min="0" max="100" placeholder="0"
            [disabled]="!data.hasPracticalComponent"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition disabled:opacity-50" />
        </div>
        <!-- Has Practical -->
        <div class="flex items-center gap-3 self-center">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" [(ngModel)]="data.hasPracticalComponent" (ngModelChange)="onChange()" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-violet-300 dark:peer-focus:ring-violet-800 rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-violet-600"></div>
            <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Has Practical Component</span>
          </label>
        </div>
        <!-- Instructions -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Instructions</label>
          <textarea [(ngModel)]="data.summativeInstructions" (ngModelChange)="onChange()" rows="3"
            placeholder="Exam instructions for students…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-violet-500 outline-none transition resize-none">
          </textarea>
        </div>
      </div>
    </div>
  </ng-container>

  <!-- ── Competency-specific Fields ────────────────────────────── -->
  <ng-container *ngIf="data.assessmentType === 'Competency'">
    <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-teal-200 dark:border-teal-800 p-6 sm:p-8 mb-6">
      <div class="flex items-center gap-2 mb-5">
        <div class="w-7 h-7 rounded-lg bg-teal-100 dark:bg-teal-900/40 flex items-center justify-center">
          <mat-icon class="text-teal-600 dark:text-teal-400 icon-size-4">verified_user</mat-icon>
        </div>
        <h3 class="text-sm font-semibold text-teal-700 dark:text-teal-300 uppercase tracking-wide">Competency Options</h3>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <!-- Competency Name -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Competency Name <span class="text-red-500">*</span>
          </label>
          <input type="text" [(ngModel)]="data.competencyName" (ngModelChange)="onChange()"
            placeholder="e.g. Numeracy and Mathematical Thinking"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition"
            [class.border-red-400]="touched && data.assessmentType === 'Competency' && !data.competencyName" />
          <p *ngIf="touched && data.assessmentType === 'Competency' && !data.competencyName" class="text-red-500 text-xs mt-1">Competency name is required</p>
        </div>
        <!-- Competency Strand -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Strand</label>
          <input type="text" [(ngModel)]="data.competencyStrand" (ngModelChange)="onChange()"
            placeholder="e.g. Number"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition" />
        </div>
        <!-- Competency Sub-Strand -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Sub-Strand</label>
          <input type="text" [(ngModel)]="data.competencySubStrand" (ngModelChange)="onChange()"
            placeholder="e.g. Counting"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition" />
        </div>
        <!-- CBC Target Level -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">CBC Level</label>
          <select [(ngModel)]="data.targetLevel" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition">
            <option value="">Select level</option>
            <option value="PP1">PP1</option>
            <option value="PP2">PP2</option>
            <option value="Grade1">Grade 1</option>
            <option value="Grade2">Grade 2</option>
            <option value="Grade3">Grade 3</option>
            <option value="Grade4">Grade 4</option>
            <option value="Grade5">Grade 5</option>
            <option value="Grade6">Grade 6</option>
            <option value="Grade7">Grade 7 (JSS1)</option>
            <option value="Grade8">Grade 8 (JSS2)</option>
            <option value="Grade9">Grade 9 (JSS3)</option>
          </select>
        </div>
        <!-- Assessment Method -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Assessment Method</label>
          <select [(ngModel)]="data.assessmentMethod" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition">
            <option value="">Select method</option>
            <option [value]="0">Observation</option>
            <option [value]="1">Written</option>
            <option [value]="2">Oral</option>
            <option [value]="3">Practical</option>
            <option [value]="4">Portfolio</option>
            <option [value]="5">Peer Assessment</option>
          </select>
        </div>
        <!-- Rating Scale -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Rating Scale</label>
          <select [(ngModel)]="data.ratingScale" (ngModelChange)="onChange()"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition">
            <option value="">Select scale</option>
            <option value="EE-ME-AE-BE">EE / ME / AE / BE (CBC Standard)</option>
            <option value="1-4">1 – 4</option>
            <option value="1-5">1 – 5</option>
            <option value="Pass/Fail">Pass / Fail</option>
          </select>
        </div>
        <!-- Observation Based -->
        <div class="flex items-center gap-3 self-center">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" [(ngModel)]="data.isObservationBased" (ngModelChange)="onChange()" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-teal-300 dark:peer-focus:ring-teal-800 rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-teal-600"></div>
            <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Observation Based</span>
          </label>
        </div>
        <!-- Performance Indicators -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Performance Indicators</label>
          <textarea [(ngModel)]="data.performanceIndicators" (ngModelChange)="onChange()" rows="2"
            placeholder="Key indicators for this competency…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition resize-none">
          </textarea>
        </div>
        <!-- Tools Required -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Tools Required</label>
          <input type="text" [(ngModel)]="data.toolsRequired" (ngModelChange)="onChange()"
            placeholder="e.g. Ruler, Calculator, Protractor"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition" />
        </div>
        <!-- Specific Learning Outcome -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Specific Learning Outcome</label>
          <textarea [(ngModel)]="data.specificLearningOutcome" (ngModelChange)="onChange()" rows="2"
            placeholder="Describe the specific learning outcome…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition resize-none">
          </textarea>
        </div>
        <!-- Instructions -->
        <div class="sm:col-span-2">
          <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Instructions</label>
          <textarea [(ngModel)]="data.competencyInstructions" (ngModelChange)="onChange()" rows="3"
            placeholder="Assessment instructions…"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-teal-500 outline-none transition resize-none">
          </textarea>
        </div>
      </div>
    </div>
  </ng-container>

  <!-- ── Summary Preview ────────────────────────────────────────── -->
  <div class="p-5 rounded-xl border flex items-start gap-3"
    [ngClass]="{
      'bg-indigo-50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-800': data.assessmentType === 'Formative',
      'bg-violet-50 dark:bg-violet-900/20 border-violet-200 dark:border-violet-800': data.assessmentType === 'Summative',
      'bg-teal-50   dark:bg-teal-900/20   border-teal-200   dark:border-teal-800':   data.assessmentType === 'Competency'
    }">
    <mat-icon class="flex-shrink-0 mt-0.5"
      [ngClass]="{
        'text-indigo-600': data.assessmentType === 'Formative',
        'text-violet-600': data.assessmentType === 'Summative',
        'text-teal-600':   data.assessmentType === 'Competency'
      }">info_outline</mat-icon>
    <p class="text-sm text-gray-700 dark:text-gray-300">
      <span class="font-semibold">{{ data.assessmentType || '—' }}</span> assessment ·
      Max score: <span class="font-semibold">{{ data.maximumScore || '—' }}</span>
      <ng-container *ngIf="data.assessmentType === 'Summative' && data.passMark">
        · Pass mark: <span class="font-semibold">{{ data.passMark }}%</span>
      </ng-container>
      <ng-container *ngIf="data.assessmentType === 'Competency' && data.competencyName">
        · Competency: <span class="font-semibold">{{ data.competencyName }}</span>
      </ng-container>
    </p>
  </div>

</div>
  `,
})
export class AssessmentDetailsStepComponent implements OnInit, OnChanges {
  @Input() formData:          any = {};
  @Input() assessmentTypes:   readonly string[] = ['Formative', 'Summative', 'Competency'];
  @Input() isEditMode         = false;
  @Input() strands:           any[] = [];
  @Input() subStrands:        any[] = [];
  @Input() learningOutcomes:  any[] = [];

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  data: any = { assessmentType: 'Formative', maximumScore: 100, assessmentWeight: 100, theoryWeight: 100, passMark: 50, isObservationBased: true };
  touched = false;

  ngOnInit():                   void { this.data = { ...this.data, ...this.formData }; this.emitValid(); }
  ngOnChanges(c: SimpleChanges): void { if (c['formData']) { this.data = { ...this.data, ...this.formData }; this.emitValid(); } }

  onChange(): void {
    this.touched = true;
    this.formChanged.emit({ ...this.data });
    this.emitValid();
  }

  private emitValid(): void {
    const hasBase = !!(this.data.assessmentType && this.data.maximumScore);
    const competencyOk = this.data.assessmentType !== 'Competency' || !!this.data.competencyName;
    this.formValid.emit(hasBase && competencyOk);
  }
}