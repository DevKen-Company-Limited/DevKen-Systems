import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { StudentDto } from 'app/administration/students/types/studentdto';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError, delay } from 'rxjs/operators';

interface PhotoMatch {
  file: File;
  admissionNumber: string;
  student: StudentDto | null;
  status: 'pending' | 'uploading' | 'success' | 'error';
  errorMessage?: string;
  preview?: string;
}

interface UploadStats {
  total: number;
  matched: number;
  unmatched: number;
  uploaded: number;
  failed: number;
}

@Component({
  selector: 'app-bulk-photo-upload-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatChipsModule,
  ],
  template: `
    <div class="flex flex-col max-h-[90vh]">
      <!-- Header -->
      <div class="flex items-center justify-between p-6 border-b">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center">
            <mat-icon class="text-white icon-size-6">photo_library</mat-icon>
          </div>
          <div>
            <h2 class="text-xl font-bold text-gray-900 dark:text-white">Bulk Photo Upload</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400">Upload multiple student photos at once</p>
          </div>
        </div>
        <button mat-icon-button (click)="close()" [disabled]="isUploading">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-6">
        <!-- Step 1: Instructions & Template -->
        <div *ngIf="currentStep === 'instructions'" class="space-y-6">
          <!-- Instructions Card -->
          <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-6">
            <div class="flex items-start gap-3">
              <mat-icon class="text-blue-600 dark:text-blue-400 icon-size-6">info</mat-icon>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-3">
                  How to Prepare Your Photos
                </h3>
                <ol class="space-y-2 text-sm text-blue-800 dark:text-blue-200">
                  <li class="flex items-start gap-2">
                    <span class="font-bold">1.</span>
                    <span>Name each photo file with the student's <strong>Admission Number</strong></span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="font-bold">2.</span>
                    <span>Supported formats: JPG, JPEG, PNG, GIF, WEBP</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="font-bold">3.</span>
                    <span>Maximum file size: 5MB per photo</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="font-bold">4.</span>
                    <span>Select all photos from your folder at once</span>
                  </li>
                </ol>
              </div>
            </div>
          </div>

          <!-- Template Examples -->
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
              <mat-icon class="text-green-600 icon-size-5">folder</mat-icon>
              File Naming Examples
            </h3>
            <div class="space-y-3">
              <div class="flex items-center gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                <mat-icon class="text-green-600 dark:text-green-400">check_circle</mat-icon>
                <div class="flex-1">
                  <p class="text-sm font-mono text-green-900 dark:text-green-100">ADM001.jpg</p>
                  <p class="text-xs text-green-700 dark:text-green-300">Correct - Uses admission number</p>
                </div>
              </div>
              <div class="flex items-center gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                <mat-icon class="text-green-600 dark:text-green-400">check_circle</mat-icon>
                <div class="flex-1">
                  <p class="text-sm font-mono text-green-900 dark:text-green-100">SCH2024-0125.png</p>
                  <p class="text-xs text-green-700 dark:text-green-300">Correct - Matches admission number format</p>
                </div>
              </div>
              <div class="flex items-center gap-3 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                <mat-icon class="text-red-600 dark:text-red-400">cancel</mat-icon>
                <div class="flex-1">
                  <p class="text-sm font-mono text-red-900 dark:text-red-100">John_Doe.jpg</p>
                  <p class="text-xs text-red-700 dark:text-red-300">Incorrect - Uses student name instead</p>
                </div>
              </div>
            </div>
          </div>

          <!-- File Selection -->
          <div class="text-center">
            <input
              #fileInput
              type="file"
              multiple
              accept="image/*"
              class="hidden"
              (change)="onFilesSelected($event)"
            />
            <button
              mat-flat-button
              class="bg-gradient-to-r from-indigo-600 to-violet-600 text-white hover:from-indigo-700 hover:to-violet-700 px-8 py-6 text-lg"
              (click)="fileInput.click()">
              <mat-icon class="icon-size-6 mr-2">upload_file</mat-icon>
              Select Photos from Folder
            </button>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-3">
              You can select multiple files at once
            </p>
          </div>
        </div>

        <!-- Step 2: Matching & Preview -->
        <div *ngIf="currentStep === 'preview'" class="space-y-6">
          <!-- Stats Summary -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div class="bg-indigo-50 dark:bg-indigo-900/20 rounded-xl p-4 border border-indigo-200 dark:border-indigo-800">
              <div class="text-2xl font-bold text-indigo-900 dark:text-indigo-100">{{ stats.total }}</div>
              <div class="text-sm text-indigo-700 dark:text-indigo-300">Total Files</div>
            </div>
            <div class="bg-green-50 dark:bg-green-900/20 rounded-xl p-4 border border-green-200 dark:border-green-800">
              <div class="text-2xl font-bold text-green-900 dark:text-green-100">{{ stats.matched }}</div>
              <div class="text-sm text-green-700 dark:text-green-300">Matched</div>
            </div>
            <div class="bg-amber-50 dark:bg-amber-900/20 rounded-xl p-4 border border-amber-200 dark:border-amber-800">
              <div class="text-2xl font-bold text-amber-900 dark:text-amber-100">{{ stats.unmatched }}</div>
              <div class="text-sm text-amber-700 dark:text-amber-300">Unmatched</div>
            </div>
            <div class="bg-blue-50 dark:bg-blue-900/20 rounded-xl p-4 border border-blue-200 dark:border-blue-800">
              <div class="text-2xl font-bold text-blue-900 dark:text-blue-100">
                {{ stats.matched > 0 ? ((stats.matched / stats.total) * 100).toFixed(0) : 0 }}%
              </div>
              <div class="text-sm text-blue-700 dark:text-blue-300">Match Rate</div>
            </div>
          </div>

          <!-- Warning for Unmatched -->
          <div *ngIf="stats.unmatched > 0" class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
            <div class="flex items-start gap-3">
              <mat-icon class="text-amber-600 dark:text-amber-400">warning</mat-icon>
              <div class="flex-1">
                <p class="text-sm font-medium text-amber-900 dark:text-amber-100">
                  {{ stats.unmatched }} photo(s) could not be matched with any student
                </p>
                <p class="text-xs text-amber-700 dark:text-amber-300 mt-1">
                  These files will be skipped during upload. Check the admission numbers in the file names.
                </p>
              </div>
            </div>
          </div>

          <!-- Photo Preview List -->
          <div class="space-y-3 max-h-96 overflow-y-auto">
            <div
              *ngFor="let match of photoMatches"
              class="flex items-center gap-4 p-4 rounded-xl border transition-all"
              [ngClass]="{
                'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800': match.student,
                'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800': !match.student
              }">
              <!-- Photo Preview -->
              <div class="w-16 h-16 rounded-lg overflow-hidden shrink-0 bg-gray-100 dark:bg-gray-800">
                <img
                  *ngIf="match.preview"
                  [src]="match.preview"
                  [alt]="match.admissionNumber"
                  class="w-full h-full object-cover"
                />
                <div *ngIf="!match.preview" class="w-full h-full flex items-center justify-center">
                  <mat-icon class="text-gray-400">image</mat-icon>
                </div>
              </div>

              <!-- File Info -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">
                    {{ match.file.name }}
                  </p>
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300">
                    {{ formatFileSize(match.file.size) }}
                  </span>
                </div>
                
                <div *ngIf="match.student" class="flex items-center gap-2 text-xs text-green-700 dark:text-green-300">
                  <mat-icon class="icon-size-4">check_circle</mat-icon>
                  <span>{{ match.student.fullName }} ({{ match.student.admissionNumber }})</span>
                </div>
                
                <div *ngIf="!match.student" class="flex items-center gap-2 text-xs text-red-700 dark:text-red-300">
                  <mat-icon class="icon-size-4">cancel</mat-icon>
                  <span>No student found with admission number: {{ match.admissionNumber }}</span>
                </div>
              </div>

              <!-- Status Icon -->
              <div class="shrink-0">
                <mat-icon *ngIf="match.student" class="text-green-600 dark:text-green-400">
                  check_circle
                </mat-icon>
                <mat-icon *ngIf="!match.student" class="text-red-600 dark:text-red-400">
                  error
                </mat-icon>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 3: Upload Progress -->
        <div *ngIf="currentStep === 'uploading'" class="space-y-6">
          <!-- Overall Progress -->
          <div class="bg-indigo-50 dark:bg-indigo-900/20 rounded-xl p-6 border border-indigo-200 dark:border-indigo-800">
            <div class="flex items-center justify-between mb-3">
              <h3 class="text-lg font-semibold text-indigo-900 dark:text-indigo-100">
                Uploading Photos...
              </h3>
              <span class="text-sm font-medium text-indigo-700 dark:text-indigo-300">
                {{ stats.uploaded + stats.failed }} / {{ stats.matched }}
              </span>
            </div>
            <mat-progress-bar
              mode="determinate"
              [value]="uploadProgress"
              class="h-2 rounded-full">
            </mat-progress-bar>
            <p class="text-xs text-indigo-700 dark:text-indigo-300 mt-2">
              {{ uploadProgress.toFixed(0) }}% complete
            </p>
          </div>

          <!-- Individual Upload Status -->
          <div class="space-y-3 max-h-96 overflow-y-auto">
            <div
              *ngFor="let match of matchedPhotos"
              class="flex items-center gap-4 p-4 rounded-xl border transition-all"
              [ngClass]="{
                'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800': match.status === 'uploading',
                'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800': match.status === 'success',
                'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800': match.status === 'error',
                'bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700': match.status === 'pending'
              }">
              <!-- Photo Preview -->
              <div class="w-12 h-12 rounded-lg overflow-hidden shrink-0 bg-gray-100 dark:bg-gray-800">
                <img
                  *ngIf="match.preview"
                  [src]="match.preview"
                  [alt]="match.student?.fullName"
                  class="w-full h-full object-cover"
                />
              </div>

              <!-- Student Info -->
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">
                  {{ match.student?.fullName }}
                </p>
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  {{ match.student?.admissionNumber }}
                </p>
                <p *ngIf="match.errorMessage" class="text-xs text-red-600 dark:text-red-400 mt-1">
                  {{ match.errorMessage }}
                </p>
              </div>

              <!-- Status Indicator -->
              <div class="shrink-0">
                <mat-spinner *ngIf="match.status === 'uploading'" diameter="24" class="text-blue-600"></mat-spinner>
                <mat-icon *ngIf="match.status === 'success'" class="text-green-600 dark:text-green-400">
                  check_circle
                </mat-icon>
                <mat-icon *ngIf="match.status === 'error'" class="text-red-600 dark:text-red-400">
                  error
                </mat-icon>
                <mat-icon *ngIf="match.status === 'pending'" class="text-gray-400">
                  schedule
                </mat-icon>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 4: Results -->
        <div *ngIf="currentStep === 'results'" class="space-y-6">
          <!-- Success Summary -->
          <div class="text-center py-8">
            <div class="w-20 h-20 rounded-full mx-auto mb-4 flex items-center justify-center"
                 [ngClass]="{
                   'bg-green-100 dark:bg-green-900/30': stats.failed === 0,
                   'bg-amber-100 dark:bg-amber-900/30': stats.failed > 0
                 }">
              <mat-icon class="icon-size-10"
                        [ngClass]="{
                          'text-green-600 dark:text-green-400': stats.failed === 0,
                          'text-amber-600 dark:text-amber-400': stats.failed > 0
                        }">
                {{ stats.failed === 0 ? 'check_circle' : 'warning' }}
              </mat-icon>
            </div>
            <h3 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
              Upload {{ stats.failed === 0 ? 'Complete' : 'Completed with Issues' }}
            </h3>
            <p class="text-gray-600 dark:text-gray-400">
              {{ stats.uploaded }} of {{ stats.matched }} photos uploaded successfully
            </p>
          </div>

          <!-- Results Stats -->
          <div class="grid grid-cols-3 gap-4">
            <div class="bg-green-50 dark:bg-green-900/20 rounded-xl p-4 border border-green-200 dark:border-green-800 text-center">
              <div class="text-3xl font-bold text-green-900 dark:text-green-100">{{ stats.uploaded }}</div>
              <div class="text-sm text-green-700 dark:text-green-300">Successful</div>
            </div>
            <div class="bg-red-50 dark:bg-red-900/20 rounded-xl p-4 border border-red-200 dark:border-red-800 text-center">
              <div class="text-3xl font-bold text-red-900 dark:text-red-100">{{ stats.failed }}</div>
              <div class="text-sm text-red-700 dark:text-red-300">Failed</div>
            </div>
            <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 border border-gray-200 dark:border-gray-700 text-center">
              <div class="text-3xl font-bold text-gray-900 dark:text-white">{{ stats.unmatched }}</div>
              <div class="text-sm text-gray-600 dark:text-gray-400">Skipped</div>
            </div>
          </div>

          <!-- Failed Uploads Details -->
          <div *ngIf="stats.failed > 0" class="space-y-3">
            <h4 class="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              <mat-icon class="text-red-600 icon-size-5">error</mat-icon>
              Failed Uploads
            </h4>
            <div class="space-y-2 max-h-60 overflow-y-auto">
              <div
                *ngFor="let match of failedPhotos"
                class="flex items-center gap-3 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-200 dark:border-red-800">
                <mat-icon class="text-red-600 dark:text-red-400 icon-size-5">cancel</mat-icon>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-red-900 dark:text-red-100">
                    {{ match.student?.fullName }}
                  </p>
                  <p class="text-xs text-red-700 dark:text-red-300">
                    {{ match.errorMessage }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="flex items-center justify-between gap-3 p-6 border-t bg-gray-50 dark:bg-gray-800">
        <button
          *ngIf="currentStep === 'preview' || currentStep === 'instructions'"
          mat-button
          (click)="cancel()"
          [disabled]="isUploading">
          <mat-icon>arrow_back</mat-icon>
          {{ currentStep === 'preview' ? 'Back' : 'Cancel' }}
        </button>

        <div class="flex-1"></div>

        <div class="flex items-center gap-3">
          <button
            *ngIf="currentStep === 'preview'"
            mat-stroked-button
            (click)="cancel()">
            Cancel
          </button>

          <button
            *ngIf="currentStep === 'preview' && stats.matched > 0"
            mat-flat-button
            class="bg-gradient-to-r from-indigo-600 to-violet-600 text-white hover:from-indigo-700 hover:to-violet-700"
            (click)="startUpload()"
            [disabled]="isUploading">
            <mat-icon class="icon-size-5 mr-2">cloud_upload</mat-icon>
            Upload {{ stats.matched }} Photo{{ stats.matched !== 1 ? 's' : '' }}
          </button>

          <button
            *ngIf="currentStep === 'results'"
            mat-flat-button
            class="bg-gradient-to-r from-indigo-600 to-violet-600 text-white hover:from-indigo-700 hover:to-violet-700"
            (click)="close()">
            <mat-icon class="icon-size-5 mr-2">check</mat-icon>
            Done
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host ::ng-deep .mat-mdc-progress-bar {
      --mdc-linear-progress-active-indicator-color: rgb(79 70 229);
      --mdc-linear-progress-track-color: rgb(224 231 255);
    }
  `]
})
export class BulkPhotoUploadDialogComponent implements OnInit, OnDestroy {
  private _dialogRef = inject(MatDialogRef<BulkPhotoUploadDialogComponent>);
  private _studentService = inject(StudentService);
  private _unsubscribe = new Subject<void>();

