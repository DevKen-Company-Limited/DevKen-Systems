import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { BookAuthorService } from 'app/core/DevKenService/Library/book-author.service';
import { BookCategoryService } from 'app/core/DevKenService/Library/book-category.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { CreateEditBookDialogComponent } from 'app/dialog-modals/Library/book/create-edit-book-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { BookDto } from './Types/book.types';
import { BookCategoryResponseDto } from '../book-category/Types/book-category.model';
import { BookAuthorResponseDto } from '../book-author/Types/book-author.model';

@Component({
  selector: 'app-books',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './books.component.html',
})
export class BooksComponent
  extends BaseListComponent<BookDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();

  private _alertService!: AlertService;
  private _bookService!: BookService;
  private _categoryService!: BookCategoryService;
  private _authorService!: BookAuthorService;
  private _schoolService!: SchoolService;

  @ViewChild('titleCell',    { static: true }) titleCell!:    TemplateRef<any>;
  @ViewChild('isbnCell',     { static: true }) isbnCell!:     TemplateRef<any>;
  @ViewChild('categoryCell', { static: true }) categoryCell!: TemplateRef<any>;
  @ViewChild('authorCell',   { static: true }) authorCell!:   TemplateRef<any>;
  @ViewChild('copiesCell',   { static: true }) copiesCell!:   TemplateRef<any>;
  @ViewChild('schoolCell',   { static: true }) schoolCell!:   TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      title:    this.titleCell,
      isbn:     this.isbnCell,
      category: this.categoryCell,
      author:   this.authorCell,
      copies:   this.copiesCell,
      school:   this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools:    SchoolDto[]       = [];
  categories: BookCategoryResponseDto[] = [];
  authors:    BookAuthorResponseDto[]   = [];
  isDataLoading  = true;
  showFilterPanel = false;
  currentPage    = 1;
  itemsPerPage   = 10;

  filterValues = {
    search: '', categoryId: 'all', authorId: 'all', schoolId: 'all',
  };

  // ── Breadcrumbs ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Books' },
  ];

  // ── Table config ───────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  get tableColumns(): TableColumn<BookDto>[] {
    const cols: TableColumn<BookDto>[] = [
      { id: 'title',    label: 'Title',    align: 'left', sortable: true },
      { id: 'isbn',     label: 'ISBN',     align: 'left', hideOnMobile: true },
      { id: 'category', label: 'Category', align: 'left', hideOnMobile: true },
      { id: 'author',   label: 'Author',   align: 'left', hideOnTablet: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push({ id: 'copies', label: 'Copies', align: 'center' });
    return cols;
  }

  tableActions: TableAction<BookDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
      divider: true,
    },
  ];

  tableHeader: TableHeader = {
    title: 'Books Catalogue',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-indigo-600 to-violet-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'menu_book',
    message: 'No books found',
    description: 'Try adjusting your filters or add a new book',
    action: { label: 'Add First Book', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const base: StatCard[] = [
      { label: 'Total Books',   value: data.length,                                             icon: 'menu_book',      iconColor: 'indigo' },
      { label: 'Total Copies',  value: data.reduce((s, b) => s + b.totalCopies, 0),            icon: 'library_books',  iconColor: 'blue'   },
      { label: 'Available',     value: data.reduce((s, b) => s + b.availableCopies, 0),        icon: 'check_circle',   iconColor: 'green'  },
      { label: 'Checked Out',   value: data.reduce((s, b) => s + (b.totalCopies - b.availableCopies), 0), icon: 'assignment_return', iconColor: 'amber' },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(b => b.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & paginated ───────────────────────────────────────────────────
  get filteredData(): BookDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(b =>
      (!q || b.title?.toLowerCase().includes(q) || b.isbn?.toLowerCase().includes(q) || b.authorName?.toLowerCase().includes(q)) &&
      (this.filterValues.categoryId === 'all' || b.categoryId === this.filterValues.categoryId) &&
      (this.filterValues.authorId   === 'all' || b.authorId   === this.filterValues.authorId  ) &&
      (this.filterValues.schoolId   === 'all' || b.schoolId   === this.filterValues.schoolId  )
    );
  }

  get paginatedData(): BookDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    dialog: MatDialog,
    snackBar: MatSnackBar,
    bookService: BookService,
    private readonly _injectedAuth: AuthService,
    alertService: AlertService,
    schoolService: SchoolService,
    categoryService: BookCategoryService,
    authorService: BookAuthorService,
  ) {
    super(bookService, dialog, snackBar);
    this._alertService    = alertService;
    this._bookService     = bookService;
    this._schoolService   = schoolService;
    this._categoryService = categoryService;
    this._authorService   = authorService;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void  { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data loading ───────────────────────────────────────────────────────────
  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._bookService.getAll(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} books found`;
          } else {
            this._alertService.error(res.message || 'Failed to load books');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to load books');
          this.isLoading = false;
        }
      });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;

    const requests: any = {
      categories: this._categoryService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      authors:    this._authorService.getAll().pipe(catchError(()    => of({ success: false, data: [] }))),
    };
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        this.categories = res.categories?.data || [];
        this.authors    = res.authors?.data    || [];
        if (res.schools) this.schools = res.schools?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); }
    });
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Title, ISBN or Author...', value: '' },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'categoryId', label: 'Category', type: 'select', value: 'all',
        options: [
          { label: 'All Categories', value: 'all' },
          ...this.categories.map(c => ({ label: c.name, value: c.id })),
        ],
      },
      {
        id: 'authorId', label: 'Author', type: 'select', value: 'all',
        options: [
          { label: 'All Authors', value: 'all' },
          ...this.authors.map(a => ({ label: a.name, value: a.id })),
        ],
      },
    );
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} books found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll();
    }
  }

  onClearFilters(): void {
    this.filterValues = { search: '', categoryId: 'all', authorId: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void  { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  private _openCreate(): void {
    this.openDialog(CreateEditBookDialogComponent, {
      panelClass: ['book-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(book: BookDto): void {
    this.openDialog(CreateEditBookDialogComponent, {
      panelClass: ['book-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', book },
    });
  }

  openCreate(): void { this._openCreate(); }

  private _confirmDelete(book: BookDto): void {
    this._alertService.confirm({
      title:       'Delete Book',
      message:     `Delete "${book.title}"? This will remove all associated data.`,
      confirmText: 'Delete',
      onConfirm:   () => this._doDelete(book),
    });
  }

  private _doDelete(book: BookDto): void {
    this.isLoading = true;
    this._bookService.delete(book.id).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Book deleted successfully');
          if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
          this.loadAll();
        } else {
          this._alertService.error(res.message || 'Failed to delete');
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to delete book');
        this.isLoading = false;
      }
    });
  }
}