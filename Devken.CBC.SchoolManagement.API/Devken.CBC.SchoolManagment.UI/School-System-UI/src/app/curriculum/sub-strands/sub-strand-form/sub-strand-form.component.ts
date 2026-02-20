import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent } from '@fuse/components/alert';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { SubStrandService } from 'app/core/DevKenService/curriculum/substrand.service ';
import { LearningAreaResponseDto } from 'app/curriculum/types/learning-area.dto ';
import { StrandResponseDto } from 'app/curriculum/types/strand.dto ';

@Component({
  selector: 'app-sub-strand-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule, MatProgressSpinnerModule,
    FuseAlertComponent, PageHeaderComponent,
  ],
  templateUrl: './sub-strand-form.component.html',
})
export class SubStrandFormComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private _service = inject(SubStrandService);
  private _strandService = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _schoolService = inject(SchoolService);
  private _router = inject(Router);
  private _route = inject(ActivatedRoute);

  form!: FormGroup;
  isEditMode = false;
  editId: string | null = null;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];
  strands: StrandResponseDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  filteredStrands: StrandResponseDto[] = [];

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard' },
      { label: 'Curriculum' },
      { label: 'Sub-Strands', url: '/curriculum/sub-strands' },
      { label: this.isEditMode ? 'Edit' : 'Create' },
    ];
  }

  get title(): string { return this.isEditMode ? 'Edit Sub-Strand' : 'Create Sub-Strand'; }

  ngOnInit(): void {
    this.editId = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.editId;
    this.buildForm();
    this.loadLookups();
    if (this.isSuperAdmin) {
      this._schoolService.getAll().pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = res?.data ?? []; });
    }
    if (this.isEditMode && this.editId) this.loadExisting(this.editId);
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private buildForm(): void {
    this.form = this.fb.group({
      name:           ['', [Validators.required, Validators.maxLength(150)]],
      learningAreaId: [''], // For filtering only – not in DTO
      strandId:       ['', Validators.required],
      ...(this.isSuperAdmin ? { tenantId: ['', Validators.required] } : {}),
    });

    // When learning area changes, filter strands
    this.form.get('learningAreaId')?.valueChanges.subscribe(laId => {
      this.filteredStrands = laId ? this.strands.filter(s => s.learningAreaId === laId) : this.strands;
      this.form.get('strandId')?.setValue('');
    });
  }

  private loadLookups(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.learningAreas = Array.isArray(data) ? data : []; });
    this._strandService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.strands = Array.isArray(data) ? data : []; this.filteredStrands = this.strands; });
  }

  private loadExisting(id: string): void {
    this.isLoading = true;
    this._service.getById(id).pipe(takeUntil(this._destroy$)).subscribe({
      next: ss => {
        // Find the learning area of this strand to pre-filter
        const strand = this.strands.find(s => s.id === ss.strandId);
        this.form.patchValue({
          name: ss.name,
          learningAreaId: strand?.learningAreaId ?? ss.learningAreaId ?? '',
          strandId: ss.strandId,
          ...(this.isSuperAdmin ? { tenantId: ss.tenantId } : {}),
        });
        this.isLoading = false;
      },
      error: err => { this._alertService.error(err?.error?.message || 'Failed to load'); this.isLoading = false; },
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const dto = { name: val.name, strandId: val.strandId };
    const obs = this.isEditMode && this.editId
      ? this._service.update(this.editId, dto)
      : this._service.create({ ...dto, tenantId: this.isSuperAdmin ? val.tenantId : undefined });

    obs.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this._alertService.success(`Sub-strand ${this.isEditMode ? 'updated' : 'created'} successfully`);
        this._router.navigate(['/curriculum/sub-strands']);
      },
      error: err => { this._alertService.error(err?.error?.message || 'Save failed'); this.isSaving = false; },
    });
  }

  cancel(): void { this._router.navigate(['/curriculum/sub-strands']); }

  isInvalid(f: string): boolean { const c = this.form.get(f); return !!(c && c.invalid && (c.dirty || c.touched)); }
  getError(f: string): string {
    const c = this.form.get(f);
    if (!c?.errors) return '';
    if (c.errors['required']) return 'Required';
    if (c.errors['maxlength']) return 'Too long';
    return 'Invalid';
  }
}