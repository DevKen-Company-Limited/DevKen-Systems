import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';

import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { StudentPersonalInfoComponent } from '../personal-information/student-personal-info.component';
import { StudentLocationComponent } from '../location/student-location.component';
import { StudentAcademicComponent } from '../academic/Student-academic.component';
import { StudentGuardiansComponent } from '../guardians/Student-guardians.component';
import { StudentMedicalComponent } from '../medical/Student-medical.component';
import { StudentReviewComponent } from '../review/Student-review.component';
import { EnrollmentStep } from '../../types/EnrollmentStep';
import { normalizeStudentEnums } from '../../types/Enums';

// Import centralized enum utilities


@Component({
  selector: 'app-student-enrollment',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    FuseAlertComponent,
    StudentPersonalInfoComponent,
    StudentLocationComponent,
    StudentAcademicComponent,
    StudentMedicalComponent,
    StudentGuardiansComponent,
    StudentReviewComponent,
  ],
  templateUrl: './student-enrollment.component.html',
  animations: [
    trigger('stepTransition', [
      transition(':increment', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in', style({ opacity: 0, transform: 'translateX(-40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))], { optional: true }),
        ]),
      ]),
      transition(':decrement', [
        query(':enter', [style({ opacity: 0, transform: 'translateX(-40px)' })], { optional: true }),
        group([
          query(':leave', [animate('180ms ease-in', style({ opacity: 0, transform: 'translateX(40px)' }))], { optional: true }),
          query(':enter', [animate('220ms 160ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))], { optional: true }),
        ]),
      ]),
    ]),
  ],
})
export class StudentEnrollmentComponent implements OnInit, OnDestroy {

  // ─── State ────────────────────────────────────────────────────────
  currentStep    = 0;
  completedSteps = new Set<number>();
  studentId: string | null = null;
  isEditMode = false;
  isSaving     = false;
  isSubmitting = false;
  lastSaved: Date | null = null;
  alert: { type: 'success' | 'error'; message: string } | null = null;

  // ─── Lookup data ─────────────────────────────────────────────────
  schools: any[]       = [];
  classes: any[]       = [];
  academicYears: any[] = [];

  private destroy$ = new Subject<void>();

  // Sidebar state
  isSidebarCollapsed = false;
  showMobileSidebar = false;
  isMobileView = false;

  // ─── Responsive handling ─────────────────────────────────────────
  @HostListener('window:resize', ['$event'])
  onResize(event?: Event): void {
    this.checkViewport();
  }

  private checkViewport(): void {
    const width = window.innerWidth;
    this.isMobileView = width < 1024;
    
    if (width < 1280 && width >= 1024) {
      this.isSidebarCollapsed = true;
    }
    
    if (!this.isMobileView) {
      this.showMobileSidebar = false;
    }
  }

  toggleSidebar(): void {
    if (this.isMobileView) {
      this.showMobileSidebar = !this.showMobileSidebar;
    } else {
      this.isSidebarCollapsed = !this.isSidebarCollapsed;
    }
  }

  // ─── Steps definition ─────────────────────────────────────────────
  steps: EnrollmentStep[] = [
    { label: 'Personal Info',     icon: 'user',         sectionKey: 'personal'   },
    { label: 'Location',          icon: 'map-pin',      sectionKey: 'location'   },
    { label: 'Academic Details',  icon: 'academic-cap', sectionKey: 'academic'   },
    { label: 'Medical & Health',  icon: 'heart',        sectionKey: 'medical'    },
    { label: 'Guardians',         icon: 'users',        sectionKey: 'guardians'  },
    { label: 'Review & Submit',   icon: 'check-circle', sectionKey: 'review'     },
  ];

  // ─── Section validity ─────────────────────────────────────────────
  sectionValid: Record<string, boolean> = {
    personal:  false,
    location:  true,
    academic:  false,
    medical:   true,
    guardians: false,
  };

  // ─── Form data per section ────────────────────────────────────────
  formSections: Record<string, any> = {
    personal:  {},
    location:  {},
    academic:  {},
    medical:   {},
    guardians: {},
  };

