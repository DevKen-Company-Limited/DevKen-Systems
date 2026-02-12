import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

export interface Breadcrumb {
  label: string;
  url?: string;
}

/**
 * Reusable Page Header Component
 * 
 * A compact, visually appealing header with gradient background
 * that can be used across all pages in the application.
 * 
 * @example
 * ```html
 * <app-page-header
 *   title="Teachers Management"
 *   description="Manage all teaching staff"
 *   icon="groups"
 *   [breadcrumbs]="breadcrumbs"
 *   [actionTemplate]="headerActions">
 * </app-page-header>
 * 
 * <ng-template #headerActions>
 *   <button mat-flat-button (click)="openCreate()">
 *     <mat-icon>add</mat-icon>
 *     <span>Add Teacher</span>
 *   </button>
 * </ng-template>
 * ```
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './page-header.component.html',
  styleUrls: ['./page-header.component.scss']
})
export class PageHeaderComponent {
  /** Main title displayed in the header */
  @Input() title: string = '';
  
  /** Subtitle/description text below the title */
  @Input() description: string = '';
  
  /** Array of breadcrumb items for navigation */
  @Input() breadcrumbs: Breadcrumb[] = [];
  
  /** Material icon name to display before the title */
  @Input() icon: string = '';
  
  /** Template reference for action buttons (e.g., Add, Export, etc.) */
  @Input() actionTemplate?: TemplateRef<any>;
  
  /** Custom CSS class for gradient (defaults to purple/violet gradient) */
  @Input() gradientClass: string = '';
  
  /** Enable compact mode for even smaller header */
  @Input() compact: boolean = false;
  
  /** Emitted when a breadcrumb with a URL is clicked */
  @Output() breadcrumbClick = new EventEmitter<Breadcrumb>();

  /**
   * Handle breadcrumb click events
   */
  onBreadcrumbClick(breadcrumb: Breadcrumb): void {
    if (breadcrumb.url) {
      this.breadcrumbClick.emit(breadcrumb);
    }
  }
}