import {
  Component, OnInit, OnDestroy, TemplateRef, ViewChild, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog }    from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule }   from '@angular/material/icon';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { BookAuthorService } from 'app/core/DevKenService/Library/book-author.service';
import { BookAuthorResponseDto } from './Types/book-author.model';
import {
  BookAuthorDialogData,
  BookAuthorDialogComponent,
} from 'app/dialog-modals/Library/book-author-dialog/book-author-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';

@Component({
  selector: 'app-book-authors',
  standalone: true,
  imports: [
    CommonModule, MatButtonModule, MatIconModule,
    PageHeaderComponent, StatsCardsComponent, FilterPanelComponent,
    DataTableComponent, PaginationComponent,
  ],
  templateUrl: './book-authors.component.html',
})
export class BookAuthorsComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();

  @ViewChild('nameCell') nameCell!: TemplateRef<any>;
  @ViewChild('bioCell')  bioCell!: TemplateRef<any>;

  allData:      BookAuthorResponseDto[] = [];
  filteredData: BookAuthorResponseDto[] = [];
  isLoading = false;

  currentPage  = 1;
  itemsPerPage = 10;
  showFilters  = false;
  private activeFilters: Record<string, any> = {};

  breadcrumbs: Breadcrumb[] = [
    { label: 'Library' },
    { label: 'Authors' },
  ];

  statsCards: StatCard[] = [];

  tableHeader: TableHeader = {
    title: 'Book Authors',
    subtitle: 'All registered book authors',
    icon: 'person',
    iconGradient: 'bg-gradient-to-br from-emerald-500 via-teal-600 to-cyan-700',
  };

  tableColumns: TableColumn<BookAuthorResponseDto>[] = [
    { id: 'name',      label: 'Name',      align: 'left',   sortable: true },
    { id: 'biography', label: 'Biography', align: 'left',   hideOnMobile: true },
    { id: 'createdOn', label: 'Created',   align: 'center', sortable: true, hideOnMobile: true },
  ];

  tableActions: TableAction<BookAuthorResponseDto>[] = [
    { id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'blue', handler: row => this.openEdit(row) },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red',  divider: true, handler: row => this.confirmDelete(row) },
  ];

  emptyState = {
    icon: 'person',
    message: 'No authors found',
    description: 'Create your first book author to get started.',
    action: { label: 'Add Author', icon: 'add', handler: () => this.openCreate() },
  };

  cellTemplates: { [k: string]: TemplateRef<any> } = {};

  filterFields: FilterField[] = [
    { id: 'search', label: 'Search', type: 'text', placeholder: 'Author name...', value: '' },
  ];

  constructor(
    private service:      BookAuthorService,
    private dialog:       MatDialog,
    private alertService: AlertService,
    private cdr:          ChangeDetectorRef,
  ) {}

  ngOnInit(): void  { this.loadAll(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  ngAfterViewInit(): void {
    this.cellTemplates = { name: this.nameCell, biography: this.bioCell };
    this.cdr.detectChanges();
  }

  loadAll(): void {
    this.isLoading = true;
    this.service.getAll().pipe(take(1)).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) { this.allData = res.data; this.applyFilters(); this.buildStats(); }
        else this.alertService.error(res.message || 'Failed to load authors');
      },
      error: err => {
        this.isLoading = false;
        this.alertService.error(err?.error?.message ?? 'Failed to load authors');
      },
    });
  }

  get paginatedData(): BookAuthorResponseDto[] {
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
    const total     = this.allData.length;
    const withBio   = this.allData.filter(r => r.biography).length;
    this.statsCards = [
      { label: 'Total Authors',  value: total,   icon: 'people', iconColor: 'indigo' },
      { label: 'With Biography', value: withBio, icon: 'notes',  iconColor: 'blue'   },
    ];
  }

  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  onColumnHeaderClick(event: { column: TableColumn<BookAuthorResponseDto> }): void {
    const col = event.column.id as keyof BookAuthorResponseDto;
    this.filteredData = [...this.filteredData].sort((a, b) =>
      String((a as any)[col] ?? '').localeCompare(String((b as any)[col] ?? ''), undefined, { numeric: true })
    );
    this.currentPage = 1;
  }

  onRowClick(_row: BookAuthorResponseDto): void {}

  openCreate(): void {
    const data: BookAuthorDialogData = { mode: 'create' };
    this.dialog.open(BookAuthorDialogComponent, { data, width: '520px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1))
      .subscribe(result => { if (result?.success) this.loadAll(); });
  }

  openEdit(item: BookAuthorResponseDto): void {
    const data: BookAuthorDialogData = { mode: 'edit', item };
    this.dialog.open(BookAuthorDialogComponent, { data, width: '520px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1))
      .subscribe(result => { if (result?.success) this.loadAll(); });
  }

  confirmDelete(item: BookAuthorResponseDto): void {
    this.alertService.confirm({
      title: 'Delete Author',
      message: `Delete "${item.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Author deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to delete author');
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed to delete author'),
        });
      },
    });
  }
}