  // ──────────────────────────────────────────────────────────────────
  constructor(
    private studentService: StudentService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.studentId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.studentId;
    
    if (this.studentId) {
      this.loadExistingStudent(this.studentId);
    } else {
      this.loadDraft();
    }
    
    this.loadLookups();
    this.checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Lookup loading ───────────────────────────────────────────────
  private loadLookups(): void {
    this.studentService.getSchools().pipe(takeUntil(this.destroy$)).subscribe(s => this.schools = s);
    this.studentService.getClasses().pipe(takeUntil(this.destroy$)).subscribe(c => this.classes = c);
    this.studentService.getAcademicYears().pipe(takeUntil(this.destroy$)).subscribe(y => this.academicYears = y);
  }

  private loadExistingStudent(id: string): void {
    this.studentService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (student) => {

          console.log('[Enrollment] Raw student data:', student);

          this.hydrateFromStudent(student);

          this.steps.slice(0, 5).forEach((_, i) => this.completedSteps.add(i));
          this.showAlert('info', 'Editing existing student record');
        },
        error: (err) => {
          console.error('[Enrollment] Failed to load student:', err);
          this.showAlert('error', 'Could not load student data.');
        },
      });
  }

  /**
   * Safely convert API values to numbers
   * API returns enum values as strings ("2", "5") but forms need numbers
   */
  private toNumber(val: any): number | null {
    if (val === null || val === undefined || val === '') return null;
    const num = typeof val === 'string' ? parseInt(val, 10) : val;
    return isNaN(num) ? null : num;
  }

  /**
   * Normalize academic status string
   * API might return "Active" but form uses "Regular"
   */
  private normalizeAcademicStatus(val: any): string {
    if (!val) return '';
    const str = String(val).toLowerCase();
    // Map API values to form values
    if (str === 'active') return 'Regular';
    if (str === 'regular') return 'Regular';
    return val; // Return as-is if no mapping needed
  }

  private hydrateFromStudent(s: any): void {
    console.log('[Enrollment] Hydrating form sections...');
    
    // CRITICAL: Use centralized normalization utility
    const normalized = normalizeStudentEnums(s);
    
    console.log('[Enrollment] Normalized student data:', normalized);
    
    // Helper to safely convert to number (API returns strings like "2", "5")
    const toNum = this.toNumber.bind(this);
    
    this.formSections.personal = {
      firstName: s.firstName,
      lastName: s.lastName,
      middleName: s.middleName,
      admissionNumber: s.admissionNumber,
      nemisNumber: s.nemisNumber,
      birthCertificateNumber: s.birthCertificateNumber,
      dateOfBirth: s.dateOfBirth,
      gender: toNum(normalized.gender ?? s.gender), // Convert string "2" -> number 2
      religion: s.religion,
      nationality: s.nationality,
      photoUrl: s.photoUrl,
      dateOfAdmission: s.dateOfAdmission,
      studentStatus: toNum(normalized.studentStatus ?? s.studentStatus), // Convert string "5" -> number 5
      cbcLevel: toNum(normalized.cbcLevel ?? s.cbcLevel), // Convert string "2" -> number 2
    };
    
    this.formSections.location = {
      placeOfBirth: s.placeOfBirth,
      county: s.county,
      subCounty: s.subCounty,
      homeAddress: s.homeAddress,
    };
    
    this.formSections.academic = {
      schoolId: s.schoolId,
      currentLevel: toNum(normalized.currentLevel ?? s.currentLevel), // Convert string "1" -> number 1
      currentClassId: s.currentClassId,
      currentAcademicYearId: s.currentAcademicYearId,
      previousSchool: s.previousSchool,
      status: this.normalizeAcademicStatus(s.status), // Normalize "Active" -> "Regular"
    };
    
    this.formSections.medical = {
      bloodGroup: s.bloodGroup,
      medicalConditions: s.medicalConditions,
      allergies: s.allergies,
      specialNeeds: s.specialNeeds,
      requiresSpecialSupport: s.requiresSpecialSupport,
    };
    
    this.formSections.guardians = {
      primaryGuardianName: s.primaryGuardianName,
      primaryGuardianRelationship: s.primaryGuardianRelationship,
      primaryGuardianPhone: s.primaryGuardianPhone,
      primaryGuardianEmail: s.primaryGuardianEmail,
      primaryGuardianOccupation: s.primaryGuardianOccupation,
      primaryGuardianAddress: s.primaryGuardianAddress,
      secondaryGuardianName: s.secondaryGuardianName,
      secondaryGuardianRelationship: s.secondaryGuardianRelationship,
      secondaryGuardianPhone: s.secondaryGuardianPhone,
      secondaryGuardianEmail: s.secondaryGuardianEmail,
      secondaryGuardianOccupation: s.secondaryGuardianOccupation,
      emergencyContactName: s.emergencyContactName,
      emergencyContactPhone: s.emergencyContactPhone,
      emergencyContactRelationship: s.emergencyContactRelationship,
    };
    
    console.log('[Enrollment] Form sections hydrated:', this.formSections);
    console.log('[Enrollment] Enum values converted to numbers:', {
      gender: this.formSections.personal.gender,
      studentStatus: this.formSections.personal.studentStatus,
      cbcLevel: this.formSections.personal.cbcLevel,
      currentLevel: this.formSections.academic.currentLevel
    });
  }

  // ─── Draft persistence (localStorage) ────────────────────────────
  private readonly DRAFT_KEY = 'student_enrollment_draft';

  private loadDraft(): void {
    if (this.studentId) return;
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections  = { ...this.formSections,  ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep   = draft.currentStep ?? 0;
      this.lastSaved     = draft.savedAt ? new Date(draft.savedAt) : null;
      this.showAlert('info', 'Draft loaded. You can continue where you left off.');
    } catch { /* malformed draft */ }
  }

  private persistDraft(): void {
    const draft = {
      formSections:   this.formSections,
      completedSteps: Array.from(this.completedSteps),
      currentStep:    this.currentStep,
      savedAt:        new Date().toISOString(),
    };
    localStorage.setItem(this.DRAFT_KEY, JSON.stringify(draft));
    this.lastSaved = new Date();
  }

  private clearDraft(): void {
    localStorage.removeItem(this.DRAFT_KEY);
  }

  // ─── Section events ───────────────────────────────────────────────
  onSectionChanged(section: string, data: any): void {
    console.log(`[Enrollment] Section ${section} changed:`, data);
    this.formSections[section] = { ...this.formSections[section], ...data };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ─── Navigation ───────────────────────────────────────────────────
  navigateToStep(index: number): void {
    if (this.canNavigateTo(index)) {
      this.currentStep = index;
      if (this.isMobileView) {
        this.showMobileSidebar = false;
      }
    }
  }

  prevStep(): void {
    if (this.currentStep > 0) this.currentStep--;
  }

  nextStep(): void {
    if (!this.canProceed()) return;
    this.completedSteps.add(this.currentStep);
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    }
    this.persistDraft();
  }

  saveDraft(): void {
    this.isSaving = true;
    this.persistDraft();
    setTimeout(() => {
      this.isSaving = false;
      this.showAlert('success', 'Draft saved locally. You can continue later.');
    }, 500);
  }

  // ─── Final submission ────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;

    this.isSubmitting = true;
    try {
      const payload = this.buildPayload();
      console.log('[Enrollment] Submitting payload:', payload);
      
      if (this.studentId) {
        // Update existing student
        await this.studentService.update(this.studentId, payload).toPromise();
        this.showAlert('success', 'Student updated successfully!');
      } else {
        // Create new student
        const created: any = await this.studentService.create(payload).toPromise();
        this.studentId = created?.data?.id ?? created?.id;
        this.showAlert('success', 'Student enrolled successfully!');
      }
      
      this.clearDraft();
      setTimeout(() => this.router.navigate(['/academic/students']), 1500);
    } catch (err: any) {
      console.error('[Enrollment] Submission error:', err);
      this.showAlert('error', err?.error?.message || 'Submission failed. Please review and try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): any {
    // All enum values should already be numbers from form emissions
    return {
      ...this.formSections.personal,
      ...this.formSections.location,
      ...this.formSections.academic,
      ...this.formSections.medical,
      ...this.formSections.guardians,
    };
  }

  // ─── Guards ───────────────────────────────────────────────────────
  canProceed(): boolean {
    const key = this.steps[this.currentStep]?.sectionKey;
    return this.sectionValid[key] !== false;
  }

  canNavigateTo(index: number): boolean {
    if (index === 0) return true;
    if (index <= this.currentStep) return true;
    if (this.isEditMode) return true;
    return this.completedSteps.has(index - 1);
  }

  isStepCompleted(index: number): boolean {
    return this.completedSteps.has(index);
  }

  allStepsCompleted(): boolean {
    return this.steps.slice(0, 5).every((_, i) => this.completedSteps.has(i));
  }

  // ─── Progress ring ────────────────────────────────────────────────
  getProgressPercent(): number {
    return Math.round((this.completedSteps.size / (this.steps.length - 1)) * 100);
  }

  getRingOffset(): number {
    const circumference = 2 * Math.PI * 56;
    const pct = this.completedSteps.size / (this.steps.length - 1);
    return circumference * (1 - pct);
  }

  // ─── Helpers ─────────────────────────────────────────────────────
  goBack(): void {
    this.router.navigate(['/academic/students']);
  }

  private showAlert(type: 'success' | 'error' | 'info', message: string): void {
    this.alert = { type: type as 'success' | 'error', message };
    if (type === 'success' || type === 'info') {
      setTimeout(() => { 
        if (this.alert?.type === type) this.alert = null; 
      }, 3500);
    }
  }
}