import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  ViewChild,
  TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Subject, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { InvoiceItemService } from 'app/core/DevKenService/Finance/InvoiceItem/invoice-item.service';
import { DataTableComponent, TableHeader, TableEmptyState, TableColumn, TableAction } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { InvoiceItemResponseDto } from '../Types/invoice-item.types';



@Component({
  selector: 'app-invoice-items-list',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    DataTableComponent,
    PaginationComponent,
    FilterPanelComponent,
    StatsCardsComponent,
  ],
  templateUrl: './invoice-items-list.component.html',
})
export class InvoiceItemsListComponent implements OnInit, OnDestroy {
  // ── Inputs ────────────────────────────────────────────────────────────────
  @Input({ required: true }) invoiceId!: string;
  @Input() showFilters = false;

  // ── Outputs ───────────────────────────────────────────────────────────────
  /** Signals the parent to open the create form */
  @Output() addItem    = new EventEmitter<void>();
  /** Signals the parent to open the edit form for a specific item */
  @Output() editItem   = new EventEmitter<InvoiceItemResponseDto>();

  // ── Cell Templates ────────────────────────────────────────────────────────
  @ViewChild('descriptionCell') descriptionCell!: TemplateRef<any>;
  @ViewChild('typeCell')        typeCell!:        TemplateRef<any>;
  @ViewChild('amountCell')      amountCell!:      TemplateRef<any>;
  @ViewChild('discountCell')    discountCell!:    TemplateRef<any>;
  @ViewChild('taxCell')         taxCell!:         TemplateRef<any>;
  @ViewChild('netCell')         netCell!:         TemplateRef<any>;

  // ── State ─────────────────────────────────────────────────────────────────
  allItems:       InvoiceItemResponseDto[] = [];
  filteredItems:  InvoiceItemResponseDto[] = [];
  paginatedItems: InvoiceItemResponseDto[] = [];

  isLoading    = false;
  currentPage  = 1;
  itemsPerPage = 10;

  statsCards: StatCard[] = [];

  // ── Table config ──────────────────────────────────────────────────────────
  tableHeader: TableHeader = {
    title: 'Invoice Line Items',
    subtitle: 'All charges and fees on this invoice',
    icon: 'receipt_long',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'receipt_long',
    message: 'No line items yet',
    description: 'Add items to this invoice using the button above.',
    action: {
      label: 'Add First Item',
      icon: 'add',
      handler: () => this.addItem.emit(),
    },
  };

  columns: TableColumn<InvoiceItemResponseDto>[] = [
    { id: 'description', label: 'Description',  sortable: true },
    { id: 'itemType',    label: 'Type',          hideOnMobile: true },
    { id: 'quantity',    label: 'Qty',           align: 'center' },
    { id: 'unitPrice',   label: 'Unit Price',    align: 'right', hideOnMobile: true },
    { id: 'discount',    label: 'Discount',      align: 'right', hideOnMobile: true },
    { id: 'taxAmount',   label: 'Tax',           align: 'right', hideOnMobile: true },
    { id: 'netAmount',   label: 'Net Amount',    align: 'right' },
  ];

