import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Reusable Pagination Component
 * 
 * Provides pagination controls with page information display.
 * 
 * @example
 * ```html
 * <app-pagination
 *   [currentPage]="currentPage"
 *   [totalItems]="filteredData.length"
 *   [itemsPerPage]="itemsPerPage"
 *   (pageChange)="onPageChange($event)"
 *   (itemsPerPageChange)="onItemsPerPageChange($event)">
 * </app-pagination>
 * ```
 */
@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent {
  /** Current active page (1-indexed) */
  @Input() currentPage: number = 1;
  
  /** Total number of items across all pages */
  @Input() totalItems: number = 0;
  
  /** Number of items to display per page */
  @Input() itemsPerPage: number = 10;
  
  /** Show items per page selector */
  @Input() showItemsPerPageSelector: boolean = false;
  
  /** Available items per page options */
  @Input() itemsPerPageOptions: number[] = [10, 25, 50, 100];
  
  /** Custom label for items (e.g., 'teachers', 'students', 'records') */
  @Input() itemLabel: string = 'items';
  
  /** Show page number buttons */
  @Input() showPageNumbers: boolean = false;
  
  /** Maximum page number buttons to show */
  @Input() maxPageButtons: number = 5;
  
  /** Emitted when page changes */
  @Output() pageChange = new EventEmitter<number>();
  
  /** Emitted when items per page changes */
  @Output() itemsPerPageChange = new EventEmitter<number>();

  /**
   * Calculate total number of pages
   */
  get totalPages(): number {
    return Math.ceil(this.totalItems / this.itemsPerPage);
  }

  /**
   * Calculate the starting item number for current page
   */
  get showingFrom(): number {
    if (this.totalItems === 0) return 0;
    return (this.currentPage - 1) * this.itemsPerPage + 1;
  }

  /**
   * Calculate the ending item number for current page
   */
  get showingTo(): number {
    return Math.min(this.currentPage * this.itemsPerPage, this.totalItems);
  }

  /**
   * Check if on first page
   */
  get isFirstPage(): boolean {
    return this.currentPage === 1;
  }

  /**
   * Check if on last page
   */
  get isLastPage(): boolean {
    return this.currentPage >= this.totalPages;
  }

  /**
   * Get array of page numbers to display
   */
  get pageNumbers(): number[] {
    const pages: number[] = [];
    const totalPages = this.totalPages;
    
    if (totalPages <= this.maxPageButtons) {
      // Show all pages if total is less than max
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Show ellipsis logic
      const halfMax = Math.floor(this.maxPageButtons / 2);
      let startPage = Math.max(1, this.currentPage - halfMax);
      let endPage = Math.min(totalPages, this.currentPage + halfMax);

      // Adjust if at start or end
      if (this.currentPage <= halfMax) {
        endPage = this.maxPageButtons;
      } else if (this.currentPage >= totalPages - halfMax) {
        startPage = totalPages - this.maxPageButtons + 1;
      }

      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
    }
    
    return pages;
  }

  /**
   * Navigate to previous page
   */
  previousPage(): void {
    if (!this.isFirstPage) {
      this.goToPage(this.currentPage - 1);
    }
  }

  /**
   * Navigate to next page
   */
  nextPage(): void {
    if (!this.isLastPage) {
      this.goToPage(this.currentPage + 1);
    }
  }

  /**
   * Navigate to first page
   */
  firstPage(): void {
    if (!this.isFirstPage) {
      this.goToPage(1);
    }
  }

  /**
   * Navigate to last page
   */
  lastPage(): void {
    if (!this.isLastPage) {
      this.goToPage(this.totalPages);
    }
  }

  /**
   * Navigate to specific page
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.pageChange.emit(page);
    }
  }

  /**
   * Handle items per page change
   */
  onItemsPerPageChange(value: number): void {
    this.itemsPerPageChange.emit(value);
  }
}