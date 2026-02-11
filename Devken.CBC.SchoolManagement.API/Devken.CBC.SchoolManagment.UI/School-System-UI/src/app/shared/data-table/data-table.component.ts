import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

// ── Column Definition ────────────────────────────────────────────────────────
export interface TableColumn<T = any> {
  /** Unique identifier for the column */
  id: string;
  /** Display label in header */
  label: string;
  /** Alignment of content (default: 'left') */
  align?: 'left' | 'center' | 'right';
  /** Whether column is sortable */
  sortable?: boolean;
  /** Whether to show this column on mobile */
  hideOnMobile?: boolean;
  /** Whether to show this column on tablet */
  hideOnTablet?: boolean;
  /** Custom CSS classes for the header */
  headerClass?: string;
  /** Custom CSS classes for the cell */
  cellClass?: string;
}

// ── Action Definition ────────────────────────────────────────────────────────
export interface TableAction<T = any> {
  /** Unique identifier */
  id: string;
  /** Display label */
  label: string;
  /** Material icon name */
  icon: string;
  /** Color theme (Tailwind color) */
  color?: string;
  /** Whether to show a divider after this action */
  divider?: boolean;
  /** Whether action is visible for this row */
  visible?: (row: T) => boolean;
  /** Whether action is disabled for this row */
  disabled?: (row: T) => boolean;
  /** Click handler */
  handler: (row: T) => void;
}

// ── Empty State Configuration ────────────────────────────────────────────────
export interface TableEmptyState {
  /** Icon to display */
  icon: string;
  /** Primary message */
  message: string;
  /** Secondary description */
  description?: string;
  /** Optional action button */
  action?: {
    label: string;
    icon?: string;
    handler: () => void;
  };
}

// ── Table Header Configuration ───────────────────────────────────────────────
export interface TableHeader {
  /** Title of the table */
  title: string;
  /** Subtitle or description */
  subtitle?: string;
  /** Icon for the header */
  icon?: string;
  /** Gradient color scheme */
  iconGradient?: string;
}

/**
 * Reusable Data Table Component
 * 
 * @example
 * // Basic usage with cell templates
 * <app-data-table
 *   [columns]="columns"
 *   [data]="paginatedData"
 *   [actions]="actions"
 *   [loading]="isLoading"
 *   [header]="tableHeader"
 *   [emptyState]="emptyState"
 *   [cellTemplates]="cellTemplates">
 * </app-data-table>
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: './data-table.component.html',
})
export class DataTableComponent<T = any> {
  // ── Inputs ───────────────────────────────────────────────────────────────────
  
  /** Array of column definitions */
  @Input() columns: TableColumn<T>[] = [];
  
  /** Array of data to display */
  @Input() data: T[] = [];
  
  /** Array of row actions */
  @Input() actions: TableAction<T>[] = [];
  
  /** Loading state */
  @Input() loading = false;
  
  /** Table header configuration */
  @Input() header?: TableHeader;
  
  /** Empty state configuration */
  @Input() emptyState?: TableEmptyState;
  
  /** Whether to show the actions column */
  @Input() showActions = true;
  
  /** Map of column ID to custom cell template */
  @Input() cellTemplates: { [columnId: string]: TemplateRef<any> } = {};
  
  /** Function to get unique identifier for row (for trackBy) */
  @Input() rowIdentifier: (row: T) => any = (row: any) => row.id || row;
  
  /** Custom CSS classes for the table wrapper */
  @Input() wrapperClass = '';
  
  /** Whether to enable hover effect on rows */
  @Input() hoverEffect = true;
  
  /** Whether to show striped rows */
  @Input() striped = false;
  
  // ── Outputs ──────────────────────────────────────────────────────────────────
  
  /** Emitted when a row is clicked */
  @Output() rowClick = new EventEmitter<T>();
  
  /** Emitted when column header is clicked (for sorting) */
  @Output() columnHeaderClick = new EventEmitter<{ column: TableColumn<T>; data: T[] }>();
  
  // ── Methods ──────────────────────────────────────────────────────────────────
  
  /**
   * Get alignment class for column
   */
  getAlignmentClass(column: TableColumn<T>): string {
    const align = column.align || 'left';
    return align === 'left' ? 'text-left' : align === 'center' ? 'text-center' : 'text-right';
  }
  
  /**
   * Get responsive classes for column
   */
  getResponsiveClass(column: TableColumn<T>): string {
    const classes: string[] = [];
    if (column.hideOnMobile) classes.push('hidden sm:table-cell');
    if (column.hideOnTablet) classes.push('hidden md:table-cell');
    return classes.join(' ');
  }
  
  /**
   * Get value from row for a column
   */
  getCellValue(row: T, column: TableColumn<T>): any {
    return (row as any)[column.id];
  }
  
  /**
   * Check if action should be visible for row
   */
  isActionVisible(action: TableAction<T>, row: T): boolean {
    return action.visible ? action.visible(row) : true;
  }
  
  /**
   * Check if action should be disabled for row
   */
  isActionDisabled(action: TableAction<T>, row: T): boolean {
    return action.disabled ? action.disabled(row) : false;
  }
  
  /**
   * Get visible actions for a row
   */
  getVisibleActions(row: T): TableAction<T>[] {
    return this.actions.filter(action => this.isActionVisible(action, row));
  }
  
  /**
   * Handle action click
   */
  onActionClick(action: TableAction<T>, row: T, event: Event): void {
    event.stopPropagation();
    if (!this.isActionDisabled(action, row)) {
      action.handler(row);
    }
  }
  
  /**
   * Handle row click
   */
  onRowClick(row: T): void {
    this.rowClick.emit(row);
  }
  
  /**
   * Handle column header click
   */
  onColumnHeaderClick(column: TableColumn<T>): void {
    if (column.sortable) {
      this.columnHeaderClick.emit({ column, data: this.data });
    }
  }
  
  /**
   * Get row CSS classes
   */
  getRowClass(index: number): string {
    const classes: string[] = [];
    
    if (this.hoverEffect) {
      classes.push('hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors');
    }
    
    if (this.striped && index % 2 === 1) {
      classes.push('bg-gray-50/50 dark:bg-gray-800/30');
    }
    
    return classes.join(' ');
  }
  
  /**
   * Get action icon color classes
   */
  getActionColorClass(action: TableAction<T>): string {
    if (!action.color) return '';
    
    const colorMap: { [key: string]: string } = {
      'blue': 'text-blue-600 dark:text-blue-400',
      'violet': 'text-violet-600 dark:text-violet-400',
      'purple': 'text-purple-600 dark:text-purple-400',
      'green': 'text-green-600 dark:text-green-400',
      'amber': 'text-amber-600 dark:text-amber-400',
      'red': 'text-red-600 dark:text-red-400',
      'indigo': 'text-indigo-600 dark:text-indigo-400',
    };
    
    return colorMap[action.color] || '';
  }
  
  /**
   * TrackBy function for rows
   */
  trackByRow = (index: number, row: T): any => {
    return this.rowIdentifier(row);
  };
  
  /**
   * TrackBy function for columns
   */
  trackByColumn = (index: number, column: TableColumn<T>): string => {
    return column.id;
  };
  
  /**
   * TrackBy function for actions
   */
  trackByAction = (index: number, action: TableAction<T>): string => {
    return action.id;
  };
}