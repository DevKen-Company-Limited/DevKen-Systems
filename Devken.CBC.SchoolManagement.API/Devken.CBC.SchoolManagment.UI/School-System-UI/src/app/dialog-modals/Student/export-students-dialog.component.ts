import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface ExportColumn {
  id: string;
  label: string;
  selected: boolean;
  required?: boolean;
  description?: string;
}

export interface ExportConfig {
  format: 'excel' | 'pdf' | 'word';
  columns: ExportColumn[];
  includePhoto: boolean;
  includeSchoolHeader: boolean;
}

@Component({
  selector: 'app-export-students-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatRadioModule,
    MatTooltipModule,
  ],
  template: `
    <div class="flex flex-col max-h-[85vh]">
      <!-- Header -->
      <div class="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
        <div class="flex items-center gap-3">
          <div class="flex items-center justify-center w-10 h-10 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600">
            <mat-icon class="text-white icon-size-6">download</mat-icon>
          </div>
          <div>
            <h2 class="text-xl font-bold text-gray-900 dark:text-white">Export Students</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400">Choose format and columns to export</p>
          </div>
        </div>
        <button mat-icon-button (click)="dialogRef.close()" class="text-gray-400 hover:text-gray-600">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-6 space-y-6">
        
        <!-- Export Format Selection -->
        <div class="space-y-3">
          <label class="text-sm font-semibold text-gray-700 dark:text-gray-300">Export Format</label>
          <mat-radio-group [(ngModel)]="exportConfig.format" class="flex flex-col gap-3">
            <mat-radio-button value="excel" class="export-format-option">
              <div class="flex items-center gap-3 py-2">
                <div class="flex items-center justify-center w-12 h-12 rounded-lg bg-green-100 dark:bg-green-900/30">
                  <mat-icon class="text-green-600 dark:text-green-400 icon-size-7">table_chart</mat-icon>
                </div>
                <div>
                  <div class="font-medium text-gray-900 dark:text-white">Microsoft Excel (.xlsx)</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400">Best for data analysis and filtering</div>
                </div>
              </div>
            </mat-radio-button>
            
            <mat-radio-button value="pdf" class="export-format-option">
              <div class="flex items-center gap-3 py-2">
                <div class="flex items-center justify-center w-12 h-12 rounded-lg bg-red-100 dark:bg-red-900/30">
                  <mat-icon class="text-red-600 dark:text-red-400 icon-size-7">picture_as_pdf</mat-icon>
                </div>
                <div>
                  <div class="font-medium text-gray-900 dark:text-white">PDF Document (.pdf)</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400">Professional report with photos and formatting</div>
                </div>
              </div>
            </mat-radio-button>
            
            <mat-radio-button value="word" class="export-format-option">
              <div class="flex items-center gap-3 py-2">
                <div class="flex items-center justify-center w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900/30">
                  <mat-icon class="text-blue-600 dark:text-blue-400 icon-size-7">description</mat-icon>
                </div>
                <div>
                  <div class="font-medium text-gray-900 dark:text-white">Microsoft Word (.docx)</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400">Editable document with full formatting</div>
                </div>
              </div>
            </mat-radio-button>
          </mat-radio-group>
        </div>

        <!-- Additional Options -->
        <div class="space-y-3 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
          <label class="text-sm font-semibold text-gray-700 dark:text-gray-300">Document Options</label>
          
          <mat-checkbox 
            [(ngModel)]="exportConfig.includeSchoolHeader"
            class="block">
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium">Include School Header</span>
              <mat-icon 
                class="icon-size-4 text-gray-400" 
                matTooltip="Add school logo, name, and contact details at the top">
                info
              </mat-icon>
            </div>
          </mat-checkbox>
          
          <mat-checkbox 
            [(ngModel)]="exportConfig.includePhoto"
            [disabled]="exportConfig.format === 'excel'"
            class="block">
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium">Include Student Photos</span>
              <mat-icon 
                class="icon-size-4 text-gray-400" 
                [matTooltip]="exportConfig.format === 'excel' ? 'Photos not supported in Excel format' : 'Include student photos in the document'">
                info
              </mat-icon>
            </div>
          </mat-checkbox>
        </div>

        <!-- Column Selection -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <label class="text-sm font-semibold text-gray-700 dark:text-gray-300">
              Select Columns to Export
            </label>
            <div class="flex gap-2">
              <button 
                mat-stroked-button 
                (click)="selectAllColumns()"
                class="text-xs h-8">
                Select All
              </button>
              <button 
                mat-stroked-button 
                (click)="deselectAllColumns()"
                class="text-xs h-8">
                Clear All
              </button>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-3 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg max-h-80 overflow-y-auto">
            <mat-checkbox
              *ngFor="let column of exportConfig.columns"
              [(ngModel)]="column.selected"
              [disabled]="column.required"
              class="export-column-checkbox">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium">{{ column.label }}</span>
                <mat-icon 
                  *ngIf="column.required" 
                  class="icon-size-4 text-indigo-500"
                  matTooltip="Required column">
                  lock
                </mat-icon>
                <mat-icon 
                  *ngIf="column.description" 
                  class="icon-size-4 text-gray-400"
                  [matTooltip]="column.description">
                  info
                </mat-icon>
              </div>
            </mat-checkbox>
          </div>

          <div class="text-xs text-gray-500 dark:text-gray-400 flex items-center gap-1">
            <mat-icon class="icon-size-4">info</mat-icon>
            <span>{{ selectedColumnsCount }} of {{ exportConfig.columns.length }} columns selected</span>
          </div>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="flex items-center justify-between gap-3 p-6 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
        <div class="text-sm text-gray-600 dark:text-gray-400">
          <mat-icon class="icon-size-4 inline-block mr-1">info</mat-icon>
          Export will include {{ selectedColumnsCount }} columns
        </div>
        <div class="flex gap-3">
          <button 
            mat-stroked-button 
            (click)="dialogRef.close()"
            class="border-gray-300 dark:border-gray-600">
            Cancel
          </button>
          <button 
            mat-flat-button 
            (click)="export()"
            [disabled]="selectedColumnsCount === 0"
            class="bg-gradient-to-r from-indigo-600 to-violet-600 text-white hover:from-indigo-700 hover:to-violet-700">
            <mat-icon class="icon-size-5">download</mat-icon>
            <span class="ml-2">Export {{ getFormatLabel() }}</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    ::ng-deep .export-format-option .mdc-radio {
      margin-right: 12px;
    }

    ::ng-deep .export-format-option .mdc-form-field {
      width: 100%;
    }

    ::ng-deep .export-column-checkbox .mdc-checkbox {
      margin-right: 8px;
    }

    .export-format-option:hover {
      background-color: rgba(99, 102, 241, 0.05);
      border-radius: 8px;
    }
  `]
})
export class ExportStudentsDialogComponent implements OnInit {
  dialogRef = inject(MatDialogRef<ExportStudentsDialogComponent>);