  actions: TableAction<InvoiceItemResponseDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'indigo',
      handler: (row) => this.editItem.emit(row),
    },
    {
      id: 'recompute',
      label: 'Recompute',
      icon: 'calculate',
      color: 'amber',
      handler: (row) => this.recompute(row),
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (row) => this.onDelete(row),
    },
  ];

  filterFields: FilterField[] = [
    {
      id: 'search',
      label: 'Search',
      type: 'text',
      placeholder: 'Description, type, GL code…',
      value: '',
    },
    {
      id: 'taxable',
      label: 'Taxable',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All',          value: 'all' },
        { label: 'Taxable',      value: 'true' },
        { label: 'Non-taxable',  value: 'false' },
      ],
    },
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private service: InvoiceItemService,
    private alertService: AlertService
  ) {}

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadItems();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data ──────────────────────────────────────────────────────────────────

  loadItems(): void {
    this.isLoading = true;
    this.service
      .getByInvoice(this.invoiceId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.allItems = res.data ?? [];
            this.applyFilters();
            this.buildStats();
          } else {
            this.alertService.error('Load failed', res.message || 'Could not load invoice items.');
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.alertService.error('Error', err?.error?.message || 'Failed to load invoice items.');
        },
      });
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  private onDelete(item: InvoiceItemResponseDto): void {
  this.alertService.confirm({
    title: 'Delete Item',
    message: `Remove "${item.description}" from this invoice?`,
    onConfirm: () => {
      this.service.delete(item.id, this.invoiceId)   // ✅ correct order: id first
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            if (res.success) {
              this.alertService.success('Deleted', 'Item removed from invoice.');
              this.loadItems();
            } else {
              this.alertService.error('Failed', res.message || 'Could not delete item.');
            }
          },
          error: (err) => {
            this.alertService.error('Error', err?.error?.message || 'Failed to delete item.');
          },
        });
    }
  });
}

  // ── Recompute ─────────────────────────────────────────────────────────────

  private recompute(item: InvoiceItemResponseDto): void {
    this.service
      .recompute(this.invoiceId, item.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.alertService.success('Recomputed', `Financials updated for "${item.description}".`);
            this.loadItems();
          } else {
            this.alertService.error('Failed', res.message || 'Recompute failed.');
          }
        },
        error: (err) => {
          this.alertService.error('Error', err?.error?.message || 'Recompute failed.');
        },
      });
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  onFilterChange(event: FilterChangeEvent): void {
    const field = this.filterFields.find((f) => f.id === event.filterId);
    if (field) field.value = event.value;
    this.currentPage = 1;
    this.applyFilters();
  }

  onClearFilters(): void {
    this.filterFields.forEach((f) => (f.value = f.type === 'select' ? 'all' : ''));
    this.currentPage = 1;
    this.applyFilters();
  }

  private applyFilters(): void {
    const search  = (this.filterFields.find((f) => f.id === 'search')?.value ?? '').toLowerCase();
    const taxable = this.filterFields.find((f) => f.id === 'taxable')?.value ?? 'all';

    this.filteredItems = this.allItems.filter((item) => {
      const matchSearch =
        !search ||
        item.description.toLowerCase().includes(search) ||
        (item.itemType ?? '').toLowerCase().includes(search) ||
        (item.glCode ?? '').toLowerCase().includes(search);

      const matchTax =
        taxable === 'all' ||
        (taxable === 'true'  &&  item.isTaxable) ||
        (taxable === 'false' && !item.isTaxable);

      return matchSearch && matchTax;
    });

    this.paginate();
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  onPageChange(page: number): void {
    this.currentPage = page;
    this.paginate();
  }

  private paginate(): void {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    this.paginatedItems = this.filteredItems.slice(start, start + this.itemsPerPage);
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  private buildStats(): void {
    const totalNet  = this.allItems.reduce((s, i) => s + i.netAmount,  0);
    const totalTax  = this.allItems.reduce((s, i) => s + i.taxAmount,  0);
    const totalDisc = this.allItems.reduce((s, i) => s + i.discount,   0);

    this.statsCards = [
      { label: 'Line Items',      value: this.allItems.length,           icon: 'receipt_long',    iconColor: 'indigo' },
      { label: 'Total Net',       value: this.formatCurrency(totalNet),  icon: 'payments',        iconColor: 'green' },
      { label: 'Total Tax',       value: this.formatCurrency(totalTax),  icon: 'account_balance', iconColor: 'amber' },
      { label: 'Total Discounts', value: this.formatCurrency(totalDisc), icon: 'discount',        iconColor: 'violet' },
    ];
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', minimumFractionDigits: 2,
    }).format(amount);
  }

  get cellTemplates(): { [id: string]: TemplateRef<any> } {
    return {
      description: this.descriptionCell,
      itemType:    this.typeCell,
      unitPrice:   this.amountCell,
      discount:    this.discountCell,
      taxAmount:   this.taxCell,
      netAmount:   this.netCell,
    };
  }
}