import {
  Component, OnInit, OnDestroy, TemplateRef, ViewChild, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog }    from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule }   from '@angular/material/icon';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { AlertService }        from 'app/core/DevKenService/Alert/AlertService';
import { BookPublisherService } from 'app/core/DevKenService/Library/book-publisher.service';
import { BookPublisherResponseDto } from './Types/book-publisher.model';
import {
  BookPublisherDialogData,
  BookPublisherDialogComponent,
} from 'app/dialog-modals/Library/book-publisher-dialog/book-publisher-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';

@Component({
  selector: 'app-book-publishers',
  standalone: true,
  imports: [
    CommonModule, MatButtonModule, MatIconModule,
    PageHeaderComponent, StatsCardsComponent, FilterPanelComponent,
    DataTableComponent, PaginationComponent,
  ],
  templateUrl: './book-publishers.component.html',
})
export class BookPublishersComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();

  @ViewChild('nameCell')    nameCell!: TemplateRef<any>;
  @ViewChild('addressCell') addressCell!: TemplateRef<any>;

  allData:      BookPublisherResponseDto[] = [];
  filteredData: BookPublisherResponseDto[] = [];
  isLoading = false;

  currentPage  = 1;
  itemsPerPage = 10;
  showFilters  = false;
  private activeFilters: Record<string, any> = {};

  breadcrumbs: Breadcrumb[] = [
    { label: 'Library' },
    { label: 'Publishers' },
  ];

  statsCards: StatCard[] = [];

  tableHeader: TableHeader = {
    title: 'Book Publishers',
    subtitle: 'All registered book publishers',
    icon: 'business',
    iconGradient: 'bg-gradient-to-br from-orange-500 via-amber-500 to-yellow-600',
  };

  tableColumns: TableColumn<BookPublisherResponseDto>[] = [
    { id: 'name',      label: 'Publisher Name', align: 'left',   sortable: true },
    { id: 'address',   label: 'Address',         align: 'left',   hideOnMobile: true },
    { id: 'createdOn', label: 'Created',          align: 'center', sortable: true, hideOnMobile: true },
  ];

  tableActions: TableAction<BookPublisherResponseDto>[] = [
    { id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'blue', handler: row => this.openEdit(row) },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red',  divider: true, handler: row => this.confirmDelete(row) },
  ];

  emptyState = {
    icon: 'business',
    message: 'No publishers found',
    description: 'Create your first book publisher to get started.',
    action: { label: 'Add Publisher', icon: 'add', handler: () => this.openCreate() },
  };

  cellTemplates: { [k: string]: TemplateRef<any> } = {};

  filterFields: FilterField[] = [
    { id: 'search', label: 'Search', type: 'text', placeholder: 'Publisher name...', value: '' },
  ];

  constructor(
    private service:      BookPublisherService,
    private dialog:       MatDialog,
    private alertService: AlertService,
    private cdr:          ChangeDetectorRef,
  ) {}

  ngOnInit(): void    { this.loadAll(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  ngAfterViewInit(): void {
    this.cellTemplates = { name: this.nameCell, address: this.addressCell };
    this.cdr.detectChanges();
  }

  loadAll(): void {
    this.isLoading = true;
    this.service.getAll().pipe(take(1)).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) { this.allData = res.data; this.applyFilters(); this.buildStats(); }
        else this.alertService.error(res.message || 'Failed to load publishers');
      },
      error: err => {
        this.isLoading = false;
        this.alertService.error(err?.error?.message ?? 'Failed to load publishers');
      },
    });
  }

  get paginatedData(): BookPublisherResponseDto[] {
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
    const total       = this.allData.length;
    const withAddress = this.allData.filter(r => r.address).length;
    this.statsCards = [
      { label: 'Total Publishers', value: total,       icon: 'business',    iconColor: 'amber'  },
      { label: 'With Address',     value: withAddress, icon: 'location_on', iconColor: 'orange' },
    ];
  }

  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  onColumnHeaderClick(event: { column: TableColumn<BookPublisherResponseDto> }): void {
    const col = event.column.id as keyof BookPublisherResponseDto;
    this.filteredData = [...this.filteredData].sort((a, b) =>
      String((a as any)[col] ?? '').localeCompare(String((b as any)[col] ?? ''), undefined, { numeric: true })
    );
    this.currentPage = 1;
  }

  onRowClick(_row: BookPublisherResponseDto): void {}

  openCreate(): void {
    const data: BookPublisherDialogData = { mode: 'create' };
    this.dialog.open(BookPublisherDialogComponent, { data, width: '480px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  openEdit(item: BookPublisherResponseDto): void {
    const data: BookPublisherDialogData = { mode: 'edit', item };
    this.dialog.open(BookPublisherDialogComponent, { data, width: '480px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  confirmDelete(item: BookPublisherResponseDto): void {
    this.alertService.confirm({
      title: 'Delete Publisher',
      message: `Delete "${item.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Publisher deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to delete publisher');
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed to delete publisher'),
        });
      },
    });
  }
}