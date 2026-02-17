import {
  Component, Inject, OnInit, OnDestroy,
  ChangeDetectorRef, inject
} from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SchoolDto, SchoolType, SchoolCategory } from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { API_BASE_URL } from 'app/app.config';
import { SecureImagePipe } from 'app/Tenant/pipe/SecureImagePipe';

export interface SchoolViewDialogData {
  school: SchoolDto;
}

@Component({
  selector: 'app-school-view-dialog',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDividerModule,
    SecureImagePipe,
  ],
  template: `
    <!-- ── Header ──────────────────────────────────────────────────────── -->
    <div class="view-header">
      <div class="header-left">
        <div class="header-icon-ring">
          <mat-icon class="header-icon">domain</mat-icon>
        </div>
        <div class="header-text">
          <h2 class="header-title">School Details</h2>
          <p class="header-subtitle">{{ school.name }}</p>
        </div>
      </div>
      <button mat-icon-button class="close-btn" (click)="close()" matTooltip="Close">
        <mat-icon>close</mat-icon>
      </button>
    </div>

    <!-- ── Body ────────────────────────────────────────────────────────── -->
    <div class="view-body">

      <!-- Hero banner with logo + key facts -->
      <div class="hero-section">

        <!-- Logo -->
        <div class="logo-wrap">
          <ng-container *ngIf="logoUrl">
            <ng-container *ngIf="logoUrl | secureImage | async as safeSrc; else logoFallback">
              <img [src]="safeSrc" [alt]="school.name" class="logo-img" />
            </ng-container>
          </ng-container>
          <ng-template #logoFallback>
            <div class="logo-placeholder">
              <mat-icon>school</mat-icon>
            </div>
          </ng-template>
          <ng-container *ngIf="!logoUrl">
            <div class="logo-placeholder">
              <mat-icon>school</mat-icon>
            </div>
          </ng-container>
        </div>

        <!-- Core identity -->
        <div class="hero-info">
          <h3 class="school-name">{{ school.name }}</h3>
          <p class="school-slug">{{ school.slugName }}</p>

          <div class="badge-row">
            <!-- Status badge -->
            <span class="badge" [class.badge-active]="school.isActive" [class.badge-inactive]="!school.isActive">
              <span class="badge-dot"></span>
              {{ school.isActive ? 'Active' : 'Inactive' }}
            </span>
            <!-- Type badge -->
            <span class="badge badge-type">
              <mat-icon class="badge-icon">category</mat-icon>
              {{ getTypeLabel(school.schoolType) }}
            </span>
            <!-- Category badge -->
            <span class="badge badge-cat">
              <mat-icon class="badge-icon">home_work</mat-icon>
              {{ getCategoryLabel(school.category) }}
            </span>
          </div>
        </div>
      </div>

      <!-- ── Info grid ──────────────────────────────────────────────────── -->
      <div class="info-grid">

        <!-- Contact Details -->
        <div class="info-card" *ngIf="school.email || school.phoneNumber">
          <div class="card-header">
            <mat-icon class="card-icon">contact_phone</mat-icon>
            <span>Contact</span>
          </div>
          <div class="card-rows">
            <div class="card-row" *ngIf="school.email">
              <mat-icon class="row-icon">email</mat-icon>
              <div>
                <p class="row-label">Email</p>
                <p class="row-value">
                  <a [href]="'mailto:' + school.email" class="link">{{ school.email }}</a>
                </p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.phoneNumber">
              <mat-icon class="row-icon">phone</mat-icon>
              <div>
                <p class="row-label">Phone</p>
                <p class="row-value">{{ school.phoneNumber }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Location -->
        <div class="info-card" *ngIf="school.address || school.county || school.subCounty">
          <div class="card-header">
            <mat-icon class="card-icon">location_on</mat-icon>
            <span>Location</span>
          </div>
          <div class="card-rows">
            <div class="card-row" *ngIf="school.address">
              <mat-icon class="row-icon">map</mat-icon>
              <div>
                <p class="row-label">Address</p>
                <p class="row-value">{{ school.address }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.county">
              <mat-icon class="row-icon">location_city</mat-icon>
              <div>
                <p class="row-label">County</p>
                <p class="row-value">{{ school.county }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.subCounty">
              <mat-icon class="row-icon">near_me</mat-icon>
              <div>
                <p class="row-label">Sub-County</p>
                <p class="row-value">{{ school.subCounty }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Legal / Registration -->
        <div class="info-card"
             *ngIf="school.registrationNumber || school.knecCenterCode || school.kraPin">
          <div class="card-header">
            <mat-icon class="card-icon">gavel</mat-icon>
            <span>Legal &amp; Registration</span>
          </div>
          <div class="card-rows">
            <div class="card-row" *ngIf="school.registrationNumber">
              <mat-icon class="row-icon">badge</mat-icon>
              <div>
                <p class="row-label">Registration No.</p>
                <p class="row-value mono">{{ school.registrationNumber }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.knecCenterCode">
              <mat-icon class="row-icon">confirmation_number</mat-icon>
              <div>
                <p class="row-label">KNEC Center Code</p>
                <p class="row-value mono">{{ school.knecCenterCode }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.kraPin">
              <mat-icon class="row-icon">receipt_long</mat-icon>
              <div>
                <p class="row-label">KRA PIN</p>
                <p class="row-value mono">{{ school.kraPin }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- System Info -->
        <div class="info-card">
          <div class="card-header">
            <mat-icon class="card-icon">info</mat-icon>
            <span>System Info</span>
          </div>
          <div class="card-rows">
            <div class="card-row">
              <mat-icon class="row-icon">tag</mat-icon>
              <div>
                <p class="row-label">School ID</p>
                <p class="row-value mono small">{{ school.id }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.createdOn">
              <mat-icon class="row-icon">calendar_today</mat-icon>
              <div>
                <p class="row-label">Created</p>
                <p class="row-value">{{ school.createdOn | date:'mediumDate' }}</p>
              </div>
            </div>
            <div class="card-row" *ngIf="school.createdOn">
              <mat-icon class="row-icon">update</mat-icon>
              <div>
                <p class="row-label">Last Updated</p>
                <p class="row-value">{{ school.updatedOn | date:'mediumDate' }}</p>
              </div>
            </div>
          </div>
        </div>

      </div><!-- /info-grid -->

      <!-- Empty hint if minimal data -->
      <div class="empty-hint"
           *ngIf="!school.email && !school.phoneNumber && !school.address
                  && !school.county && !school.registrationNumber">
        <mat-icon>info_outline</mat-icon>
        <span>No additional details have been added for this school yet.</span>
      </div>

    </div><!-- /view-body -->

    <!-- ── Footer ──────────────────────────────────────────────────────── -->
    <div class="view-footer">
      <span class="footer-meta">
        ID: <span class="mono">{{ school.id | slice:0:8 }}…</span>
      </span>
      <div class="footer-actions">
        <button mat-stroked-button class="edit-btn" (click)="edit()">
          <mat-icon>edit</mat-icon>
          Edit School
        </button>
        <button mat-flat-button class="close-action-btn" (click)="close()">
          Close
        </button>
      </div>
    </div>
  `,
  styles: [`
    // ── Variables ────────────────────────────────────────────────────────
    :host {
      --primary:      #4f46e5;
      --primary-lt:   #e0e7ff;
      --success:      #16a34a;
      --success-lt:   #dcfce7;
      --danger:       #dc2626;
      --danger-lt:    #fee2e2;
      --text:         #111827;
      --muted:        #6b7280;
      --border:       #e5e7eb;
      --bg:           #f9fafb;
      --radius:       12px;
      --radius-sm:    8px;

      display:        flex;
      flex-direction: column;
      width:          100%;
      height:         100%;
      overflow:       hidden;
      background:     #ffffff;
    }

    // ── Header ───────────────────────────────────────────────────────────
    .view-header {
      display:         flex;
      align-items:     center;
      justify-content: space-between;
      padding:         18px 24px;
      background:      linear-gradient(135deg, #4f46e5 0%, #7c3aed 60%, #6d28d9 100%);
      flex-shrink:     0;
      gap:             12px;
      border-bottom:   1px solid rgba(255,255,255,.1);
    }
    .header-left {
      display:     flex;
      align-items: center;
      gap:         12px;
      min-width:   0;
      flex:        1;
    }
    .header-icon-ring {
      width:           44px;
      height:          44px;
      border-radius:   50%;
      background:      rgba(255,255,255,.2);
      display:         flex;
      align-items:     center;
      justify-content: center;
      flex-shrink:     0;
      border:          1px solid rgba(255,255,255,.3);
      box-shadow:      0 4px 12px rgba(0,0,0,.15);
    }
    .header-icon { color: #fff; font-size: 20px; width: 20px; height: 20px; }
    .header-text { min-width: 0; flex: 1; }
    .header-title {
      font-size: 17px; font-weight: 700; color: #fff; margin: 0;
      line-height: 1.3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .header-subtitle {
      font-size: 12.5px; color: rgba(255,255,255,.8); margin: 2px 0 0;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .close-btn {
      color: rgba(255,255,255,.85); flex-shrink: 0; transition: all .2s;
      &:hover { color: #fff; background: rgba(255,255,255,.15); transform: scale(1.05); }
    }

    // ── Body ─────────────────────────────────────────────────────────────
    .view-body {
      flex:       1;
      min-height: 0;
      overflow-y: auto;
      padding:    24px;
      background: var(--bg);

      scrollbar-width: thin;
      scrollbar-color: #cbd5e1 transparent;
      &::-webkit-scrollbar       { width: 6px; }
      &::-webkit-scrollbar-track { background: transparent; }
      &::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 4px;
        &:hover { background: #94a3b8; } }
    }

    // ── Hero ─────────────────────────────────────────────────────────────
    .hero-section {
      display:       flex;
      align-items:   flex-start;
      gap:           20px;
      margin-bottom: 24px;
      padding:       20px;
      background:    #ffffff;
      border:        1px solid var(--border);
      border-radius: var(--radius);
      box-shadow:    0 1px 3px rgba(0,0,0,.06);
      animation:     slideIn .25s ease;
    }

    .logo-wrap { flex-shrink: 0; }
    .logo-img {
      width:         80px;
      height:        80px;
      object-fit:    contain;
      border-radius: var(--radius-sm);
      border:        1px solid var(--border);
      background:    #fff;
      padding:       6px;
    }
    .logo-placeholder {
      width:           80px;
      height:          80px;
      border-radius:   var(--radius-sm);
      background:      linear-gradient(135deg, #e0e7ff, #ddd6fe);
      display:         flex;
      align-items:     center;
      justify-content: center;
      mat-icon { font-size: 36px; width: 36px; height: 36px; color: #6366f1; }
    }

    .hero-info { flex: 1; min-width: 0; }
    .school-name {
      font-size: 20px; font-weight: 700; color: var(--text);
      margin: 0 0 4px; line-height: 1.3;
    }
    .school-slug {
      font-size: 13px; color: var(--muted); margin: 0 0 12px;
      font-family: 'Courier New', monospace;
    }

    .badge-row { display: flex; flex-wrap: wrap; gap: 8px; }
    .badge {
      display:       inline-flex;
      align-items:   center;
      gap:           5px;
      padding:       4px 10px;
      border-radius: 99px;
      font-size:     12px;
      font-weight:   600;
      border:        1px solid transparent;
    }
    .badge-active   { background: var(--success-lt); color: var(--success); border-color: #bbf7d0; }
    .badge-inactive { background: var(--danger-lt);  color: var(--danger);  border-color: #fecaca; }
    .badge-type     { background: #ede9fe; color: #6d28d9; border-color: #ddd6fe; }
    .badge-cat      { background: #fef3c7; color: #92400e; border-color: #fde68a; }
    .badge-dot {
      width: 7px; height: 7px; border-radius: 50%;
      background: currentColor; flex-shrink: 0;
    }
    .badge-icon { font-size: 13px; width: 13px; height: 13px; }

    // ── Info grid ─────────────────────────────────────────────────────────
    .info-grid {
      display:               grid;
      grid-template-columns: 1fr 1fr;
      gap:                   16px;
    }

    .info-card {
      background:    #ffffff;
      border:        1px solid var(--border);
      border-radius: var(--radius-sm);
      overflow:      hidden;
      box-shadow:    0 1px 3px rgba(0,0,0,.05);
      animation:     slideIn .3s ease;
    }

    .card-header {
      display:     flex;
      align-items: center;
      gap:         8px;
      padding:     12px 16px;
      background:  linear-gradient(to right, #fafbff, #f5f3ff);
      border-bottom: 1px solid var(--border);
      font-size:   12px;
      font-weight: 700;
      color:       var(--muted);
      text-transform: uppercase;
      letter-spacing: .05em;
    }
    .card-icon { font-size: 16px; width: 16px; height: 16px; color: var(--primary); }

    .card-rows { padding: 8px 0; }
    .card-row {
      display:     flex;
      align-items: flex-start;
      gap:         10px;
      padding:     10px 16px;
      transition:  background .15s;
      &:hover { background: var(--bg); }
      & + .card-row { border-top: 1px solid #f3f4f6; }
    }
    .row-icon {
      font-size: 17px; width: 17px; height: 17px;
      color: #9ca3af; flex-shrink: 0; margin-top: 2px;
    }
    .row-label {
      font-size: 11px; font-weight: 600; color: var(--muted);
      text-transform: uppercase; letter-spacing: .04em; margin: 0 0 2px;
    }
    .row-value {
      font-size: 14px; color: var(--text); margin: 0; line-height: 1.4;
      &.mono  { font-family: 'Courier New', monospace; font-size: 13px; }
      &.small { font-size: 12px; word-break: break-all; }
    }
    .link {
      color: var(--primary); text-decoration: none;
      &:hover { text-decoration: underline; }
    }

    .empty-hint {
      display:         flex;
      align-items:     center;
      gap:             8px;
      margin-top:      16px;
      padding:         14px 16px;
      background:      #fffbeb;
      border:          1px solid #fde68a;
      border-radius:   var(--radius-sm);
      font-size:       13px;
      color:           #92400e;
      mat-icon { font-size: 18px; width: 18px; height: 18px; color: #d97706; }
    }

    // ── Footer ───────────────────────────────────────────────────────────
    .view-footer {
      display:         flex;
      align-items:     center;
      justify-content: space-between;
      gap:             12px;
      padding:         14px 24px;
      border-top:      2px solid var(--border);
      background:      linear-gradient(to bottom, #ffffff, #fafbfc);
      flex-shrink:     0;
    }
    .footer-meta {
      font-size: 12px; color: var(--muted);
      .mono { font-family: 'Courier New', monospace; }
    }
    .footer-actions { display: flex; align-items: center; gap: 10px; }

    .edit-btn {
      font-size: 13.5px; font-weight: 500;
      color: var(--primary); border-color: var(--primary-lt);
      display: flex; align-items: center; gap: 6px;
      transition: all .2s;
      &:hover { background: var(--primary-lt); }
      mat-icon { font-size: 17px; width: 17px; height: 17px; }
    }

    .close-action-btn {
      font-size:     13.5px;
      font-weight:   600;
      background:    linear-gradient(135deg, var(--primary) 0%, #7c3aed 100%);
      color:         #fff;
      padding:       0 20px;
      height:        38px;
      border-radius: 8px;
      transition:    all .2s;
      box-shadow:    0 3px 10px rgba(79,70,229,.25);
      &:hover { opacity: .9; transform: translateY(-1px); }
    }

    // ── Animations ───────────────────────────────────────────────────────
    @keyframes slideIn {
      from { opacity: 0; transform: translateY(8px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    // ── Responsive ───────────────────────────────────────────────────────
    @media (max-width: 580px) {
      .view-body { padding: 16px; }
      .hero-section { flex-direction: column; align-items: center; text-align: center; }
      .badge-row { justify-content: center; }
      .info-grid { grid-template-columns: 1fr; }
      .view-footer { flex-wrap: wrap;
        .footer-meta   { width: 100%; }
        .footer-actions { width: 100%; justify-content: flex-end; }
      }
    }
  `]
})
export class SchoolViewDialogComponent implements OnInit, OnDestroy {
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private _unsubscribe = new Subject<void>();

  school: SchoolDto;
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
    dialogRef.addPanelClass(['school-view-dialog', 'responsive-dialog']);
  }

  ngOnInit(): void {
    this._resolveLogoUrl();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

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

  edit(): void   { this.dialogRef.close({ action: 'edit', school: this.school }); }
  close(): void  { this.dialogRef.close(); }
}