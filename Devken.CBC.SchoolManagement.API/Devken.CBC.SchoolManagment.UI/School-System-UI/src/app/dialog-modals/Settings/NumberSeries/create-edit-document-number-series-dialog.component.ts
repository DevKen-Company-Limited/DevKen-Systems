import { Component, Inject, OnInit, ViewChild, TemplateRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { 
  CreateDocumentNumberSeriesRequest, 
  DocumentNumberSeriesDto, 
  ENTITY_TYPES, 
  UpdateDocumentNumberSeriesRequest 
} from 'app/Settings/types/DocumentNumberSeries';
import { DialogFooter, DialogHeader, DialogSize, DialogTab, DialogTheme, FormDialogComponent } from 'app/shared/dialogs/form/form-dialog.component';
import { DocumentNumberSeriesService } from 'app/core/DevKenService/Settings/NumberSeries/DocumentNumberSeriesService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { UserService } from 'app/core/user/user.service';
import { finalize } from 'rxjs/operators';

export interface CreateEditDocumentNumberSeriesDialogData {
  mode: 'create' | 'edit';
  numberSeries?: DocumentNumberSeriesDto;
  tenantId?: string;
  isSuperAdmin?: boolean;
}

@Component({
  selector: 'app-create-edit-document-number-series-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    FormDialogComponent,
  ],
  templateUrl: './create-edit-document-number-series-dialog.component.html',
})
export class CreateEditDocumentNumberSeriesDialogComponent implements OnInit, AfterViewInit {
  @ViewChild('configTab', { static: true }) configTabTemplate!: TemplateRef<any>;
  @ViewChild('infoTab', { static: true }) infoTabTemplate!: TemplateRef<any>;

  form!: FormGroup;
  activeTab = 0;
  formSubmitted = false;
  entityTypes = ENTITY_TYPES;
  schools: SchoolDto[] = [];
  isSuperAdmin = false;
  isLoadingSchools = false;

  // Dialog configuration
  dialogHeader: DialogHeader = {
    title: '',
    subtitle: '',
    icon: 'pin',
    gradient: 'bg-gradient-to-r from-indigo-600 via-violet-600 to-purple-600',
    showCloseButton: true,
  };

  dialogTabs: DialogTab[] = [
    {
      id: 'config',
      label: 'Configuration',
      icon: 'settings',
      fields: ['entityName', 'prefix', 'padding', 'resetEveryYear'],
    },
    {
      id: 'info',
      label: 'Information',
      icon: 'info',
      fields: [], // No fields to validate
    },
  ];

  footerConfig: DialogFooter = {
    cancelText: 'Cancel',
    submitText: 'Save',
    submitIcon: 'save',
    loading: false,
    loadingText: 'Saving...',
    showError: true,
    errorMessage: 'Please fix all errors before saving.',
    errorIcon: 'error_outline',
    errorPosition: 'left',
    showCancel: true,
    showSubmit: true,
    disableCancelOnLoading: true,
  };

  dialogSize: DialogSize = {
    width: 'w-full',
    maxWidth: 'max-w-2xl',
    maxHeight: 'max-h-[90vh]',
  };

  dialogTheme: DialogTheme = {
    header: {
      textColor: 'text-white',
      iconBackground: 'bg-white/20',
      closeButtonColor: 'text-white/80',
      closeButtonHoverColor: 'hover:text-white hover:bg-white/10',
    },
    tabs: {
      containerBackground: 'bg-white dark:bg-gray-800',
      borderColor: 'border-gray-200 dark:border-gray-700',
      activeTab: {
        borderColor: 'border-indigo-600',
        textColor: 'text-indigo-600',
        backgroundColor: '',
      },
      inactiveTab: {
        borderColor: 'border-transparent',
        textColor: 'text-gray-500 dark:text-gray-400',
        backgroundColor: '',
        hoverTextColor: 'hover:text-gray-700 dark:hover:text-gray-300',
      },
    },
    content: {
      background: 'bg-gray-50 dark:bg-gray-900',
    },
    footer: {
      background: 'bg-white dark:bg-gray-800',
      borderColor: 'border-gray-200 dark:border-gray-700',
      textColor: 'text-gray-900 dark:text-gray-100',
    },
  };

