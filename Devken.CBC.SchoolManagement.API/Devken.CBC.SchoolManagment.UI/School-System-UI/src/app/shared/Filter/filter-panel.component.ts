import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

export interface FilterField {
  id: string;
  label: string;
  type: 'text' | 'select' | 'date' | 'dateRange' | 'custom';
  placeholder?: string;
  options?: FilterOption[];
  value?: any;
  customTemplate?: TemplateRef<any>;
}

export interface FilterOption {
  label: string;
  value: any;
}

export interface FilterChangeEvent {
  filterId: string;
  value: any;
}

/**
 * Reusable Filter Panel Component
 * 
 * A flexible filter panel that supports text search, dropdowns, dates,
 * and custom filter types.
 * 
 * @example
 * ```html
 * <app-filter-panel
 *   [fields]="filterFields"
 *   [showPanel]="showFilters"
 *   (filterChange)="onFilterChange($event)"
 *   (clearAll)="onClearFilters()"
 *   (togglePanel)="showFilters = !showFilters">
 * </app-filter-panel>
 * ```
 */
@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './filter-panel.component.html',
  styleUrls: ['./filter-panel.component.scss']
})
export class FilterPanelComponent {
  /** Array of filter field configurations */
  @Input() fields: FilterField[] = [];
  
  /** Whether the filter panel is visible */
  @Input() showPanel: boolean = false;
  
  /** Number of columns for filter fields (1-4) */
  @Input() columns: number = 4;
  
  /** Show/hide the toggle button */
  @Input() showToggleButton: boolean = true;
  
  /** Custom toggle button text */
  @Input() toggleButtonText?: string;
  
  /** Emitted when any filter value changes */
  @Output() filterChange = new EventEmitter<FilterChangeEvent>();
  
  /** Emitted when clear all filters is clicked */
  @Output() clearAll = new EventEmitter<void>();
  
  /** Emitted when toggle panel button is clicked */
  @Output() togglePanel = new EventEmitter<void>();

  /**
   * Get grid column classes based on columns input
   */
  get gridColsClass(): string {
    const colsMap: { [key: number]: string } = {
      1: 'grid-cols-1',
      2: 'grid-cols-1 md:grid-cols-2',
      3: 'grid-cols-1 md:grid-cols-3',
      4: 'grid-cols-1 md:grid-cols-4',
    };
    return colsMap[this.columns] || colsMap[4];
  }

  /**
   * Get toggle button text
   */
  get toggleText(): string {
    if (this.toggleButtonText) return this.toggleButtonText;
    return this.showPanel ? 'Hide Filters' : 'Show Filters';
  }

  /**
   * Handle filter value change
   */
  onValueChange(field: FilterField, value: any): void {
    field.value = value;
    this.filterChange.emit({ filterId: field.id, value });
  }

  /**
   * Handle clear all filters click
   */
  onClearAllClick(): void {
    this.fields.forEach(field => field.value = field.type === 'select' ? 'all' : '');
    this.clearAll.emit();
  }

  /**
   * Handle toggle panel click
   */
  onToggleClick(): void {
    this.togglePanel.emit();
  }
}