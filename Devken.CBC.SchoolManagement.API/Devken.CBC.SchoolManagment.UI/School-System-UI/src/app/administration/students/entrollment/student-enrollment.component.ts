import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';
import { Subject } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';

import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { EnrollmentStep } from '../types/EnrollmentStep';
import { StudentPersonalInfoComponent } from '../personal-information/student-personal-info.component';
import { StudentLocationComponent } from '../location/student-location.component';
import { StudentAcademicComponent } from '../academic/Student-academic.component';
import { StudentGuardiansComponent } from '../guardians/Student-guardians.component';
import { StudentMedicalComponent } from '../medical/Student-medical.component';
import { StudentReviewComponent } from '../review/Student-review.component';

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
    this.isMobileView = width < 1024; // lg breakpoint
    
    // Auto-collapse sidebar on tablet
    if (width < 1280 && width >= 1024) { // xl breakpoint
      this.isSidebarCollapsed = true;
    }
    
    // Close mobile sidebar when resizing to desktop
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
    location:  true,   // optional sections start valid
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
    if (this.studentId) this.loadExistingStudent(this.studentId);
    this.loadLookups();
    this.loadDraft();
    this.checkViewport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Lookup loading ───────────────────────────────────────────────
  private loadLookups(): void {
    this.studentService.getSchools().pipe().subscribe(s => this.schools = s);
    this.studentService.getClasses().pipe().subscribe(c => this.classes = c);
    this.studentService.getAcademicYears().pipe().subscribe(y => this.academicYears = y);
  }

  private loadExistingStudent(id: string): void {
    this.studentService.getById(id).pipe().subscribe({
      next: (student) => {
        this.hydrateFromStudent(student);
        this.steps.slice(0, 5).forEach((_, i) => this.completedSteps.add(i));
      },
      error: () => this.showAlert('error', 'Could not load student data.'),
    });
  }

  private hydrateFromStudent(s: any): void {
    this.formSections.personal = {
      firstName: s.firstName, lastName: s.lastName, middleName: s.middleName,
      admissionNumber: s.admissionNumber, nemisNumber: s.nemisNumber,
      birthCertificateNumber: s.birthCertificateNumber, dateOfBirth: s.dateOfBirth,
      gender: s.gender, religion: s.religion, nationality: s.nationality,
      photoUrl: s.photoUrl, dateOfAdmission: s.dateOfAdmission,
      studentStatus: s.studentStatus, cbcLevel: s.cbcLevel,
    };
    this.formSections.location = {
      placeOfBirth: s.placeOfBirth, county: s.county,
      subCounty: s.subCounty, homeAddress: s.homeAddress,
    };
    this.formSections.academic = {
      schoolId: s.schoolId, currentLevel: s.currentLevel,
      currentClassId: s.currentClassId, currentAcademicYearId: s.currentAcademicYearId,
      previousSchool: s.previousSchool, status: s.status,
    };
    this.formSections.medical = {
      bloodGroup: s.bloodGroup, medicalConditions: s.medicalConditions,
      allergies: s.allergies, specialNeeds: s.specialNeeds,
      requiresSpecialSupport: s.requiresSpecialSupport,
    };
    this.formSections.guardians = {
      primaryGuardianName: s.primaryGuardianName, primaryGuardianRelationship: s.primaryGuardianRelationship,
      primaryGuardianPhone: s.primaryGuardianPhone, primaryGuardianEmail: s.primaryGuardianEmail,
      primaryGuardianOccupation: s.primaryGuardianOccupation, primaryGuardianAddress: s.primaryGuardianAddress,
      secondaryGuardianName: s.secondaryGuardianName, secondaryGuardianRelationship: s.secondaryGuardianRelationship,
      secondaryGuardianPhone: s.secondaryGuardianPhone, secondaryGuardianEmail: s.secondaryGuardianEmail,
      secondaryGuardianOccupation: s.secondaryGuardianOccupation,
      emergencyContactName: s.emergencyContactName, emergencyContactPhone: s.emergencyContactPhone,
      emergencyContactRelationship: s.emergencyContactRelationship,
    };
  }

  // ─── Draft persistence (localStorage) ────────────────────────────
  private readonly DRAFT_KEY = 'student_enrollment_draft';

  private loadDraft(): void {
    if (this.studentId) return; // editing existing — skip draft
    const raw = localStorage.getItem(this.DRAFT_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      this.formSections  = { ...this.formSections,  ...draft.formSections };
      this.completedSteps = new Set(draft.completedSteps ?? []);
      this.currentStep   = draft.currentStep ?? 0;
      this.lastSaved     = draft.savedAt ? new Date(draft.savedAt) : null;
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
    this.formSections[section] = { ...this.formSections[section], ...data };
  }

  onSectionValidChanged(section: string, valid: boolean): void {
    this.sectionValid[section] = valid;
  }

  // ─── Navigation ───────────────────────────────────────────────────
  navigateToStep(index: number): void {
    if (this.canNavigateTo(index)) {
      this.currentStep = index;
      // Close mobile sidebar after navigation
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
    this.persistDraft();
    this.showAlert('success', 'Draft saved locally. You can continue later.');
  }

  // ─── Final submission ────────────────────────────────────────────
  async submitForm(): Promise<void> {
    if (!this.allStepsCompleted()) return;

    this.isSubmitting = true;
    try {
      const payload = this.buildPayload(); // combine all sections
      if (this.studentId) {
        await this.studentService.update(this.studentId, payload).toPromise();
      } else {
        const created: any = await this.studentService.create(payload).toPromise();
        this.studentId = created?.data?.id ?? created?.id;
      }
      this.clearDraft();
      this.showAlert('success', 'Student enrolled successfully!');
      setTimeout(() => this.router.navigate(['/academic/students']), 1500);
    } catch {
      this.showAlert('error', 'Submission failed. Please review and try again.');
    } finally {
      this.isSubmitting = false;
    }
  }

  private buildPayload(): any {
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
    const circumference = 2 * Math.PI * 56; // r=56
    const pct = this.completedSteps.size / (this.steps.length - 1);
    return circumference * (1 - pct);
  }

  // ─── Helpers ─────────────────────────────────────────────────────
  goBack(): void {
    this.router.navigate(['/academic/students']);
  }

  private showAlert(type: 'success' | 'error', message: string): void {
    this.alert = { type, message };
    if (type === 'success') {
      setTimeout(() => { if (this.alert?.type === 'success') this.alert = null; }, 3500);
    }
  }
}