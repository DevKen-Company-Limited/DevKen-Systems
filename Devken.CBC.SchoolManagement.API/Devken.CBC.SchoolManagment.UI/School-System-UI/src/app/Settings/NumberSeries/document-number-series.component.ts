import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
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

// Import reusable components
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
import { DocumentNumberSeriesDto } from '../types/DocumentNumberSeries';
import { DocumentNumberSeriesService } from 'app/core/DevKenService/Settings/NumberSeries/DocumentNumberSeriesService';
import { CreateEditDocumentNumberSeriesDialogComponent } from 'app/dialog-modals/Settings/NumberSeries/create-edit-document-number-series-dialog.component';
import { UserService } from 'app/core/user/user.service';
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
  ],
  templateUrl: './document-number-series.component.html',
})
export class DocumentNumberSeriesComponent implements OnInit, OnDestroy {
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

  // Stats Cards
  get statsCards(): StatCard[] {
    return [
      {
        label: 'Total Series',
        value: this.total,
        icon: 'functions',
        iconColor: 'indigo',
      },
      {
        label: 'With Yearly Reset',
        value: this.yearlyResetCount,
        icon: 'autorenew',
        iconColor: 'amber',
      },
      {
        label: 'Total Numbers Generated',
        value: this.totalNumbersGenerated,
        icon: 'tag',
        iconColor: 'violet',
      },
      {
        label: 'Active This Year',
        value: this.activeThisYearCount,
        icon: 'event',
        iconColor: 'green',
      },
    ];
  }

  // Table Configuration
  tableColumns: TableColumn<DocumentNumberSeriesDto>[] = [
    {
      id: 'entity',
      label: 'Entity Type',
      align: 'left',
      sortable: true,
    },
    {
      id: 'format',
      label: 'Format Preview',
      align: 'left',
    },
    {
      id: 'status',
      label: 'Status',
      align: 'left',
    },
  ];

  tableActions: TableAction<DocumentNumberSeriesDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (series) => this.openEdit(series),
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (series) => this.deleteSeries(series),
      divider: true,
    },
  ];

  tableHeader: TableHeader = {
    title: 'Number Series',
    subtitle: '',
    icon: 'view_list',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'functions',
    message: 'No number series configured',
    description: 'Create your first number series to enable automatic numbering',
    action: {
      label: 'Create Number Series',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // State
  allData: DocumentNumberSeriesDto[] = [];
  isLoading = false;

  // Pagination
  currentPage = 1;
  itemsPerPage = 10;

  // Computed Stats
  get total(): number { 
    return this.allData.length; 
  }
  
  get yearlyResetCount(): number { 
    return this.allData.filter(s => s.resetEveryYear).length; 
  }
  
  get totalNumbersGenerated(): number { 
    return this.allData.reduce((sum, s) => sum + s.lastNumber, 0); 
  }
  
  get activeThisYearCount(): number {
    const currentYear = new Date().getFullYear();
    return this.allData.filter(s => s.lastGeneratedYear === currentYear).length;
  }

  get filteredData(): DocumentNumberSeriesDto[] {
    return this.allData;
  }

  get paginatedData(): DocumentNumberSeriesDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: DocumentNumberSeriesService,
    private _dialog: MatDialog,
    private _snackBar: MatSnackBar,
    private _confirmation: FuseConfirmationService,
    private _authService: AuthService,

  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      entity: this.entityCellTemplate,
      format: this.formatCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // Entity icon mapping
  getEntityIcon(entityName: string): string {
    const iconMap: { [key: string]: string } = {
      'Student': 'school',
      'Teacher': 'person',
      'Invoice': 'receipt',
      'Payment': 'payment',
      'Assessment': 'assignment',
      'Class': 'class',
    };
    return iconMap[entityName] || 'label';
  }

  // Format preview
  getFormatPreview(series: DocumentNumberSeriesDto): string {
    const nextNumber = (series.lastNumber + 1).toString().padStart(series.padding, '0');
    return `${series.prefix}${nextNumber}`;
  }

  // Data Loading
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allData = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} series configured`;
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load number series:', err);
          this.isLoading = false;
          this._showError(err.error?.message || 'Failed to load number series');
        }
      });
  }

openCreate(): void {
  const isSuperAdmin = this._authService.authUser?.isSuperAdmin ?? false;

  const dialogRef = this._dialog.open(CreateEditDocumentNumberSeriesDialogComponent, {
    panelClass: ['number-series-dialog', 'no-padding-dialog'],
    width: '700px',
    maxWidth: '95vw',
    maxHeight: '95vh',
    disableClose: true,
    data: { 
      mode: 'create',
      isSuperAdmin
    },
  });

  dialogRef.afterClosed()
    .pipe(takeUntil(this._unsubscribe))
    .subscribe(result => {
      if (result?.success) {
        this.loadAll();
      }
    });
}


  // Edit
  openEdit(series: DocumentNumberSeriesDto): void {
    const dialogRef = this._dialog.open(CreateEditDocumentNumberSeriesDialogComponent, {
      panelClass: ['number-series-dialog', 'no-padding-dialog'],
      width: '700px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      data: { mode: 'edit', numberSeries: series },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result?.success) {
          this.loadAll();
        }
      });
  }

  // Delete
  deleteSeries(series: DocumentNumberSeriesDto): void {
    const confirmation = this._confirmation.open({
      title: 'Delete Number Series',
      message: `Are you sure you want to delete the number series for ${series.entityName}? This action cannot be undone.`,
      icon: {
        name: 'delete',
        color: 'warn',
      },
      actions: {
        confirm: {
          label: 'Delete',
          color: 'warn',
        },
        cancel: {
          label: 'Cancel',
        },
      },
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed') {
        this._service.delete(series.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._showSuccess('Number series deleted successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to delete number series:', err);
              this._showError(err.error?.message || 'Failed to delete number series');
            }
          });
      }
    });
  }

  // Pagination
  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage = 1;
  }

  // Notifications
  private _showSuccess(message: string): void {
    this._snackBar.open(message, 'Close', { 
      duration: 3000, 
      panelClass: ['bg-green-600', 'text-white'] 
    });
  }

  private _showError(message: string): void {
    this._snackBar.open(message, 'Close', { 
      duration: 5000, 
      panelClass: ['bg-red-600', 'text-white'] 
    });
  }
}