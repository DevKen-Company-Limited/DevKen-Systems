import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

// Reusable components
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { 
  DataTableComponent, 
  TableColumn, 
  TableAction, 
  TableHeader, 
  TableEmptyState 
} from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';

// Services & DTOs
import { DocumentNumberSeriesDto } from '../types/DocumentNumberSeries';
import { DocumentNumberSeriesService } from 'app/core/DevKenService/Settings/NumberSeries/DocumentNumberSeriesService';
import { CreateEditDocumentNumberSeriesDialogComponent } from 'app/dialog-modals/Settings/NumberSeries/create-edit-document-number-series-dialog.component';
import { AuthService } from 'app/core/auth/auth.service';

@Component({
  selector: 'app-document-number-series',
  standalone: true,
  imports: [
    CommonModule,
    ScrollingModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    PageHeaderComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
    FilterPanelComponent
  ],
  templateUrl: './document-number-series.component.html',
})
export class DocumentNumberSeriesComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('entityCell') entityCellTemplate!: TemplateRef<any>;
  @ViewChild('formatCell') formatCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();

  // Breadcrumbs
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Settings', url: '/settings' },
    { label: 'Number Series' }
  ];

  // Filter panel
  showFilters = false;
  filterFields: FilterField[] = [
    {
      id: 'entityName',
      label: 'Entity Type',
      type: 'select',
      options: [
        { label: 'All', value: 'all' },
        { label: 'Student', value: 'Student' },
        { label: 'Teacher', value: 'Teacher' },
        { label: 'Invoice', value: 'Invoice' },
        { label: 'Payment', value: 'Payment' },
        { label: 'Assessment', value: 'Assessment' },
        { label: 'Class', value: 'Class' },
      ],
      value: 'all'
    },
    {
      id: 'prefix',
      label: 'Prefix',
      type: 'text',
      placeholder: 'Search prefix...',
      value: ''
    },
    {
      id: 'resetEveryYear',
      label: 'Yearly Reset',
      type: 'select',
      options: [
        { label: 'All', value: 'all' },
        { label: 'Yes', value: true },
        { label: 'No', value: false },
      ],
      value: 'all'
    }
  ];

  // Stats cards
  get statsCards(): StatCard[] {
    return [
      { label: 'Total Series', value: this.total, icon: 'functions', iconColor: 'indigo' },
      { label: 'With Yearly Reset', value: this.yearlyResetCount, icon: 'autorenew', iconColor: 'amber' },
      { label: 'Total Numbers Generated', value: this.totalNumbersGenerated, icon: 'tag', iconColor: 'violet' },
      { label: 'Active This Year', value: this.activeThisYearCount, icon: 'event', iconColor: 'green' },
    ];
  }

  // Table config
  tableColumns: TableColumn<DocumentNumberSeriesDto>[] = [
    { id: 'entity', label: 'Entity Type', align: 'left', sortable: true },
    { id: 'format', label: 'Format Preview', align: 'left' },
    { id: 'status', label: 'Status', align: 'left' },
  ];

  tableActions: TableAction<DocumentNumberSeriesDto>[] = [
    { id: 'edit', label: 'Edit', icon: 'edit', color: 'blue', handler: series => this.openEdit(series) },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red', handler: series => this.deleteSeries(series), divider: true },
  ];

  tableHeader: TableHeader = {
    title: 'Number Series',
    subtitle: '',
    icon: 'view_list',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700'
  };

  tableEmptyState: TableEmptyState = {
    icon: 'functions',
    message: 'No number series configured',
    description: 'Create your first number series to enable automatic numbering',
    action: { label: 'Create Number Series', icon: 'add', handler: () => this.openCreate() }
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // State
  allData: DocumentNumberSeriesDto[] = [];
  private _filteredData: DocumentNumberSeriesDto[] = [];
  isLoading = false;

  // Pagination
  currentPage = 1;
  itemsPerPage = 10;

  // Computed stats
  get total() { return this.allData.length; }
  get yearlyResetCount() { return this.allData.filter(s => s.resetEveryYear).length; }
  get totalNumbersGenerated() { return this.allData.reduce((sum, s) => sum + s.lastNumber, 0); }
  get activeThisYearCount() {
    const year = new Date().getFullYear();
    return this.allData.filter(s => s.lastGeneratedYear === year).length;
  }

  // Filtered & paginated data
  get filteredData(): DocumentNumberSeriesDto[] { return this._filteredData || this.allData; }
  set filteredData(value: DocumentNumberSeriesDto[]) { this._filteredData = value; }
  get paginatedData(): DocumentNumberSeriesDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: DocumentNumberSeriesService,
    private _dialog: MatDialog,
    private _snackBar: MatSnackBar,
    private _confirmation: FuseConfirmationService,
    private _authService: AuthService
  ) {}

  ngOnInit(): void { this.loadAll(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      entity: this.entityCellTemplate,
      format: this.formatCellTemplate,
      status: this.statusCellTemplate
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // Filter handling
  onFilterChange(event: FilterChangeEvent): void {
    const field = this.filterFields.find(f => f.id === event.filterId);
    if (field) field.value = event.value;
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.allData];

    const entity = this.filterFields.find(f => f.id === 'entityName')?.value;
    if (entity && entity !== 'all') filtered = filtered.filter(s => s.entityName === entity);

    const prefix = this.filterFields.find(f => f.id === 'prefix')?.value;
    if (prefix) filtered = filtered.filter(s => s.prefix?.toLowerCase().includes(prefix.toLowerCase()));

    const reset = this.filterFields.find(f => f.id === 'resetEveryYear')?.value;
    if (reset !== undefined && reset !== 'all') filtered = filtered.filter(s => s.resetEveryYear === reset);

    this.filteredData = filtered;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} series configured`;
  }

  onClearFilters(): void {
    this.filterFields.forEach(f => f.value = f.type === 'select' ? 'all' : '');
    this.applyFilters();
  }

  // Format preview
  getFormatPreview(series: DocumentNumberSeriesDto): string {
    const next = (series.lastNumber + 1).toString().padStart(series.padding, '0');
    return `${series.prefix}${next}`;
  }

  // Entity icons
  getEntityIcon(entityName: string): string {
    const icons: Record<string, string> = {
      Student: 'school',
      Teacher: 'person',
      Invoice: 'receipt',
      Payment: 'payment',
      Assessment: 'assignment',
      Class: 'class'
    };
    return icons[entityName] || 'label';
  }

  // Load data
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll().pipe(takeUntil(this._unsubscribe)).subscribe({
      next: res => { if (res.success) { this.allData = res.data; this.applyFilters(); } this.isLoading = false; },
      error: err => { console.error(err); this.isLoading = false; this._showError(err.error?.message || 'Failed to load number series'); }
    });
  }

  // Create/Edit/Delete
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditDocumentNumberSeriesDialogComponent, {
      panelClass: ['number-series-dialog', 'no-padding-dialog'],
      width: '700px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      data: { mode: 'create', isSuperAdmin: this._authService.authUser?.isSuperAdmin ?? false }
    });

    dialogRef.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  openEdit(series: DocumentNumberSeriesDto): void {
    const dialogRef = this._dialog.open(CreateEditDocumentNumberSeriesDialogComponent, {
      panelClass: ['number-series-dialog', 'no-padding-dialog'],
      width: '700px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      data: { mode: 'edit', numberSeries: series }
    });

    dialogRef.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => { if (result?.success) this.loadAll(); });
  }

  deleteSeries(series: DocumentNumberSeriesDto): void {
    const confirmation = this._confirmation.open({
      title: 'Delete Number Series',
      message: `Are you sure you want to delete the number series for ${series.entityName}?`,
      icon: { name: 'delete', color: 'warn' },
      actions: { confirm: { label: 'Delete', color: 'warn' }, cancel: { label: 'Cancel' } }
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed') {
        this._service.delete(series.id).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: res => { if (res.success) { this._showSuccess('Number series deleted'); this.loadAll(); } },
          error: err => { console.error(err); this._showError(err.error?.message || 'Failed to delete number series'); }
        });
      }
    });
  }

  // Pagination
  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(items: number): void { this.itemsPerPage = items; this.currentPage = 1; }

  // Notifications
  private _showSuccess(msg: string) { this._snackBar.open(msg, 'Close', { duration: 3000, panelClass: ['bg-green-600', 'text-white'] }); }
  private _showError(msg: string) { this._snackBar.open(msg, 'Close', { duration: 5000, panelClass: ['bg-red-600', 'text-white'] }); }
}