  currentStep: 'instructions' | 'preview' | 'uploading' | 'results' = 'instructions';
  photoMatches: PhotoMatch[] = [];
  students: StudentDto[] = [];
  isUploading = false;

  stats: UploadStats = {
    total: 0,
    matched: 0,
    unmatched: 0,
    uploaded: 0,
    failed: 0,
  };

  get matchedPhotos(): PhotoMatch[] {
    return this.photoMatches.filter(m => m.student !== null);
  }

  get failedPhotos(): PhotoMatch[] {
    return this.photoMatches.filter(m => m.status === 'error');
  }

  get uploadProgress(): number {
    if (this.stats.matched === 0) return 0;
    return ((this.stats.uploaded + this.stats.failed) / this.stats.matched) * 100;
  }

  ngOnInit(): void {
    this.loadStudents();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    
    // Clean up preview URLs
    this.photoMatches.forEach(match => {
      if (match.preview) {
        URL.revokeObjectURL(match.preview);
      }
    });
  }

  private loadStudents(): void {
    this._studentService.getAll()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (students) => {
          this.students = students;
        },
        error: (err) => {
          console.error('Failed to load students:', err);
        }
      });
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const files = Array.from(input.files);
    this.processFiles(files);
  }

  private processFiles(files: File[]): void {
    this.photoMatches = [];
    this.stats = {
      total: files.length,
      matched: 0,
      unmatched: 0,
      uploaded: 0,
      failed: 0,
    };

    files.forEach(file => {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        return;
      }

      // Extract admission number from filename
      const admissionNumber = this.extractAdmissionNumber(file.name);
      
      // Find matching student
      const student = this.findStudentByAdmissionNumber(admissionNumber);

      // Create preview
      const preview = URL.createObjectURL(file);

      const match: PhotoMatch = {
        file,
        admissionNumber,
        student,
        status: 'pending',
        preview,
      };

      this.photoMatches.push(match);

      if (student) {
        this.stats.matched++;
      } else {
        this.stats.unmatched++;
      }
    });

    if (this.photoMatches.length > 0) {
      this.currentStep = 'preview';
    }
  }

  private extractAdmissionNumber(filename: string): string {
    // Remove file extension
    const nameWithoutExt = filename.substring(0, filename.lastIndexOf('.')) || filename;
    
    // Return the filename without extension as the admission number
    // This allows for various admission number formats
    return nameWithoutExt.trim();
  }

  private findStudentByAdmissionNumber(admissionNumber: string): StudentDto | null {
    return this.students.find(s => 
      s.admissionNumber?.toLowerCase() === admissionNumber.toLowerCase()
    ) || null;
  }

  async startUpload(): Promise<void> {
    this.currentStep = 'uploading';
    this.isUploading = true;
    this.stats.uploaded = 0;
    this.stats.failed = 0;

    const matchedPhotos = this.matchedPhotos;

    // Upload photos sequentially to show progress
    for (const match of matchedPhotos) {
      match.status = 'uploading';
      
      try {
        await this.uploadPhoto(match);
        match.status = 'success';
        this.stats.uploaded++;
      } catch (error: any) {
        match.status = 'error';
        match.errorMessage = error.message || 'Upload failed';
        this.stats.failed++;
      }

      // Small delay for visual feedback
      await new Promise(resolve => setTimeout(resolve, 300));
    }

    this.isUploading = false;
    this.currentStep = 'results';
  }

  private uploadPhoto(match: PhotoMatch): Promise<void> {
    return new Promise((resolve, reject) => {
      if (!match.student) {
        reject(new Error('No student found'));
        return;
      }

      this._studentService.uploadPhoto(match.student.id, match.file)
        .pipe(
          takeUntil(this._unsubscribe),
          catchError(err => {
            const errorMessage = err.error?.message || err.message || 'Upload failed';
            reject(new Error(errorMessage));
            return of(null);
          })
        )
        .subscribe({
          next: (response) => {
            if (response) {
              resolve();
            } else {
              reject(new Error('Upload failed - no response'));
            }
          },
          error: (err) => {
            const errorMessage = err.error?.message || err.message || 'Upload failed';
            reject(new Error(errorMessage));
          }
        });
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  cancel(): void {
    if (this.currentStep === 'preview') {
      this.currentStep = 'instructions';
      this.photoMatches = [];
      this.stats = {
        total: 0,
        matched: 0,
        unmatched: 0,
        uploaded: 0,
        failed: 0,
      };
    } else {
      this.close();
    }
  }

  close(): void {
    this._dialogRef.close(this.stats.uploaded > 0);
  }
}