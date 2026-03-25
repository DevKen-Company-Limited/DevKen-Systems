// dialog-modals/Library/book-recommendation-dialog/create-book-recommendation-dialog.component.ts

import {
  Component, OnInit, OnDestroy, Inject, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogRef, MAT_DIALOG_DATA, MatDialogModule,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSliderModule } from '@angular/material/slider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { BookDto } from 'app/Library/book/Types/book.types';
import { SchoolDto } from 'app/Tenant/types/school';
import {
  BookRecommendationDto,
  CreateBookRecommendationRequest,
  UpdateBookRecommendationRequest,
  GenerateRecommendationsRequest,
} from 'app/Library/book-recommendation/Types/book-recommendation.types';
import { StudentDto } from 'app/administration/students/types/studentdto';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { BookRecommendationService } from 'app/core/DevKenService/Library/book-recommendation.service';

export interface CreateBookRecommendationDialogData {
  mode: 'create' | 'edit' | 'view' | 'generate';
  recommendation?: BookRecommendationDto;
  preselectedStudentId?: string;
  preselectedBookId?: string;
}

@Component({
  selector: 'app-create-book-recommendation-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatSliderModule,
  ],
  templateUrl: './create-book-recommendation-dialog.component.html',
  styles: [`:host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }`]
})
export class CreateBookRecommendationDialogComponent
  extends BaseFormDialog<CreateBookRecommendationRequest, UpdateBookRecommendationRequest, BookRecommendationDto, CreateBookRecommendationDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();
  private readonly _alertService: AlertService;
  private readonly _recommendationService: BookRecommendationService;

  books:       BookDto[]    = [];
  students:    StudentDto[] = [];
  schools:     SchoolDto[]  = [];
  isLoading    = false;
  formSubmitted = false;
  isSaving     = false;

  get isEditMode():     boolean { return this.data.mode === 'edit'; }
  get isViewMode():     boolean { return this.data.mode === 'view'; }
  get isCreateMode():   boolean { return this.data.mode === 'create'; }
  get isGenerateMode(): boolean { return this.data.mode === 'generate'; }
  get isSuperAdmin():   boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    const map = {
      create: 'New Book Recommendation',
      edit: 'Edit Recommendation',
      view: 'Recommendation Details',
      generate: 'Generate AI Recommendations',
    };
    return map[this.data.mode] || 'Book Recommendation';
  }

  get dialogSubtitle(): string {
    if (this.isViewMode && this.data.recommendation) {
      return `${this.data.recommendation.bookTitle} → ${this.data.recommendation.studentName}`;
    }
    if (this.isGenerateMode) {
      return 'Let AI suggest personalized books for a student';
    }
    return 'Recommend a book to a student';
  }

  get headerGradient(): string {
    if (this.isViewMode) return 'bg-gradient-to-r from-amber-600 via-orange-600 to-yellow-600';
    if (this.isGenerateMode) return 'bg-gradient-to-r from-violet-600 via-purple-600 to-indigo-600';
    return 'bg-gradient-to-r from-orange-600 via-amber-600 to-yellow-600';
  }

  get recommendation(): BookRecommendationDto | undefined { return this.data.recommendation; }

  constructor(
    fb:                    FormBuilder,
    snackBar:              MatSnackBar,
    alertService:          AlertService,
    dialogRef:             MatDialogRef<CreateBookRecommendationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateBookRecommendationDialogData,
    private readonly _authService:   AuthService,
    private readonly _bookService:   BookService,
    private readonly _studentService: StudentService,
    private readonly _schoolService: SchoolService,
    recommendationService: BookRecommendationService,
    private readonly _cdr: ChangeDetectorRef,
  ) {
    super(fb, recommendationService as any, snackBar, dialogRef, data);
    this._alertService          = alertService;
    this._recommendationService = recommendationService;
    dialogRef.addPanelClass(['book-recommendation-dialog', 'responsive-dialog']);
  }

  ngOnInit():    void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    if (this.isEditMode) {
      return this.fb.group({
        score: [null as number | null, [Validators.required, Validators.min(0), Validators.max(100)]],
        reason: ['', [Validators.required, Validators.maxLength(500)]],
      });
    }

    // Use a Record-style definition to allow dynamic schoolId without type errors
    const config: any = {
      studentId: ['', [Validators.required]],
    };

    if (this.isGenerateMode) {
      config.maxRecommendations = [10, [Validators.required, Validators.min(1), Validators.max(50)]];
    } else {
      config.bookId = [this.data.preselectedBookId || '', [Validators.required]];
      config.score = [75, [Validators.required, Validators.min(0), Validators.max(100)]];
      config.reason = ['', [Validators.required, Validators.maxLength(500)]];
    }

    if (this.isSuperAdmin) {
      config.schoolId = ['', [Validators.required]];
    }

    return this.fb.group(config);
  }

  protected override init(): void {
    this.form = this.buildForm();
    if (!this.isViewMode) this._loadDropdowns();
    if (this.isEditMode && this.data.recommendation) this.patchForEdit(this.data.recommendation);
  }

  protected override patchForEdit(item: BookRecommendationDto): void {
    this.form.patchValue({
      score:  item.score,
      reason: item.reason,
    });
    this._cdr.detectChanges();
  }

  private _loadDropdowns(): void {
    this.isLoading = true;
    const requests: any = {};

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    if (!this.isGenerateMode) {
      requests.books    = this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
      requests.students = this._studentService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    if (Object.keys(requests).length === 0) {
      this.isLoading = false;
      return;
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (res: any) => {
        if (res.schools)  this.schools  = res.schools?.data  || [];
        if (res.books)    this.books    = res.books?.data    || [];
       if (res.students) {
        this.students = Array.isArray(res.students) ? res.students : (res.students?.data || []);
      }
      },
      error: () => {}
    });
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { 
      this.form.markAllAsTouched(); 
      return; 
    }

    const raw = this.form.getRawValue(); // Use getRawValue to get disabled controls if any
    this.isSaving = true;

    // Helper to get schoolId safely
    // Casting to 'any' for schoolId access if AuthUser interface is missing it
    const schoolId = this.isSuperAdmin ? raw.schoolId : (this._authService.authUser as any)?.schoolId || '';

    if (this.isGenerateMode) {
      const payload: GenerateRecommendationsRequest = {
        schoolId: schoolId,
        studentId: raw.studentId,
        maxRecommendations: raw.maxRecommendations,
      };

      this._recommendationService.generateRecommendations(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success(`Generated ${res.data.length} recommendations successfully`);
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this._alertService.error(res.message || 'Failed to generate');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Error generating recommendations'),
        });
      return;
    }

    if (this.isEditMode) {
      const payload: UpdateBookRecommendationRequest = {
        score: raw.score,
        reason: raw.reason?.trim(),
      };

      this._recommendationService.update(this.data.recommendation!.id, payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Updated successfully');
              this.dialogRef.close({ success: true, data: res.data });
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Update failed'),
        });
    } else {
      const payload: CreateBookRecommendationRequest = {
        schoolId: schoolId,
        bookId: raw.bookId,
        studentId: raw.studentId,
        score: raw.score,
        reason: raw.reason?.trim(),
      };

      this._recommendationService.create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Created successfully');
              this.dialogRef.close({ success: true, data: res.data });
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Creation failed'),
        });
    }
  }


  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('min'))       return `Minimum value is ${c.getError('min').min}`;
    if (c.hasError('max'))       return `Maximum value is ${c.getError('max').max}`;
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'text-green-600 dark:text-green-400';
    if (score >= 50) return 'text-amber-600 dark:text-amber-400';
    return 'text-red-600 dark:text-red-400';
  }

  formatScore(value: number): string {
    return `${value}%`;
  }
}