  exportConfig: ExportConfig = {
    format: 'excel',
    includePhoto: true,
    includeSchoolHeader: true,
    columns: [
      { id: 'photo', label: 'Photo', selected: true, description: 'Student photo (PDF/Word only)' },
      { id: 'fullName', label: 'Full Name', selected: true, required: true },
      { id: 'admissionNumber', label: 'Admission Number', selected: true, required: true },
      { id: 'nemisNumber', label: 'NEMIS Number', selected: true },
      { id: 'gender', label: 'Gender', selected: true },
      { id: 'dateOfBirth', label: 'Date of Birth', selected: true },
      { id: 'cbcLevel', label: 'CBC Level', selected: true },
      { id: 'currentLevel', label: 'Current Level', selected: true },
      { id: 'studentStatus', label: 'Student Status', selected: true },
      { id: 'primaryGuardianName', label: 'Guardian Name', selected: true },
      { id: 'primaryGuardianRelationship', label: 'Guardian Relationship', selected: false },
      { id: 'primaryGuardianPhone', label: 'Guardian Phone', selected: true },
      { id: 'primaryGuardianEmail', label: 'Guardian Email', selected: false },
      { id: 'secondaryGuardianName', label: 'Secondary Guardian Name', selected: false },
      { id: 'secondaryGuardianPhone', label: 'Secondary Guardian Phone', selected: false },
      { id: 'county', label: 'County', selected: false },
      { id: 'subCounty', label: 'Sub-County', selected: false },
      { id: 'village', label: 'Village', selected: false },
      { id: 'isActive', label: 'Active Status', selected: true },
      { id: 'enrollmentDate', label: 'Enrollment Date', selected: false },
      { id: 'schoolName', label: 'School Name', selected: false, description: 'For super admin exports' },
    ]
  };

  ngOnInit(): void {
    // Auto-disable photo checkbox for Excel
    if (this.exportConfig.format === 'excel') {
      this.exportConfig.includePhoto = false;
    }
  }

  get selectedColumnsCount(): number {
    return this.exportConfig.columns.filter(col => col.selected).length;
  }

  selectAllColumns(): void {
    this.exportConfig.columns.forEach(col => {
      if (!col.required) {
        col.selected = true;
      }
    });
  }

  deselectAllColumns(): void {
    this.exportConfig.columns.forEach(col => {
      if (!col.required) {
        col.selected = false;
      }
    });
  }

  getFormatLabel(): string {
    const labels = {
      excel: 'Excel',
      pdf: 'PDF',
      word: 'Word'
    };
    return labels[this.exportConfig.format];
  }

  export(): void {
    if (this.selectedColumnsCount === 0) {
      return;
    }

    // Filter out photo column if Excel format
    if (this.exportConfig.format === 'excel') {
      const photoColumn = this.exportConfig.columns.find(col => col.id === 'photo');
      if (photoColumn) {
        photoColumn.selected = false;
      }
      this.exportConfig.includePhoto = false;
    }

    this.dialogRef.close(this.exportConfig);
  }
}