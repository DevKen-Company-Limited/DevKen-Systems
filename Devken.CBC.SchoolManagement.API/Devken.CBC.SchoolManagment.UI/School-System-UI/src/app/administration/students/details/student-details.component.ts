import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { EnumService, EnumItemDto } from 'app/core/DevKenService/common/enum.service';
import { AuthService } from 'app/core/auth/auth.service';
import { API_BASE_URL } from 'app/app.config';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PhotoViewerDialogComponent } from 'app/dialog-modals/Student/photo-viewer-dialog';
import { StudentDto } from '../types/studentdto';

interface DetailSection {
  title: string;
  icon: string;
  iconColor: string;
  items: DetailItem[];
}

interface DetailItem {
  label: string;
  value: string | number | undefined | null;
  icon?: string;
  copyable?: boolean;
  type?: 'text' | 'email' | 'phone' | 'date' | 'status' | 'badge' | 'boolean';
}

interface EnumMaps {
  genderValueToName: Map<number, string>;
  studentStatusValueToName: Map<number, string>;
  cbcLevelValueToName: Map<number, string>;
  religionValueToName: Map<number, string>;
  nationalityValueToName: Map<number, string>;
}

@Component({
  selector: 'app-student-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule,
    MatTooltipModule,
    MatMenuModule,
    MatSnackBarModule,
    MatDialogModule,
    PageHeaderComponent,
  ],
  templateUrl: './student-details.component.html',
  styleUrls: ['./student-details.component.scss'],
})
export class StudentDetailsComponent implements OnInit, OnDestroy {
  private _unsubscribe = new Subject<void>();
  private _route = inject(ActivatedRoute);
  private _router = inject(Router);
  private _service = inject(StudentService);
  private _enumService = inject(EnumService);
  private _snackBar = inject(MatSnackBar);
  private _dialog = inject(MatDialog);
  private _confirmation = inject(FuseConfirmationService);
  private _http = inject(HttpClient);
  private _sanitizer = inject(DomSanitizer);
  private _authService = inject(AuthService);
  private _apiBaseUrl = inject(API_BASE_URL);

