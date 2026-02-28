import {
  Component, OnInit, TemplateRef, ViewChild, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { take, takeUntil } from 'rxjs/operators';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { FeeItemDialogData, FeeItemDialogComponent } from 'app/dialog-modals/Finance/fee-item-dialog/fee-item-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { FeeItemResponseDto, FEE_TYPE_OPTIONS, resolveFeeTypeLabel, resolveLevelLabel } from './Types/fee-item.model';

import { FeeItemService } from 'app/core/DevKenService/Finance/fee-item.service';
import { Subject } from 'rxjs';
import { MatButton, MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FeeItemResponseDto, FEE_TYPE_OPTIONS, resolveFeeTypeLabel, resolveLevelLabel } from './Types/fee-item.model';



@Component({
  selector: 'app-fee-items',
  standalone: true,
  imports: [
    CommonModule,
    PageHeaderComponent,
    StatsCardsComponent,
    FilterPanelComponent,
    DataTableComponent,
    PaginationComponent,
    MatIconModule, 
    MatButtonModule,
  ],
  templateUrl: './fee-items.component.html',
})
export class FeeItemsComponent implements OnInit {
  private _destroy$ = new Subject<void>();
  // ── Templates ──────────────────────────────────────────────────────────────
  @ViewChild('nameCell')      nameCell!: TemplateRef<any>;
  @ViewChild('feeTypeCell')   feeTypeCell!: TemplateRef<any>;
  @ViewChild('amountCell')    amountCell!: TemplateRef<any>;
  @ViewChild('levelCell')     levelCell!: TemplateRef<any>;
  @ViewChild('mandatoryCell') mandatoryCell!: TemplateRef<any>;
  @ViewChild('statusCell')    statusCell!: TemplateRef<any>;

  // ── Data ───────────────────────────────────────────────────────────────────
  allData:      FeeItemResponseDto[] = [];
  filteredData: FeeItemResponseDto[] = [];
  isLoading = false;

  // ── Pagination ─────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  // ── Filters ────────────────────────────────────────────────────────────────
  showFilters = false;
  private activeFilters: Record<string, any> = {};

  // ── Page Header ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Finance' },
    { label: 'Fee Items' },
  ];

  // ── Stats ──────────────────────────────────────────────────────────────────
  statsCards: StatCard[] = [];

  // ── Table ──────────────────────────────────────────────────────────────────
  tableHeader: TableHeader = {
    title: 'Fee Items',
    subtitle: 'All configured fee templates',
    icon: 'payments',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableColumns: TableColumn<FeeItemResponseDto>[] = [
    { id: 'name',             label: 'Name',          align: 'left',   sortable: true },
    { id: 'feeType',          label: 'Type',          align: 'center', sortable: true, hideOnMobile: true },
    { id: 'defaultAmount',    label: 'Amount',        align: 'right',  sortable: true },
    { id: 'applicableLevel',  label: 'Level',         align: 'center', hideOnMobile: true },
    { id: 'isMandatory',      label: 'Mandatory',     align: 'center', hideOnMobile: true },
    { id: 'isActive',         label: 'Status',        align: 'center' },
  ];

  tableActions: TableAction<FeeItemResponseDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: row => this.openEdit(row),
    },
    {
      id: 'toggle',
      label: 'Toggle Active',
      icon: 'toggle_on',
      color: 'amber',
      handler: row => this.toggleActive(row),
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      divider: true,
      handler: row => this.confirmDelete(row),
    },
  ];

  emptyState = {
    icon: 'payments',
    message: 'No fee items found',
    description: 'Create your first fee item to get started.',
    action: { label: 'Add Fee Item', icon: 'add', handler: () => this.openCreate() },
  };

  cellTemplates: { [k: string]: TemplateRef<any> } = {};

  filterFields: FilterField[] = [
    {
      id: 'search',
      label: 'Search',
      type: 'text',
      placeholder: 'Name or code...',
      value: '',
    },
    {
      id: 'feeType',
      label: 'Fee Type',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All Types', value: 'all' },
        ...FEE_TYPE_OPTIONS.map(o => ({ label: o.label, value: String(o.value) })),
      ],
    },
    {
      id: 'status',
      label: 'Status',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All', value: 'all' },
        { label: 'Active', value: 'true' },
        { label: 'Inactive', value: 'false' },
      ],
    },
  ];

  // ── Helpers ────────────────────────────────────────────────────────────────
  resolveFeeType = resolveFeeTypeLabel;
  resolveLevel   = resolveLevelLabel;

  constructor(
    private service:      FeeItemService,
    private dialog:       MatDialog,
    private alertService: AlertService,
    private cdr:          ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      name:            this.nameCell,
      feeType:         this.feeTypeCell,
      defaultAmount:   this.amountCell,
      applicableLevel: this.levelCell,
      isMandatory:     this.mandatoryCell,
      isActive:        this.statusCell,
    };
    this.cdr.detectChanges();
  }

  // ── Data ───────────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;
    this.service.getAll().pipe(take(1)).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) {
          this.allData = res.data;
          this.applyFilters();
          this.buildStats();
        } else {
          this.alertService.error('Error', res.message);
        }
      },
      error: err => {
        this.isLoading = false;
        this.alertService.error('Error', err?.error?.message ?? 'Failed to load fee items.');
      },
    });
  }

  // ── Computed ───────────────────────────────────────────────────────────────
  get paginatedData(): FeeItemResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  onFilterChange(event: FilterChangeEvent): void {
    this.activeFilters[event.filterId] = event.value;
    this.currentPage = 1;
    this.applyFilters();
  }

  onClearFilters(): void {
    this.activeFilters = {};
    this.filterFields.forEach(f => {
      f.value = f.type === 'select' ? 'all' : '';
    });
    this.currentPage = 1;
    this.applyFilters();
  }

  private applyFilters(): void {
    let data = [...this.allData];

    const search  = (this.activeFilters['search']  ?? '').toLowerCase().trim();
    const feeType = this.activeFilters['feeType'] ?? 'all';
    const status  = this.activeFilters['status']  ?? 'all';

    if (search) {
      data = data.filter(r =>
        r.name.toLowerCase().includes(search) ||
        r.code.toLowerCase().includes(search)
      );
    }
    if (feeType !== 'all') {
      data = data.filter(r => r.feeType === feeType);
    }
    if (status !== 'all') {
      const active = status === 'true';
      data = data.filter(r => r.isActive === active);
    }

    this.filteredData = data;
  }

  // ── Pagination ─────────────────────────────────────────────────────────────
  onPageChange(page: number): void        { this.currentPage = page; }
  onItemsPerPageChange(n: number): void   { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Stats ──────────────────────────────────────────────────────────────────
  private buildStats(): void {
    const total    = this.allData.length;
    const active   = this.allData.filter(r => r.isActive).length;
    const mandatory= this.allData.filter(r => r.isMandatory).length;
    const taxable  = this.allData.filter(r => r.isTaxable).length;

    this.statsCards = [
      { label: 'Total Fee Items', value: total,     icon: 'payments',   iconColor: 'indigo' },
      { label: 'Active',          value: active,     icon: 'check_circle', iconColor: 'green' },
      { label: 'Mandatory',       value: mandatory,  icon: 'lock',       iconColor: 'amber' },
      { label: 'Taxable',         value: taxable,    icon: 'receipt',    iconColor: 'violet' },
    ];
  }

  // ── Sorting ────────────────────────────────────────────────────────────────
  onColumnHeaderClick(event: { column: TableColumn<FeeItemResponseDto> }): void {
    const col = event.column.id as keyof FeeItemResponseDto;
    this.filteredData = [...this.filteredData].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      return String(va).localeCompare(String(vb), undefined, { numeric: true });
    });
    this.currentPage = 1;
  }

  onRowClick(_row: FeeItemResponseDto): void {
    // Optional: navigate to detail view
  }

  // ── CRUD ───────────────────────────────────────────────────────────────────
  openCreate(): void {
    const data: FeeItemDialogData = { mode: 'create' };
    this.dialog.open(FeeItemDialogComponent, { data, width: '640px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1))
      .subscribe(result => { if (result?.success) this.loadAll(); });
  }

  openEdit(item: FeeItemResponseDto): void {
    const data: FeeItemDialogData = { mode: 'edit', item };
    this.dialog.open(FeeItemDialogComponent, { data, width: '640px', panelClass: 'rounded-2xl' })
      .afterClosed().pipe(take(1))
      .subscribe(result => { if (result?.success) this.loadAll(); });
  }

  toggleActive(item: FeeItemResponseDto): void {
    this.service.toggleActive(item.id, !item.isActive).pipe(take(1)).subscribe({
      next: res => {
        if (res.success) {
          this.alertService.success('Updated', `${item.name} is now ${!item.isActive ? 'active' : 'inactive'}.`);
          this.loadAll();
        } else {
          this.alertService.error('Error', res.message);
        }
      },
      error: err => this.alertService.error('Error', err?.error?.message),
    });
  }

confirmDelete(item: FeeItemResponseDto): void{
    this.alertService.confirm({
      title: 'Delete Item',
      message: `Delete "${item.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Item deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed to delete item'),
        });
      },
    });
  }
//  confirmDelete(item: FeeItemResponseDto): void {
//   this.alertService.confirm(
//     `Delete "${item.name}"?`,
//     'This action cannot be undone.',
//     () => this.executeDelete(item),
//     {
//       message:     'This action cannot be undone.',
//       confirmText: 'Delete',
//       cancelText:  'Cancel',
//       onConfirm:   () => this.executeDelete(item),
//     }
//   );
// }

private executeDelete(item: FeeItemResponseDto): void {
  this.service.delete(item.id).pipe(take(1)).subscribe({
    next: res => {
      if (res.success) {
        this.alertService.success('Deleted', `${item.name} has been deleted.`);
        this.loadAll();
      } else {
        this.alertService.error('Error', res.message);
      }
    },
    error: err => this.alertService.error('Error', err?.error?.message),
  });
}
}