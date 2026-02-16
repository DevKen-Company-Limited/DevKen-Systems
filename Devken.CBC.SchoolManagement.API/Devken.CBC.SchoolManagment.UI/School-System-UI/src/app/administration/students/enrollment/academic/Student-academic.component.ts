import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { AuthService } from 'app/core/auth/auth.service';
import { CBCLevelOptions, normalizeCBCLevel, toNumber } from '../../types/Enums';

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
    FuseAlertComponent,
  ],
  templateUrl: './student-academic.component.html',
})
export class StudentAcademicComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() schools: any[] = [];
  @Input() classes: any[] = [];
  @Input() academicYears: any[] = [];
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  form!: FormGroup;

  // Use centralized CBC Levels
  cbcLevels = CBCLevelOptions;

  get isSuperAdmin(): boolean {
    return this.authService.authUser?.isSuperAdmin ?? false;
  }

  ngOnInit(): void {
    this.buildForm();
    this.setupFormListeners();
    this.formValid.emit(this.form.valid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      console.log('[Academic] Raw formData:', this.formData);
      
      // Normalize the incoming data
      const patchData = this.normalizeFormData(this.formData);
      
      console.log('[Academic] Normalized patch data:', patchData);
      this.form.patchValue(patchData, { emitEvent: false });
    }
  }

  private buildForm(): void {
    console.log('[Academic] Building form with data:', this.formData);
    
    // Normalize data for initial form build
    const normalizedData = this.normalizeFormData(this.formData);
    
    console.log('[Academic] Normalized initial data:', normalizedData);
    
    const formConfig: any = {
      currentLevel: [
        normalizedData.currentLevel ?? '', 
        Validators.required
      ],
      currentClassId: [normalizedData.currentClassId ?? null],
      currentAcademicYearId: [normalizedData.currentAcademicYearId ?? null],
      previousSchool: [normalizedData.previousSchool ?? ''],
      status: [normalizedData.status ?? ''],
    };

    // Add schoolId field for SuperAdmin
    if (this.isSuperAdmin) {
      formConfig.schoolId = [normalizedData.schoolId ?? '', Validators.required];
    }

    this.form = this.fb.group(formConfig);
  }

  /**
   * Normalize form data from API
   * - Converts currentLevel string to number
   * - Normalizes status field (Active -> Regular)
   */
  private normalizeFormData(data: any): any {
    if (!data) return {};
    
    // Convert currentLevel to number (handles "1" -> 1)
    const currentLevel = normalizeCBCLevel(data.currentLevel);
    
    // Normalize academic status (Active -> Regular, etc.)
    const status = this.normalizeAcademicStatus(data.status);
    
    const normalized = {
      ...data,
      currentLevel,
      status,
    };
    
    console.log('[Academic] Normalization:', {
      input: { currentLevel: data.currentLevel, status: data.status },
      output: { currentLevel, status }
    });
    
    return normalized;
  }

  /**
   * Normalize academic status string
   * API might return "Active" but form uses "Regular"
   */
  private normalizeAcademicStatus(val: any): string {
    if (!val) return '';
    
    const str = String(val).toLowerCase().trim();
    
    // Map API values to form values
    const statusMap: { [key: string]: string } = {
      'active': 'Regular',
      'regular': 'Regular',
      'onprobation': 'OnProbation',
      'on probation': 'OnProbation',
      'suspended': 'Suspended',
      'transferred': 'Transferred',
      'graduated': 'Graduated',
    };
    
    return statusMap[str] || val; // Return mapped value or original
  }

  private setupFormListeners(): void {
    this.form.valueChanges.subscribe(value => {
      // Ensure currentLevel is emitted as a number
      const emitValue = {
        ...value,
        currentLevel: value.currentLevel !== null && value.currentLevel !== '' 
          ? Number(value.currentLevel) 
          : null
      };
      
      console.log('[Academic] Form changed, emitting:', emitValue);
      this.formChanged.emit(emitValue);
      this.formValid.emit(this.form.valid);
    });
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
}