  tabTemplates: { [key: string]: TemplateRef<any> } = {};

  constructor(
    private fb: FormBuilder,
    private service: DocumentNumberSeriesService,
    private schoolService: SchoolService,
    private userService: UserService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<CreateEditDocumentNumberSeriesDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditDocumentNumberSeriesDialogData
  ) {
    this.isSuperAdmin = this.data.isSuperAdmin || false;
  }

  ngOnInit(): void {
    this.initializeForm();
    this.updateHeaderForMode();
    
    if (this.shouldLoadSchools()) {
      this.loadSchools();
    }
    
    if (this.isEditMode && this.data.numberSeries) {
      this.patchFormForEdit();
    }

    // Set up form value changes for preview
    this.setupFormListeners();
  }

  ngAfterViewInit(): void {
    // Important: Assign templates after view is initialized
    this.tabTemplates = {
      config: this.configTabTemplate,
      info: this.infoTabTemplate,
    };
  }

  // Helper getters
  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  get entityNameControl() {
    return this.form.get('entityName');
  }

  get schoolIdControl() {
    return this.form.get('schoolId');
  }

  private shouldLoadSchools(): boolean {
    return this.isSuperAdmin && !this.isEditMode;
  }

  private initializeForm(): void {
    const formConfig: any = {
      entityName: [
        { value: '', disabled: this.isEditMode }, 
        [Validators.required]
      ],
      prefix: ['', [Validators.maxLength(10), Validators.pattern(/^[A-Za-z0-9]*$/)]],
      padding: [5, [Validators.required, Validators.min(1), Validators.max(10)]],
      resetEveryYear: [false],
    };

    // Add schoolId field for SuperAdmin in create mode
    if (this.shouldLoadSchools()) {
      formConfig.schoolId = ['', [Validators.required]];
    }

    this.form = this.fb.group(formConfig);
  }

  private setupFormListeners(): void {
    // Update preview in real-time
    this.form.valueChanges.subscribe(() => {
      // Force change detection for preview
    });
  }

  private updateHeaderForMode(): void {
    if (this.isEditMode) {
      this.dialogHeader.title = 'Edit Number Series';
      this.dialogHeader.subtitle = `Update configuration for ${this.data.numberSeries?.entityName || ''}`;
      this.footerConfig.submitText = 'Update';
      this.footerConfig.submitIcon = 'update';
    } else {
      this.dialogHeader.title = 'Create Number Series';
      this.dialogHeader.subtitle = 'Configure automatic number generation for documents';
      this.footerConfig.submitText = 'Create';
      this.footerConfig.submitIcon = 'add_circle';
    }
  }

  private patchFormForEdit(): void {
    if (this.data.numberSeries) {
      this.form.patchValue({
        entityName: this.data.numberSeries.entityName,
        prefix: this.data.numberSeries.prefix,
        padding: this.data.numberSeries.padding,
        resetEveryYear: this.data.numberSeries.resetEveryYear,
      });
    }
  }

  getPreview(): string {
    const prefix = this.form.get('prefix')?.value || '';
    const padding = this.form.get('padding')?.value || 5;
    
    // Generate a sample number based on current year if resetEveryYear is enabled
    const resetEveryYear = this.form.get('resetEveryYear')?.value;
    let sampleNumber = '1';
    
    if (resetEveryYear) {
      const currentYear = new Date().getFullYear();
      sampleNumber = `${currentYear}-1`;
    } else {
      sampleNumber = '1'.padStart(padding, '0');
    }
    
    return prefix ? `${prefix}${sampleNumber}` : sampleNumber;
  }

  onTabChange(index: number): void {
    this.activeTab = index;
  }

