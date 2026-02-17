import {
  Component, Inject, OnInit, OnDestroy,
  ChangeDetectorRef, inject
} from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { SchoolDto, SchoolType, SchoolCategory } from 'app/Tenant/types/school';
import { API_BASE_URL } from 'app/app.config';
import { SecureImagePipe } from 'app/Tenant/pipe/SecureImagePipe';

export interface SchoolViewDialogData {
  school: SchoolDto;
}

@Component({
  selector:    'app-school-view-dialog',
  standalone:  true,
  imports: [
    CommonModule,
    AsyncPipe,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    SecureImagePipe,
  ],
  templateUrl: './school-view-dialog.component.html',
  styleUrls:   ['./school-view-dialog.component.scss'],
})
export class SchoolViewDialogComponent implements OnInit, OnDestroy {
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private _unsubscribe = new Subject<void>();

  school!: SchoolDto;
  logoUrl: string | null = null;

  private readonly _typeLabels: Record<number, string> = {
    [SchoolType.Public]:        'Public School',
    [SchoolType.Private]:       'Private School',
    [SchoolType.International]: 'International School',
    [SchoolType.NGO]:           'NGO / Mission School',
  };

  private readonly _catLabels: Record<number, string> = {
    [SchoolCategory.Day]:      'Day School',
    [SchoolCategory.Boarding]: 'Boarding School',
    [SchoolCategory.Mixed]:    'Mixed (Day & Boarding)',
  };

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: SchoolViewDialogData,
    private dialogRef: MatDialogRef<SchoolViewDialogComponent>,
    private cdr: ChangeDetectorRef,
  ) {
    this.school = data.school;
    // Panel classes are targeted by the global ::ng-deep rule in dialog-styles.scss
    dialogRef.addPanelClass(['school-view-dialog', 'responsive-dialog']);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this._resolveLogoUrl();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private _resolveLogoUrl(): void {
    const raw = this.school.logoUrl;
    if (!raw) return;
    if (raw.startsWith('http://') || raw.startsWith('https://')) {
      this.logoUrl = raw;
    } else {
      const base = this._apiBaseUrl.replace(/\/$/, '');
      this.logoUrl = `${base}${raw.startsWith('/') ? raw : '/' + raw}`;
    }
  }

  // ── Public helpers ────────────────────────────────────────────────────────

  getTypeLabel(v: number | string | null | undefined): string {
    if (v == null) return '—';
    const k = typeof v === 'string' ? parseInt(v, 10) : v;
    return this._typeLabels[k] ?? `Type ${v}`;
  }

  getCategoryLabel(v: number | string | null | undefined): string {
    if (v == null) return '—';
    const k = typeof v === 'string' ? parseInt(v, 10) : v;
    return this._catLabels[k] ?? `Category ${v}`;
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  edit():  void { this.dialogRef.close({ action: 'edit', school: this.school }); }
  close(): void { this.dialogRef.close(); }
}