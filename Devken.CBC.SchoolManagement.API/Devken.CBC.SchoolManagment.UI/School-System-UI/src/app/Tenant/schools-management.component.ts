import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FuseAlertComponent } from '@fuse/components/alert';
import { CreateEditSchoolDialogComponent } from 'app/dialog-modals/Tenant/create-edit-school-dialog.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { SchoolDto } from './types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';


@Component({
  selector: 'app-schools-management',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    FuseAlertComponent
  ],
  templateUrl: './schools-management.component.html',
})
export class SchoolsManagementComponent extends BaseListComponent<SchoolDto> implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  displayedColumns: string[] = ['name', 'slug', 'email', 'phone', 'isActive', 'actions'];

  // paging state
  pageSize = 20;
  currentPage = 1;
  total = 0;

  // alerts
  showAlert = false;
  alert = { type: 'success' as 'success' | 'error', message: '' };

  constructor(
    protected service: SchoolService, 
    protected dialog: MatDialog,
    protected snackBar: MatSnackBar
  ) {
    super(service, dialog, snackBar);
  }

  ngOnInit(): void {
    this.init();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  applyFilter(value: string) {
    const v = (value || '').trim().toLowerCase();
    this.dataSource.filter = v;
  }

  openCreate() {
    this.openDialog(CreateEditSchoolDialogComponent, { width: '600px', data: { mode: 'create' } });
  }

  openEdit(school: SchoolDto) {
    this.openDialog(CreateEditSchoolDialogComponent, { width: '600px', data: { mode: 'edit', school } });
  }

  removeSchool(school: SchoolDto) {
    this.deleteItem(school.id);
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    // If using server-side paging, call loadAll() with pagination params
  }
}