  loadSchools(): void {
    this.isLoadingSchools = true;
    this.schoolService.getAll()
      .pipe(
        finalize(() => this.isLoadingSchools = false)
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.schools = res.data.filter(school => school.isActive);
            
            // Auto-select if there's only one school
            if (this.schools.length === 1 && this.schoolIdControl) {
              this.schoolIdControl.setValue(this.schools[0].id);
            }
          }
        },
        error: (err) => {
          console.error('Failed to load schools:', err);
          this.snackBar.open('Failed to load schools. Please try again.', 'Close', {
            duration: 5000,
            panelClass: ['bg-red-600', 'text-white']
          });
        }
      });
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  onSubmit(): void {
    this.formSubmitted = true;
    
    // Mark all controls as touched to trigger validation
    this.form.markAllAsTouched();
    
    if (this.form.invalid) {
      // Find first invalid tab and switch to it
      const invalidTabIndex = this.findFirstInvalidTab();
      if (invalidTabIndex !== -1) {
        this.activeTab = invalidTabIndex;
      }
      
      this.snackBar.open('Please fix all errors before saving', 'Close', { 
        duration: 5000,
        panelClass: ['bg-red-600', 'text-white']
      });
      return;
    }

    this.footerConfig.loading = true;

    // Get form value including disabled controls
    const formValue = {
      ...this.form.getRawValue(),
    };

    if (this.isEditMode) {
      this.updateNumberSeries(formValue);
    } else {
      this.createNumberSeries(formValue);
    }
  }

  private findFirstInvalidTab(): number {
    for (let i = 0; i < this.dialogTabs.length; i++) {
      const tab = this.dialogTabs[i];
      if (tab.fields && tab.fields.length > 0) {
        const hasInvalidField = tab.fields.some(field => {
          const control = this.form.get(field);
          return control && control.invalid;
        });
        if (hasInvalidField) {
          return i;
        }
      }
    }
    return -1;
  }

  private createNumberSeries(formValue: any): void {
    const request: CreateDocumentNumberSeriesRequest = {
      tenantId: this.isSuperAdmin ? formValue.schoolId : (this.data.tenantId || ''),
      entityName: formValue.entityName,
      prefix: formValue.prefix || '',
      padding: formValue.padding,
      resetEveryYear: formValue.resetEveryYear,
    };

    this.service.create(request)
      .pipe(
        finalize(() => this.footerConfig.loading = false)
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.snackBar.open(res.message || 'Number series created successfully', 'Close', {
              duration: 3000,
              panelClass: ['bg-green-600', 'text-white']
            });
            this.dialogRef.close({ success: true, data: res.data });
          } else {
            this.snackBar.open(res.message || 'Failed to create number series', 'Close', {
              duration: 5000,
              panelClass: ['bg-red-600', 'text-white']
            });
          }
        },
        error: (err) => {
          console.error('Create error:', err);
          this.snackBar.open(
            err?.error?.message || 'An error occurred while creating number series', 
            'Close', 
            { duration: 5000, panelClass: ['bg-red-600', 'text-white'] }
          );
        }
      });
  }

  private updateNumberSeries(formValue: any): void {
    const request: UpdateDocumentNumberSeriesRequest = {
      prefix: formValue.prefix || '',
      padding: formValue.padding,
      resetEveryYear: formValue.resetEveryYear,
    };

    this.service.update(this.data.numberSeries!.id, request)
      .pipe(
        finalize(() => this.footerConfig.loading = false)
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.snackBar.open(res.message || 'Number series updated successfully', 'Close', {
              duration: 3000,
              panelClass: ['bg-green-600', 'text-white']
            });
            this.dialogRef.close({ success: true, data: res.data });
          } else {
            this.snackBar.open(res.message || 'Failed to update number series', 'Close', {
              duration: 5000,
              panelClass: ['bg-red-600', 'text-white']
            });
          }
        },
        error: (err) => {
          console.error('Update error:', err);
          this.snackBar.open(
            err?.error?.message || 'An error occurred while updating number series', 
            'Close', 
            { duration: 5000, panelClass: ['bg-red-600', 'text-white'] }
          );
        }
      });
  }
}