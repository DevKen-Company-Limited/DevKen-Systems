import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { take } from 'rxjs/operators';
import { MatTableDataSource } from '@angular/material/table';
import { IListService } from '../Services/IListService';
import { ApiResponse } from 'app/Tenant/types/school';

export abstract class BaseListComponent<TDto> {
  dataSource = new MatTableDataSource<TDto>([]);
  isLoading = false;

  constructor(
    protected service: IListService<TDto>,
    protected dialog: MatDialog,
    protected snackBar: MatSnackBar
  ) {}

  protected init() {
    this.loadAll();
  }

  protected loadAll() {
    this.isLoading = true;
    this.service.getAll()
      .pipe(take(1))
      .subscribe({
        next: (res: ApiResponse<TDto[]>) => {
          this.isLoading = false;
          if (res.success) {
            this.dataSource.data = res.data;
          } else {
            this.snackBar.open(res.message || 'Failed to load data', 'Close', { duration: 3000 });
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.snackBar.open(err?.error?.message || err.message || 'Failed to load data', 'Close', { duration: 4000 });
        }
      });
  }

  protected deleteItem(id: string, onSuccess?: () => void) {
    if (!confirm('Are you sure?')) return;

    this.isLoading = true;
    this.service.delete(id)
      .pipe(take(1))
      .subscribe({
        next: (res: ApiResponse<any>) => {
          this.isLoading = false;
          if (res.success) {
            this.snackBar.open(res.message || 'Deleted', 'Close', { duration: 2500 });
            this.loadAll();
            onSuccess?.();
          } else {
            this.snackBar.open(res.message || 'Failed to delete', 'Close', { duration: 3000 });
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.snackBar.open(err?.error?.message || err.message || 'Failed to delete', 'Close', { duration: 4000 });
        }
      });
  }

  /**
   * Helper to open a dialog
   * component: dialog component class
   * config: MatDialog config (data, width, etc)
   */
  protected openDialog(component: any, config: any = {}) {
    const ref = this.dialog.open(component, config);
    ref.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadAll();
      }
    });
    return ref;
  }
}
