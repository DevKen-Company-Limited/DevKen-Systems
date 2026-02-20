// details/subject-details.component.ts
// KEY FIXES in detailItems getter:
//   • CBC Level:    uses subject.level      (not subject.cbcLevel — doesn't exist on SubjectDto)
//   • Date Created: uses subject.createdAt  (not subject.createdOn — backend now serialises as createdAt)
//   • Last Updated: uses subject.updatedAt  (not subject.updatedOn — backend now serialises as updatedAt)
//   • School row removed — SchoolName is not available without a navigation property

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }          from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatIconModule }         from '@angular/material/icon';
import { MatButtonModule }       from '@angular/material/button';
import { MatTabsModule }         from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule }        from '@angular/material/chips';
import { MatDividerModule }      from '@angular/material/divider';
import { MatTooltipModule }      from '@angular/material/tooltip';
import { MatMenuModule }         from '@angular/material/menu';
import { Subject }               from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';
import { of }                    from 'rxjs';

import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { getCBCLevelLabel, getSubjectTypeLabel } from '../Types/SubjectEnums';
import { SubjectDto }    from '../Types/subjectdto';

interface DetailItem {
  label:     string;
  value:     string | number | boolean | undefined | null;
  icon?:     string;
  copyable?: boolean;
  type?:     'text' | 'badge' | 'status' | 'boolean' | 'date';
}

@Component({
  selector: 'app-subject-details',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatIconModule, MatButtonModule, MatTabsModule,
    MatProgressSpinnerModule, MatChipsModule,
    MatDividerModule, MatTooltipModule, MatMenuModule,
    PageHeaderComponent,
  ],
  templateUrl: './subject-details.component.html',
})
export class SubjectDetailsComponent implements OnInit, OnDestroy {

  private _destroy$     = new Subject<void>();
  private _route        = inject(ActivatedRoute);
  private _router       = inject(Router);
  private _service      = inject(SubjectService);
  private _alertService = inject(AlertService);

  subject:  SubjectDto | null = null;
  isLoading = true;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard'        },
    { label: 'Academic',  url: '/academic'          },
    { label: 'Subjects',  url: '/academic/subjects' },
    { label: 'Details' },
  ];

  getSubjectTypeName = getSubjectTypeLabel;
  getCBCLevelName    = getCBCLevelLabel;

  ngOnInit(): void  { this._loadSubject(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private _loadSubject(): void {
    const id = this._route.snapshot.paramMap.get('id');
    if (!id) {
      this._alertService.error('Invalid subject ID');
      this._router.navigate(['/academic/subjects']);
      return;
    }

    this.isLoading = true;
    this._service.getById(id)
      .pipe(
        takeUntil(this._destroy$),
        catchError(err => {
          this._alertService.error(err.error?.message || 'Failed to load subject details');
          this._router.navigate(['/academic/subjects']);
          return of(null as any);
        }),
        finalize(() => { this.isLoading = false; }),
      )
      .subscribe(subject => {
        if (!subject) return;
        this.subject = subject;
        this.breadcrumbs[3] = { label: subject.name || 'Details' };
      });
  }

  get detailItems(): DetailItem[] {
    if (!this.subject) return [];
    return [
      { label: 'Subject Name',  value: this.subject.name,  icon: 'menu_book',  type: 'badge'  },
      { label: 'Subject Code',  value: this.subject.code,  icon: 'tag',        type: 'badge', copyable: true },

      // FIX: getSubjectTypeLabel handles numeric strings ("1" → "Core")
      { label: 'Subject Type',  value: this.getSubjectTypeName(this.subject.subjectType), icon: 'category' },

      // FIX: use subject.level — not subject.cbcLevel (that field doesn't exist on SubjectDto)
      { label: 'CBC Level',     value: this.getCBCLevelName(this.subject.level),          icon: 'stairs'   },

      // { label: 'Compulsory',    value: this.subject.isCompulsory ? 'Yes' : 'No',           icon: 'star',   type: 'boolean' },
      { label: 'Status',        value: this.subject.isActive     ? 'Active' : 'Inactive',  icon: 'info',   type: 'status'  },
      { label: 'Description',   value: this.subject.description,                           icon: 'description' },

      // FIX: use createdAt / updatedAt — backend now serialises with these names
      { label: 'Date Created',  value: this.formatDate(this.subject.createdOn), icon: 'event',  type: 'date' },
      { label: 'Last Updated',  value: this.formatDate(this.subject.updatedOn), icon: 'update', type: 'date' },
    ];
  }

  trackByLabel(_index: number, item: DetailItem): string { return item.label; }

  formatDate(val: string | Date | undefined | null): string {
    if (!val) return '—';
    try {
      const d = new Date(val);
      return isNaN(d.getTime()) ? '—' :
        d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
    } catch { return '—'; }
  }

  copyToClipboard(val: any): void {
    if (!val) return;
    navigator.clipboard.writeText(val.toString()).then(
      () => this._alertService.success('Copied to clipboard'),
      () => this._alertService.error('Failed to copy'),
    );
  }

  editSubject(): void {
    if (this.subject) this._router.navigate(['/academic/subjects/edit', this.subject.id]);
  }

  toggleActive(): void {
    if (!this.subject) return;
    const newStatus = !this.subject.isActive;
    const action    = newStatus ? 'activate' : 'deactivate';
    this._alertService.confirm({
      title:       `${newStatus ? 'Activate' : 'Deactivate'} Subject`,
      message:     `Are you sure you want to ${action} "${this.subject.name}"?`,
      confirmText: newStatus ? 'Activate' : 'Deactivate',
      cancelText:  'Cancel',
      onConfirm:   () => {
        this._service.toggleActive(this.subject!.id, newStatus)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alertService.success(`Subject ${action}d successfully`); this._loadSubject(); },
            error: err => this._alertService.error(err.error?.message || `Failed to ${action} subject`),
          });
      },
    });
  }

  deleteSubject(): void {
    if (!this.subject) return;
    this._alertService.confirm({
      title: 'Delete Subject',
      message: `Delete "${this.subject.name}" (${this.subject.code})? This cannot be undone.`,
      confirmText: 'Delete', cancelText: 'Cancel',
      onConfirm: () => {
        this._service.delete(this.subject!.id)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alertService.success('Subject deleted successfully'); this._router.navigate(['/academic/subjects']); },
            error: err => this._alertService.error(err.error?.message || 'Failed to delete subject'),
          });
      },
    });
  }

  goBack(): void { this._router.navigate(['/academic/subjects']); }
}