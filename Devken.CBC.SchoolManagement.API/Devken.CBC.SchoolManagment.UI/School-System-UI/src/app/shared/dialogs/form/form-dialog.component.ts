import { Component, Input, Output, EventEmitter, TemplateRef, ContentChild, ViewEncapsulation, InjectionToken, inject } from '@angular/core';
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
  /** Custom icon template (overrides icon string) */
  iconTemplate?: TemplateRef<any>;
  /** Gradient background classes (default: indigo-violet-purple) */
  gradient?: string;
  /** Custom CSS classes for header */
  class?: string;
  /** Header height */
  height?: string;
  /** Show close button */
  showCloseButton?: boolean;
}

// ── Dialog Tab Definition ───────────────────────────────────────────────────
export interface DialogTab {
  /** Unique identifier */
  id: string;
  /** Display label */
  label: string;
  /** Material icon name */
  icon: string;
  /** Custom icon template (overrides icon string) */
  iconTemplate?: TemplateRef<any>;
  /** Fields in this tab (for error validation) */
  fields?: string[];
  /** Whether tab is disabled */
  disabled?: boolean;
  /** Whether to show tab */
  hidden?: boolean;
  /** Custom CSS classes for tab */
  class?: string;
  /** Badge text */
  badge?: string;
  /** Badge color (default: red) */
  badgeColor?: string;
}

// ── Photo Upload Configuration ──────────────────────────────────────────────
export interface PhotoUploadConfig {
  /** Whether to show photo upload section */
  enabled: boolean;
  /** Current photo URL (for edit mode) */
  photoUrl?: string;
  /** Preview of selected photo */
  preview?: string | null;
  /** Label text */
  label?: string;
  /** Description text */
  description?: string;
  /** Button text */
  buttonText?: string;
  /** Custom upload button template */
  buttonTemplate?: TemplateRef<any>;
  /** Custom preview template */
  previewTemplate?: TemplateRef<any>;
  /** Change handler */
  onChange?: (file: File) => void;
  /** Remove handler */
  onRemove?: () => void;
  /** Accepted file types */
  accept?: string;
  /** Max file size in MB */
  maxSize?: number;
  /** Shape: 'circle' | 'square' | 'rounded' */
  shape?: 'circle' | 'square' | 'rounded';
  /** Preview size in pixels */
  previewSize?: number;
  /** Show remove button */
  showRemoveButton?: boolean;
  /** Custom CSS classes */
  class?: string;
}

// ── Dialog Footer Configuration ─────────────────────────────────────────────
export interface DialogFooter {
  /** Cancel button text */
  cancelText?: string;
  /** Submit button text */
  submitText?: string;
  /** Submit button icon */
  submitIcon?: string;
  /** Custom cancel button template */
  cancelButtonTemplate?: TemplateRef<any>;
  /** Custom submit button template */
  submitButtonTemplate?: TemplateRef<any>;
  /** Whether to show loading spinner */
  loading?: boolean;
  /** Loading text */
  loadingText?: string;
  /** Whether form is invalid (for error message) */
  showError?: boolean;
  /** Custom error message */
  errorMessage?: string;
  /** Error message icon */
  errorIcon?: string;
  /** Position of error message: 'left' | 'right' | 'center' */
  errorPosition?: 'left' | 'right' | 'center';
  /** Custom error message template */
  errorTemplate?: TemplateRef<any>;
  /** Whether to show cancel button */
  showCancel?: boolean;
  /** Whether to show submit button */
  showSubmit?: boolean;
  /** Disable cancel button when loading */
  disableCancelOnLoading?: boolean;
  /** Custom CSS classes for footer */
  class?: string;
}

// ── Dialog Size Configuration ───────────────────────────────────────────────
export interface DialogSize {
  /** Width (Tailwind or custom class) */
  width?: string;
  /** Height (Tailwind or custom class) */
  height?: string;
  /** Max width (Tailwind or custom class) */
  maxWidth?: string;
  /** Max height (Tailwind or custom class) */
  maxHeight?: string;
  /** Min width (Tailwind or custom class) */
  minWidth?: string;
  /** Min height (Tailwind or custom class) */
  minHeight?: string;
}

