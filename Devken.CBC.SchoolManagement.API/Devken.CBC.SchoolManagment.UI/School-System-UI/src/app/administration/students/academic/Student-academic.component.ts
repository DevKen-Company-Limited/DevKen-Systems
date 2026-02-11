import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { EnumService, EnumItemDto } from 'app/core/DevKenService/common/enum.service';

@Component({
  selector: 'app-student-academic',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
  ],
  templateUrl: './student-academic.component.html',
})
export class StudentAcademicComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() schools: any[] = [];
  @Input() classes: any[] = [];
  @Input() academicYears: any[] = [];
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  private enumService = inject(EnumService);
  private destroyRef = inject(DestroyRef);

  form!: FormGroup;

  // Enums
  cbcLevels: EnumItemDto[] = [];
  studentStatuses$: Observable<EnumItemDto[]> = of([]);

  ngOnInit(): void {
    this.buildForm();
    this.setupFormListeners();
    this.loadEnums();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue(this.formData, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      currentLevel: [this.formData?.currentLevel ?? '', Validators.required],
      currentClassId: [this.formData?.currentClassId ?? ''],
      currentAcademicYearId: [this.formData?.currentAcademicYearId ?? ''],
      status: [this.formData?.status ?? '', Validators.required],
      previousSchool: [this.formData?.previousSchool ?? ''],
    });
  }

  private setupFormListeners(): void {
    this.form.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        const numericLevel = this.mapLevelToNumber(value.currentLevel);
        const numericStatus = Number(value.status);
        this.formChanged.emit({ 
          ...value, 
          currentLevel: numericLevel,
          status: numericStatus 
        });
        this.formValid.emit(this.form.valid);
      });
  }

  private loadEnums(): void {
    // Load CBC Levels from backend
    this.enumService.getCBCLevels()
      .pipe(
        catchError(err => {
          console.error('Failed to load CBC Levels', err);
          return of([]);
        })
      )
      .subscribe(levels => {
        this.cbcLevels = levels;
      });

    // Load student statuses
    this.studentStatuses$ = this.enumService.getStudentStatuses()
      .pipe(
        catchError(err => {
          console.error('Failed to load Student Statuses', err);
          return of([]);
        })
      );
  }

  private mapLevelToNumber(level: string | number): number | null {
    if (!level) return null;
    if (typeof level === 'number') return level;

    // Try to find the enum with matching id/name
    const matched = this.cbcLevels.find(l => l.id === level || l.name === level);
    return matched ? matched.value : null;
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
}