  student: StudentDto | null = null;
  isLoading = true;
  isEnumLoading = true;
  photoUrl: SafeUrl | null = null;
  photoLoading = false;
  photoError = false;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Students', url: '/academic/students' },
    { label: 'Details' }
  ];

  private enumMaps: EnumMaps = {
    genderValueToName: new Map<number, string>(),
    studentStatusValueToName: new Map<number, string>(),
    cbcLevelValueToName: new Map<number, string>(),
    religionValueToName: new Map<number, string>(),
    nationalityValueToName: new Map<number, string>(),
  };

  ngOnInit(): void {
    this.loadEnumsAndStudent();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    
    // Cleanup blob URL if exists
    if (this.photoUrl) {
      try {
        const urlString = (this.photoUrl as any).changingThisBreaksApplicationSecurity;
        if (urlString && urlString.startsWith('blob:')) {
          URL.revokeObjectURL(urlString);
        }
      } catch (error) {
        // Silently handle cleanup errors
      }
    }
  }

  private loadEnumsAndStudent(): void {
    this.isEnumLoading = true;

    const enumRequests = {
      genders: this._enumService.getGenders().pipe(catchError(() => of([]))),
      studentStatuses: this._enumService.getStudentStatuses().pipe(catchError(() => of([]))),
      cbcLevels: this._enumService.getCBCLevels().pipe(catchError(() => of([]))),
      religions: this._enumService.getReligions().pipe(catchError(() => of([]))),
      nationalities: this._enumService.getNationalities().pipe(catchError(() => of([]))),
    };

    forkJoin(enumRequests)
      .pipe(
        takeUntil(this._unsubscribe),
        finalize(() => {
          this.isEnumLoading = false;
        })
      )
      .subscribe({
        next: (results) => {
          this.mapEnums(results);
          this.loadStudent();
        },
        error: (err) => {
          console.error('Error loading enums:', err);
          this._showError('Failed to load configuration data');
          this.isEnumLoading = false;
          // Continue loading student even if enums fail
          this.loadStudent();
        }
      });
  }

  private mapEnums(results: any): void {
    results.genders?.forEach((item: EnumItemDto) => {
      if (item.value !== undefined && item.name) {
        this.enumMaps.genderValueToName.set(item.value, item.name);
      }
    });

    results.studentStatuses?.forEach((item: EnumItemDto) => {
      if (item.value !== undefined && item.name) {
        this.enumMaps.studentStatusValueToName.set(item.value, item.name);
      }
    });

    results.cbcLevels?.forEach((item: EnumItemDto) => {
      if (item.value !== undefined && item.name) {
        this.enumMaps.cbcLevelValueToName.set(item.value, item.name);
      }
    });

    results.religions?.forEach((item: EnumItemDto) => {
      if (item.value !== undefined && item.name) {
        this.enumMaps.religionValueToName.set(item.value, item.name);
      }
    });

    results.nationalities?.forEach((item: EnumItemDto) => {
      if (item.value !== undefined && item.name) {
        this.enumMaps.nationalityValueToName.set(item.value, item.name);
      }
    });
  }

  private loadStudent(): void {
    const studentId = this._route.snapshot.paramMap.get('id');
    if (!studentId) {
      console.error('No student ID in route');
      this._showError('Invalid student ID');
      this._router.navigate(['/academic/students']);
      return;
    }

    console.log('Loading student:', studentId);
    this.isLoading = true;
    
    this._service.getById(studentId)
      .pipe(
        takeUntil(this._unsubscribe),
        catchError((err) => {
          console.error('Error loading student:', err);
          this._showError(err.error?.message || 'Failed to load student details');
          this.isLoading = false;
          this._router.navigate(['/academic/students']);
          return of(null);
        }),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: (student) => {
          if (!student) {
            console.error('No student data returned');
            return;
          }
          
          console.log('Student loaded:', student);
          this.student = student;
          this.breadcrumbs[3] = { label: student.fullName || 'Details' };
          
          if (student.photoUrl) {
            this.loadPhoto(student.photoUrl);
          }
        },
        error: (err) => {
          console.error('Subscription error:', err);
          this._showError(err.error?.message || 'Failed to load student details');
        }
      });
  }

  private loadPhoto(photoUrl: string): void {
    this.photoLoading = true;
    this.photoError = false;

    const url = photoUrl.startsWith('http')
      ? photoUrl
      : `${this._apiBaseUrl}${photoUrl}`;

    const token = this._authService.accessToken;
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;

    this._http.get(url, {
      responseType: 'blob',
      headers
    }).pipe(
      takeUntil(this._unsubscribe),
      catchError((error) => {
        console.error('Photo load error:', error);
        this.photoError = true;
        this.photoLoading = false;
        return of(null);
      }),
      finalize(() => {
        this.photoLoading = false;
      })
    ).subscribe((blob: any) => {
      if (blob && blob.size > 0) {
        try {
          const blobUrl = URL.createObjectURL(blob);
          this.photoUrl = this._sanitizer.bypassSecurityTrustUrl(blobUrl);
          this.photoError = false;
        } catch (error) {
          console.error('Error creating blob URL:', error);
          this.photoError = true;
        }
      } else {
        this.photoError = true;
      }
    });
  }

  retryPhotoLoad(): void {
    if (this.student?.photoUrl) {
      this.loadPhoto(this.student.photoUrl);
    }
  }

  viewFullPhoto(): void {
    if (!this.student?.photoUrl) return;

    const backendUrl = this.student.photoUrl.startsWith('http')
      ? this.student.photoUrl
      : `${this._apiBaseUrl}${this.student.photoUrl}`;

    const token = this._authService.accessToken;

    this._dialog.open(PhotoViewerDialogComponent, {
      disableClose: false,
      data: {
        photoUrl: backendUrl,
        studentName: this.student.fullName,
        admissionNumber: this.student.admissionNumber,
        authToken: token,
        additionalInfo: [
          this.getEnumName('gender', this.student.gender),
          this.getEnumName('cbcLevel', this.student.cbcLevel),
          this.student.currentLevel || 'N/A'
        ].join(' • ')
      }
    });
  }

  getEnumName(type: 'gender' | 'studentStatus' | 'cbcLevel' | 'religion' | 'nationality', value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';

    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }

    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    const mapKey = `${type}ValueToName` as keyof EnumMaps;
    return this.enumMaps[mapKey].get(numValue) || value.toString();
  }

  get detailSections(): DetailSection[] {
    if (!this.student) return [];

    return [
      {
        title: 'Personal Information',
        icon: 'person',
        iconColor: 'text-blue-600',
        items: [
          { label: 'Full Name', value: this.student.fullName, icon: 'badge' },
          { label: 'First Name', value: this.student.firstName, icon: 'person' },
          { label: 'Middle Name', value: this.student.middleName, icon: 'person' },
          { label: 'Last Name', value: this.student.lastName, icon: 'person' },
          { label: 'Date of Birth', value: this.formatDate(this.student.dateOfBirth), icon: 'cake', type: 'date' },
          { label: 'Place of Birth', value: this.student.placeOfBirth, icon: 'location_on' },
          { label: 'Gender', value: this.getEnumName('gender', this.student.gender), icon: 'wc' },
          { label: 'Religion', value: this.student.religion, icon: 'church' },
          { label: 'Nationality', value: this.student.nationality, icon: 'flag' },
          { label: 'Birth Certificate No.', value: this.student.birthCertificateNumber, icon: 'badge', copyable: true },
        ]
      },
      {
        title: 'Academic Details',
        icon: 'school',
        iconColor: 'text-indigo-600',
        items: [
          { label: 'Admission Number', value: this.student.admissionNumber, icon: 'confirmation_number', copyable: true, type: 'badge' },
          { label: 'NEMIS Number', value: this.student.nemisNumber, icon: 'confirmation_number', copyable: true },
          { label: 'CBC Level', value: this.getEnumName('cbcLevel', this.student.cbcLevel), icon: 'stairs' },
          { label: 'Current Level', value: this.student.currentLevel, icon: 'school' },
          { label: 'Current Class', value: this.student.currentClassName, icon: 'class' },
          { label: 'Student Status', value: this.getEnumName('studentStatus', this.student.studentStatus), icon: 'info', type: 'badge' },
          { label: 'Admission Date', value: this.formatDate(this.student.dateOfAdmission), icon: 'event', type: 'date' },
          { label: 'Previous School', value: this.student.previousSchool, icon: 'school' },
          { label: 'Status', value: this.student.isActive ? 'Active' : 'Inactive', icon: 'check_circle', type: 'status' },
        ]
      },
      {
        title: 'Location & Contact',
        icon: 'contact_phone',
        iconColor: 'text-green-600',
        items: [
          { label: 'Home Address', value: this.student.homeAddress, icon: 'home' },
          { label: 'County', value: this.student.county, icon: 'map' },
          { label: 'Sub-County', value: this.student.subCounty, icon: 'location_city' },
        ]
      },
      {
        title: 'Primary Guardian',
        icon: 'family_restroom',
        iconColor: 'text-purple-600',
        items: [
          { label: 'Guardian Name', value: this.student.primaryGuardianName, icon: 'person' },
          { label: 'Relationship', value: this.student.primaryGuardianRelationship, icon: 'diversity_3' },
          { label: 'Phone', value: this.student.primaryGuardianPhone, icon: 'phone', type: 'phone', copyable: true },
          { label: 'Email', value: this.student.primaryGuardianEmail, icon: 'email', type: 'email', copyable: true },
          { label: 'Occupation', value: this.student.primaryGuardianOccupation, icon: 'work' },
          { label: 'Address', value: this.student.primaryGuardianAddress, icon: 'home' },
        ]
      },
      {
        title: 'Secondary Guardian',
        icon: 'person_add',
        iconColor: 'text-amber-600',
        items: [
          { label: 'Name', value: this.student.secondaryGuardianName, icon: 'person' },
          { label: 'Relationship', value: this.student.secondaryGuardianRelationship, icon: 'diversity_3' },
          { label: 'Phone', value: this.student.secondaryGuardianPhone, icon: 'phone', type: 'phone', copyable: true },
          { label: 'Email', value: this.student.secondaryGuardianEmail, icon: 'email', type: 'email', copyable: true },
          { label: 'Occupation', value: this.student.secondaryGuardianOccupation, icon: 'work' },
        ]
      },
      {
        title: 'Emergency Contact',
        icon: 'emergency',
        iconColor: 'text-orange-600',
        items: [
          { label: 'Contact Name', value: this.student.emergencyContactName, icon: 'person' },
          { label: 'Phone', value: this.student.emergencyContactPhone, icon: 'phone', type: 'phone', copyable: true },
          { label: 'Relationship', value: this.student.emergencyContactRelationship, icon: 'diversity_3' },
        ]
      },
      {
        title: 'Medical & Health',
        icon: 'medical_services',
        iconColor: 'text-red-600',
        items: [
          { label: 'Blood Group', value: this.student.bloodGroup, icon: 'bloodtype' },
          { label: 'Allergies', value: this.student.allergies, icon: 'warning' },
          { label: 'Medical Conditions', value: this.student.medicalConditions, icon: 'health_and_safety' },
          { label: 'Special Needs', value: this.student.specialNeeds, icon: 'accessible' },
          { label: 'Requires Special Support', value: this.formatBoolean(this.student.requiresSpecialSupport), icon: 'support', type: 'boolean' },
        ]
      },
      {
        title: 'Additional Information',
        icon: 'info',
        iconColor: 'text-teal-600',
        items: [
          { label: 'School', value: this.student.schoolName, icon: 'school' },
          { label: 'Academic Year', value: this.student.academicYearName, icon: 'calendar_today' },
          { label: 'Notes', value: this.student.notes, icon: 'note' },
          { label: 'Created', value: this.formatDate(this.student.createdAt), icon: 'schedule', type: 'date' },
          { label: 'Last Updated', value: this.formatDate(this.student.updatedAt), icon: 'update', type: 'date' },
        ]
      }
    ];
  }

  // TrackBy functions to prevent NG0956 error
  trackByTitle(index: number, section: DetailSection): string {
    return section.title;
  }

  trackByLabel(index: number, item: DetailItem): string {
    return item.label;
  }

  formatDate(date: string | Date | undefined | null): string {
    if (!date) return '—';
    try {
      const d = new Date(date);
      if (isNaN(d.getTime())) return '—';
      return d.toLocaleDateString('en-US', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric' 
      });
    } catch {
      return '—';
    }
  }

  formatBoolean(value: boolean | undefined | null): string {
    if (value === undefined || value === null) return '—';
    return value ? 'Yes' : 'No';
  }

  copyToClipboard(value: string | number | undefined | null): void {
    if (!value) return;

    navigator.clipboard.writeText(value.toString()).then(() => {
      this._showSuccess('Copied to clipboard');
    }).catch(() => {
      this._showError('Failed to copy');
    });
  }

  editStudent(): void {
    if (this.student) {
      this._router.navigate(['/academic/students/edit', this.student.id]);
    }
  }

  toggleActive(): void {
    if (!this.student) return;

    const newStatus = !this.student.isActive;
    const action = newStatus ? 'activate' : 'deactivate';

    const confirmation = this._confirmation.open({
      title: `${newStatus ? 'Activate' : 'Deactivate'} Student`,
      message: `Are you sure you want to ${action} ${this.student.fullName}?`,
      icon: {
        name: newStatus ? 'check_circle' : 'block',
        color: newStatus ? 'success' : 'warn',
      },
      actions: {
        confirm: {
          label: newStatus ? 'Activate' : 'Deactivate',
          color: newStatus ? 'primary' : 'warn',
        },
        cancel: {
          label: 'Cancel',
        },
      },
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed' && this.student) {
        const payload: Partial<StudentDto> = { isActive: newStatus };

        this._service.updatePartial(this.student.id, payload)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: () => {
              this._showSuccess(`Student ${action}d successfully`);
              this.loadStudent();
            },
            error: (err) => {
              this._showError(err.error?.message || `Failed to ${action} student`);
            }
          });
      }
    });
  }

  deleteStudent(): void {
    if (!this.student) return;

    const confirmation = this._confirmation.open({
      title: 'Delete Student',
      message: `Are you sure you want to delete ${this.student.fullName}? This action cannot be undone.`,
      icon: {
        name: 'delete',
        color: 'warn',
      },
      actions: {
        confirm: {
          label: 'Delete',
          color: 'warn',
        },
        cancel: {
          label: 'Cancel',
        },
      },
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed' && this.student) {
        this._service.delete(this.student.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: () => {
              this._showSuccess('Student deleted successfully');
              this._router.navigate(['/academic/students']);
            },
            error: (err) => {
              this._showError(err.error?.message || 'Failed to delete student');
            }
          });
      }
    });
  }

  uploadPhoto(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    
    input.onchange = (event: Event) => {
      const target = event.target as HTMLInputElement;
      if (!target.files?.length || !this.student) return;

      const file = target.files[0];

      if (file.size > 5 * 1024 * 1024) {
        this._showError('File size must be less than 5MB');
        return;
      }

      if (!file.type.startsWith('image/')) {
        this._showError('File must be an image');
        return;
      }

      this._service.uploadPhoto(this.student.id, file)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: () => {
            this._showSuccess('Photo uploaded successfully');
            this.loadStudent();
          },
          error: (err) => {
            this._showError(err.error?.message || 'Failed to upload photo');
          }
        });
    };

    input.click();
  }

  goBack(): void {
    this._router.navigate(['/academic/students']);
  }

  private _showSuccess(message: string): void {
    this._snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['bg-green-600', 'text-white']
    });
  }

  private _showError(message: string): void {
    this._snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['bg-red-600', 'text-white']
    });
  }
}