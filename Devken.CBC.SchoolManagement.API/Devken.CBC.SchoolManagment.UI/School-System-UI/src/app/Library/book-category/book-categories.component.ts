import {
  Component, OnInit, OnDestroy, TemplateRef, ViewChild, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog }    from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule }   from '@angular/material/icon';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { AlertService }       from 'app/core/DevKenService/Alert/AlertService';
import { BookCategoryService } from 'app/core/DevKenService/Library/book-category.service';
import { BookCategoryResponseDto } from './Types/book-category.model';
import {
  BookCategoryDialogData,
  BookCategoryDialogComponent,
} from 'app/dialog-modals/Library/book-category-dialog/book-category-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';

@Component({
  selector: 'app-book-categories',
  standalone: true,
  imports: [
    CommonModule, MatButtonModule, MatIconModule,
    PageHeaderComponent, StatsCardsComponent, FilterPanelComponent,
    DataTableComponent, PaginationComponent,
  ],
  templateUrl: './book-categories.component.html',
})
export class BookCategoriesComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();

  @ViewChild('nameCell') nameCell!: TemplateRef<any>;
  @ViewChild('descCell') descCell!: TemplateRef<any>;

  allData:      BookCategoryResponseDto[] = [];
  filteredData: BookCategoryResponseDto[] = [];
  isLoading = false;

  currentPage  = 1;
  itemsPerPage = 10;
  showFilters  = false;
  private activeFilters: Record<string, any> = {};

  breadcrumbs: Breadcrumb[] = [
    { label: 'Library' },
    { label: 'Categories' },
  ];

  statsCards: StatCard[] = [];

  tableHeader: TableHeader = {
    title: 'Book Categories',
    subtitle: 'All registered book categories',
    icon: 'category',
    iconGradient: 'bg-gradient-to-br from-violet-500 via-purple-600 to-fuchsia-700',
  };

  tableColumns: TableColumn<BookCategoryResponseDto>[] = [
    { id: 'name',        label: 'Category Name', align: 'left',  sortable: true },
    { id: 'description', label: 'Description',   align: 'left',  hideOnMobile: true },
    { id: 'createdOn',   label: 'Created',        align: 'center', sortable: true, hideOnMobile: true },
  ];

  tableActions: TableAction<BookCategoryResponseDto>[] = [
    { id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'blue', handler: row => this.openEdit(row) },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red',  divider: true, handler: row => this.confirmDelete(row) },
  ];

  emptyState = {
    icon: 'category',
    message: 'No categories found',
    description: 'Create your first book category to get started.',
    action: { label: 'Add Category', icon: 'add', handler: () => this.openCreate() },
  };

  cellTemplates: { [k: string]: TemplateRef<any> } = {};

  filterFields: FilterField[] = [
    { id: 'search', label: 'Search', type: 'text', placeholder: 'Category name...', value: '' },
  ];

  constructor(
    private service:      BookCategoryService,
    private dialog:       MatDialog,
    private alertService: AlertService,
    private cdr:          ChangeDetectorRef,
  ) {}

  ngOnInit(): void  { this.loadAll(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  ngAfterViewInit(): void {
    this.cellTemplates = { name: this.nameCell, description: this.descCell };
    this.cdr.detectChanges();
  }

  loadAll(): void {
    this.isLoading = true;
    this.service.getAll().pipe(take(1)).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) { this.allData = res.data; this.applyFilters(); this.buildStats(); }
        else this.alertService.error('Error', res.message);
      },
      error: err => { this.isLoading = false; this.alertService.error('Error', err?.error?.message ?? 'Failed to load categories.'); },
    });
  }

  get paginatedData(): BookCategoryResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  onFilterChange(event: FilterChangeEvent): void {
    this.activeFilters[event.filterId] = event.value;
    this.currentPage = 1;
    this.applyFilters();
  }

  onClearFilters(): void {
    this.activeFilters = {};
    this.filterFields.forEach(f => f.value = '');
    this.currentPage = 1;
    this.applyFilters();
  }

  private applyFilters(): void {
    let data = [...this.allData];
    const search = (this.activeFilters['search'] ?? '').toLowerCase().trim();
    if (search) data = data.filter(r => r.name.toLowerCase().includes(search));
    this.filteredData = data;
  }

  private buildStats(): void {
    const total    = this.allData.length;
    const withDesc = this.allData.filter(r => r.description).length;
    this.statsCards = [
      { label: 'Total Categories',   value: total,    icon: 'category',    iconColor: 'violet' },
      { label: 'With Description',   value: withDesc, icon: 'description', iconColor: 'indigo' },
    ];
  }

  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  onColumnHeaderClick(event: { column: TableColumn<BookCategoryResponseDto> }): void {
    const col = event.column.id as keyof BookCategoryResponseDto;
    this.filteredData = [...this.filteredData].sort((a, b) =>
      String((a as any)[col] ?? '').localeCompare(String((b as any)[col] ?? ''), undefined, { numeric: true })
    );
    this.currentPage = 1;
  }

  onRowClick(_row: BookCategoryResponseDto): void {}

  openCreate(): void {
    const data: BookCategoryDialogData = { mode: 'create' };
    this.dialog.open(BookCategoryDialogComponent, { data, width: '480px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  openEdit(item: BookCategoryResponseDto): void {
    const data: BookCategoryDialogData = { mode: 'edit', item };
    this.dialog.open(BookCategoryDialogComponent, { data, width: '480px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  confirmDelete(item: BookCategoryResponseDto): void {
    this.alertService.confirm({
      title: 'Delete Category',
      message: `Delete "${item.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Category deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed to delete category'),
        });
      },
    });
  }
}