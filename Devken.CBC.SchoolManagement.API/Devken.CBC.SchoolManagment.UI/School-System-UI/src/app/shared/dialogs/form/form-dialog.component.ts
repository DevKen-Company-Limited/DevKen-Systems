import { Component, Input, Output, EventEmitter, TemplateRef, ContentChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

// ── Dialog Header Configuration ─────────────────────────────────────────────
export interface DialogHeader {
  /** Title text */
  title: string;
  /** Subtitle/description text */
  subtitle?: string;
  /** Material icon name */
  icon?: string;
  /** Gradient background classes (default: indigo-violet-purple) */
  gradient?: string;
}

// ── Dialog Tab Definition ───────────────────────────────────────────────────
export interface DialogTab {
  /** Unique identifier */
  id: string;
  /** Display label */
  label: string;
  /** Material icon name */
  icon: string;
  /** Fields in this tab (for error validation) */
  fields?: string[];
}

// ── Photo Upload Configuration ──────────────────────────────────────────────
export interface PhotoUploadConfig {
  /** Whether to show photo upload section */
  enabled: boolean;
  /** Current photo URL (for edit mode) */
  photoUrl?: string;
  /** Preview of selected photo */
  preview?: string;
  /** Label text */
  label?: string;
  /** Description text */
  description?: string;
  /** Button text */
  buttonText?: string;
  /** Change handler */
  onChange?: (file: File) => void;
  /** Remove handler */
  onRemove?: () => void;
}

// ── Dialog Footer Configuration ─────────────────────────────────────────────
export interface DialogFooter {
  /** Cancel button text */
  cancelText?: string;
  /** Submit button text */
  submitText?: string;
  /** Submit button icon */
  submitIcon?: string;
  /** Whether to show loading spinner */
  loading?: boolean;
  /** Loading text */
  loadingText?: string;
  /** Whether form is invalid (for error message) */
  showError?: boolean;
  /** Custom error message */
  errorMessage?: string;
}

/**
 * Reusable Form Dialog Component
 * 
 * @example
 * <app-form-dialog
 *   [header]="dialogHeader"
 *   [tabs]="dialogTabs"
 *   [form]="form"
 *   [activeTab]="activeTab"
 *   [photoConfig]="photoConfig"
 *   [footer]="footerConfig"
 *   (tabChange)="onTabChange($event)"
 *   (cancel)="onCancel()"
 *   (submit)="onSubmit()">
 *   
 *   <!-- Tab content templates -->
 *   <ng-template #tab0>...</ng-template>
 *   <ng-template #tab1>...</ng-template>
 * </app-form-dialog>
 */
@Component({
  selector: 'app-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './form-dialog.component.html',
})
export class FormDialogComponent {
  // ── Inputs ───────────────────────────────────────────────────────────────────
  
  /** Dialog header configuration */
  @Input() header!: DialogHeader;
  
  /** Array of tab definitions */
  @Input() tabs: DialogTab[] = [];
  
  /** Form group reference */
  @Input() form!: FormGroup;
  
  /** Active tab index */
  @Input() activeTab = 0;
  
  /** Photo upload configuration */
  @Input() photoConfig?: PhotoUploadConfig;
  
  /** Footer configuration */
  @Input() footer: DialogFooter = {
    cancelText: 'Cancel',
    submitText: 'Save',
    submitIcon: 'save',
    loading: false,
    loadingText: 'Saving...',
    showError: false,
    errorMessage: 'Please fix all errors before saving.',
  };
  
  /** Map of tab ID to content template */
  @Input() tabTemplates: { [tabId: string]: TemplateRef<any> } = {};
  
  /** Whether form has been submitted (for validation) */
  @Input() formSubmitted = false;
  
  /** Whether to show tab error indicators */
  @Input() showTabErrors = true;
  
  /** Custom CSS classes for dialog container */
  @Input() containerClass = '';
  
  /** Maximum height for dialog content */
  @Input() maxHeight = 'max-h-screen';
  
  // ── Outputs ──────────────────────────────────────────────────────────────────
  
  /** Emitted when tab changes */
  @Output() tabChange = new EventEmitter<number>();
  
  /** Emitted when cancel button is clicked */
  @Output() cancel = new EventEmitter<void>();
  
  /** Emitted when submit button is clicked */
  @Output() submit = new EventEmitter<void>();
  
  /** Emitted when photo is selected */
  @Output() photoSelected = new EventEmitter<File>();
  
  /** Emitted when photo is removed */
  @Output() photoRemoved = new EventEmitter<void>();
  
  // ── Methods ──────────────────────────────────────────────────────────────────
  
  /**
   * Change active tab
   */
  onTabChange(index: number): void {
    this.activeTab = index;
    this.tabChange.emit(index);
  }
  
  /**
   * Check if tab has validation errors
   */
  tabHasErrors(tab: DialogTab): boolean {
    if (!this.showTabErrors || !this.formSubmitted || !tab.fields) {
      return false;
    }
    
    return tab.fields.some(field => {
      const control = this.form.get(field);
      return control && control.invalid;
    });
  }
  
  /**
   * Handle photo file selection
   */
  onPhotoChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    
    const file = input.files[0];
    
    if (this.photoConfig?.onChange) {
      this.photoConfig.onChange(file);
    }
    
    this.photoSelected.emit(file);
  }
  
  /**
   * Handle photo removal
   */
  onPhotoRemove(): void {
    if (this.photoConfig?.onRemove) {
      this.photoConfig.onRemove();
    }
    
    this.photoRemoved.emit();
  }
  
  /**
   * Handle cancel action
   */
  onCancel(): void {
    this.cancel.emit();
  }
  
  /**
   * Handle submit action
   */
  onSubmit(): void {
    this.submit.emit();
  }
  
  /**
   * Get header gradient classes
   */
  getHeaderGradient(): string {
    return this.header.gradient || 'bg-gradient-to-r from-indigo-700 via-violet-700 to-purple-700';
  }
  
  /**
   * Get photo display URL
   */
  getPhotoUrl(): string | null {
    if (!this.photoConfig) return null;
    return this.photoConfig.preview || this.photoConfig.photoUrl || null;
  }
  
  /**
   * TrackBy function for tabs
   */
  trackByTab = (index: number, tab: DialogTab): string => {
    return tab.id;
  };
}