// ── Dialog Theme Configuration ──────────────────────────────────────────────
export interface DialogTheme {
  /** Header theme */
  header?: {
    background?: string;
    textColor?: string;
    iconBackground?: string;
    iconColor?: string;
    closeButtonColor?: string;
    closeButtonHoverColor?: string;
  };
  /** Tab theme */
  tabs?: {
    containerBackground?: string;
    borderColor?: string;
    activeTab?: {
      borderColor?: string;
      textColor?: string;
      backgroundColor?: string;
    };
    inactiveTab?: {
      borderColor?: string;
      textColor?: string;
      backgroundColor?: string;
      hoverTextColor?: string;
    };
  };
  /** Content theme */
  content?: {
    background?: string;
  };
  /** Footer theme */
  footer?: {
    background?: string;
    borderColor?: string;
    textColor?: string;
  };
  /** Photo upload theme */
  photo?: {
    containerBackground?: string;
    borderColor?: string;
    previewBorderColor?: string;
    removeButtonBackground?: string;
    removeButtonHoverBackground?: string;
    removeButtonIconColor?: string;
  };
}

/**
 * Reusable Form Dialog Component with extensive customization
 * 
 * @example
 * <app-form-dialog
 *   [header]="dialogHeader"
 *   [tabs]="dialogTabs"
 *   [form]="form"
 *   [activeTab]="activeTab"
 *   [photoConfig]="photoConfig"
 *   [footer]="footerConfig"
 *   [size]="dialogSize"
 *   [theme]="dialogTheme"
 *   (tabChange)="onTabChange($event)"
 *   (cancel)="onCancel()"
 *   (submit)="onSubmit()">
 *   
 *   <!-- Custom header icon template -->
 *   <ng-template #customHeaderIcon>
 *     <svg class="w-6 h-6">...</svg>
 *   </ng-template>
 *   
 *   <!-- Tab content templates -->
 *   <ng-template #personalTab>...</ng-template>
 *   <ng-template #contactTab>...</ng-template>
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
//   styleUrls: ['./form-dialog.component.scss'],
  encapsulation: ViewEncapsulation.None, // Allow custom styling from parent
})
export class FormDialogComponent {
  // ── Core Inputs ─────────────────────────────────────────────────────────────
  
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
    errorIcon: 'error_outline',
    errorPosition: 'left',
    showCancel: true,
    showSubmit: true,
    disableCancelOnLoading: true,
  };
  
  /** Map of tab ID to content template */
  @Input() tabTemplates: { [tabId: string]: TemplateRef<any> } = {};
  
  /** Whether form has been submitted (for validation) */
  @Input() formSubmitted = false;
  
  /** Whether to show tab error indicators */
  @Input() showTabErrors = true;
  
  /** Custom CSS classes for dialog container */
  @Input() containerClass = '';
  
  /** Dialog size configuration */
  @Input() size: DialogSize = {
    width: 'w-full',
    maxWidth: 'max-w-3xl',
    height: 'h-auto',
    maxHeight: 'max-h-screen',
  };
  
  /** Dialog theme configuration */
  @Input() theme: DialogTheme = {};
  
  /** Whether dialog is embedded (not in modal) */
  @Input() embedded = false;
  
  /** Whether to show scroll indicators */
  @Input() showScrollIndicators = false;
  
  /** Custom content header template */
  @Input() contentHeaderTemplate?: TemplateRef<any>;
  
  /** Custom content footer template */
  @Input() contentFooterTemplate?: TemplateRef<any>;
  
  /** Custom empty state template */
  @Input() emptyStateTemplate?: TemplateRef<any>;
  
  // ── ContentChild Templates for Overrides ────────────────────────────────────
  
  /** Custom header template */
  @ContentChild('headerTemplate') headerTemplate?: TemplateRef<any>;
  
  /** Custom tabs template */
  @ContentChild('tabsTemplate') tabsTemplate?: TemplateRef<any>;
  
  /** Custom photo upload template */
  @ContentChild('photoUploadTemplate') photoUploadTemplate?: TemplateRef<any>;
  
  /** Custom footer template */
  @ContentChild('footerTemplate') footerTemplate?: TemplateRef<any>;
  
  /** Custom error message template */
  @ContentChild('errorTemplate') errorTemplate?: TemplateRef<any>;
  
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
  
  /** Emitted when form field is touched */
  @Output() fieldTouched = new EventEmitter<string>();
  
  /** Emitted when dialog is scrolled to top/bottom */
  @Output() scrollState = new EventEmitter<{ isTop: boolean; isBottom: boolean }>();
  
  // ── Properties ───────────────────────────────────────────────────────────────
  
  /** Track scroll position */
  isScrolledToTop = true;
  isScrolledToBottom = false;
  
  // ── Methods ──────────────────────────────────────────────────────────────────
  
  /**
   * Change active tab
   */
  onTabChange(index: number): void {
    if (this.tabs[index]?.disabled) return;
    
    this.activeTab = index;
    this.tabChange.emit(index);
  }
  
  /**
   * Check if tab has validation errors
   */
  tabHasErrors(tab: DialogTab): boolean {
    if (!this.showTabErrors || !this.formSubmitted || !tab.fields || !this.form) {
      return false;
    }
    
    return tab.fields.some(field => {
      const control = this.form.get(field);
      return control && control.invalid && (control.dirty || control.touched || this.formSubmitted);
    });
  }
  
  /**
   * Check if any field in tab is touched
   */
  tabIsTouched(tab: DialogTab): boolean {
    if (!tab.fields || !this.form) return false;
    
    return tab.fields.some(field => {
      const control = this.form.get(field);
      return control && (control.dirty || control.touched);
    });
  }
  
  /**
   * Get tab CSS classes
   */
  getTabClasses(tab: DialogTab, index: number): string {
    const classes = [
      'flex items-center gap-2 px-5 py-3 text-sm font-semibold whitespace-nowrap transition-all border-b-2 -mb-px',
      tab.class || ''
    ];
    
    const isActive = this.activeTab === index;
    const theme = this.theme.tabs;
    
    if (isActive) {
      classes.push(theme?.activeTab?.borderColor || 'border-indigo-600');
      classes.push(theme?.activeTab?.textColor || 'text-indigo-600');
      classes.push(theme?.activeTab?.backgroundColor || '');
    } else {
      classes.push(theme?.inactiveTab?.borderColor || 'border-transparent');
      classes.push(theme?.inactiveTab?.textColor || 'text-gray-500 dark:text-gray-400');
      classes.push(theme?.inactiveTab?.backgroundColor || '');
      classes.push(theme?.inactiveTab?.hoverTextColor || 'hover:text-gray-700 dark:hover:text-gray-300');
    }
    
    if (tab.disabled) {
      classes.push('opacity-50 cursor-not-allowed');
    }
    
    return classes.join(' ');
  }
  
  /**
   * Handle photo file selection
   */
  onPhotoChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    
    const file = input.files[0];
    const maxSize = this.photoConfig?.maxSize || 5; // Default 5MB
    
    if (file.size > maxSize * 1024 * 1024) {
      // Emit error - parent component should handle this
      console.error(`File size exceeds ${maxSize}MB limit`);
      return;
    }
    
    if (this.photoConfig?.onChange) {
      this.photoConfig.onChange(file);
    }
    
    this.photoSelected.emit(file);
    
    // Reset input
    input.value = '';
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
    if (this.theme.header?.background) {
      return this.theme.header.background;
    }
    return this.header.gradient || 'bg-gradient-to-r from-indigo-700 via-violet-700 to-purple-700';
  }
  
  /**
   * Get header CSS classes
   */
  getHeaderClasses(): string {
    const classes = ['flex items-center justify-between px-6 py-4 shrink-0'];
    
    if (this.header.class) {
      classes.push(this.header.class);
    }
    
    classes.push(this.getHeaderGradient());
    
    if (this.theme.header?.textColor) {
      classes.push(this.theme.header.textColor);
    }
    
    return classes.join(' ');
  }
  
  /**
   * Get photo display URL
   */
  getPhotoUrl(): string | null {
    if (!this.photoConfig) return null;
    return this.photoConfig.preview || this.photoConfig.photoUrl || null;
  }
  
  /**
   * Get photo container shape classes
   */
  getPhotoShapeClasses(): string {
    const shape = this.photoConfig?.shape || 'rounded';
    
    switch (shape) {
      case 'circle':
        return 'rounded-full';
      case 'square':
        return 'rounded-none';
      case 'rounded':
      default:
        return 'rounded-2xl';
    }
  }
  
  /**
   * Get photo preview size
   */
  getPhotoPreviewSize(): string {
    const size = this.photoConfig?.previewSize || 80;
    return `${size}px`;
  }
  
  /**
   * Handle scroll event
   */
  onScroll(event: Event): void {
    const element = event.target as HTMLElement;
    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight;
    const clientHeight = element.clientHeight;
    
    this.isScrolledToTop = scrollTop === 0;
    this.isScrolledToBottom = Math.abs(scrollHeight - clientHeight - scrollTop) < 1;
    
    this.scrollState.emit({
      isTop: this.isScrolledToTop,
      isBottom: this.isScrolledToBottom
    });
  }
  
  /**
   * TrackBy function for tabs
   */
  trackByTab = (index: number, tab: DialogTab): string => {
    return tab.id || index.toString();
  };
  
  /**
   * Check if footer cancel button should be disabled
   */
  isCancelDisabled(): boolean {
    return this.footer.disableCancelOnLoading && !!this.footer.loading;
  }
}