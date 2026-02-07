import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { UserRole, UserWithRoles } from 'app/core/DevKenService/Types/roles';

export interface RoleManageDialogData {
    user: UserWithRoles;
    availableRoles: UserRole[];
    allUsers?: UserWithRoles[];
}

@Component({
    selector: 'app-role-manage-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatInputModule,
        MatDividerModule,
        MatTooltipModule,
        MatChipsModule,
        MatProgressSpinnerModule,
        MatBadgeModule,
        ScrollingModule
    ],
    template: `
<div class="flex flex-col h-[85vh] max-h-[900px] bg-white dark:bg-gray-900 rounded-lg overflow-hidden shadow-xl">

    <!-- HEADER -->
    <div class="flex items-center justify-between px-6 py-4 bg-gradient-to-r from-primary-600 to-primary-700">
        <div class="flex items-center gap-3">
            <div class="w-12 h-12 rounded-full bg-white/20 flex items-center justify-center">
                <mat-icon class="text-white" svgIcon="heroicons_outline:shield-check"></mat-icon>
            </div>
            <div>
                <h2 class="text-xl font-semibold text-white">Manage User Roles</h2>
                <p class="text-sm text-primary-100">{{ data.user.fullName }}</p>
            </div>
        </div>

        <button mat-icon-button [mat-dialog-close] class="text-white">
            <mat-icon svgIcon="heroicons_outline:x-mark"></mat-icon>
        </button>
    </div>

    <!-- SEARCH -->
    <div class="px-6 py-4">
        <mat-form-field class="w-full">
            <mat-label>Search roles</mat-label>
            <input matInput [(ngModel)]="searchText" (ngModelChange)="onSearchChange($event)" />
            <mat-icon matPrefix svgIcon="heroicons_outline:magnifying-glass"></mat-icon>
            <button *ngIf="searchText" matSuffix mat-icon-button (click)="clearSearch()">
                <mat-icon>close</mat-icon>
            </button>
        </mat-form-field>
    </div>

    <mat-divider></mat-divider>

    <!-- ROLE LIST -->
    <div class="flex-1 px-6 bg-gray-50 dark:bg-gray-900 overflow-hidden">

        <div *ngIf="isFiltering" class="flex justify-center items-center py-20">
            <mat-spinner diameter="40"></mat-spinner>
        </div>

        <cdk-virtual-scroll-viewport
            *ngIf="!isFiltering && filteredRoles.length"
            itemSize="120"
            class="h-full py-4 role-viewport">

            <div
                *cdkVirtualFor="let role of filteredRoles; trackBy: trackByRoleId"
                class="role-item flex items-start gap-4 p-5 mb-4 border rounded-xl cursor-pointer transition-all
                       bg-white dark:bg-gray-800"
                [class.border-primary-400]="isRoleSelected(role.id)"
                [class.bg-primary-50]="isRoleSelected(role.id)"
                (click)="toggleRole(role.id)">

                <mat-checkbox
                    [checked]="isRoleSelected(role.id)"
                    (click)="$event.stopPropagation()"
                    (change)="toggleRole(role.id)"
                    class="mt-1 shrink-0">
                </mat-checkbox>

                <div class="flex-1 min-w-0">
                    <div class="flex justify-between gap-4">
                        <div>
                            <div class="font-semibold text-base text-gray-900 dark:text-gray-100 flex items-center gap-2">
                                <mat-icon
                                    class="icon-size-5"
                                    [svgIcon]="role.isSystemRole
                                        ? 'heroicons_outline:lock-closed'
                                        : 'heroicons_outline:shield-check'">
                                </mat-icon>
                                {{ role.name }}
                            </div>

                            <div class="text-sm text-gray-600 dark:text-gray-400 mt-2 line-clamp-2">
                                {{ role.description || 'No description provided' }}
                            </div>

                            <div class="flex flex-wrap gap-2 mt-3">
                                <span *ngIf="role.isSystemRole"
                                      class="px-2 py-1 text-xs rounded-md bg-amber-100 text-amber-800">
                                    System Role
                                </span>

                                <span *ngIf="getRoleUserCount(role.id) > 0"
                                      class="px-2 py-1 text-xs rounded-md bg-gray-200 text-gray-700">
                                    {{ getRoleUserCount(role.id) }} users
                                </span>
                            </div>
                        </div>

                        <mat-icon
                            *ngIf="isRoleSelected(role.id)"
                            class="text-primary-600 icon-size-6"
                            svgIcon="heroicons_solid:check-circle">
                        </mat-icon>
                    </div>
                </div>
            </div>
        </cdk-virtual-scroll-viewport>

        <div *ngIf="!isFiltering && !filteredRoles.length"
             class="text-center py-16 text-gray-500">
            <p class="text-lg font-medium">No roles found</p>
        </div>

    </div>

    <mat-divider></mat-divider>

    <!-- FOOTER -->
    <div class="px-6 py-4 bg-white border-t">
        <div class="flex gap-3">
            <button mat-stroked-button [mat-dialog-close] class="flex-1">Cancel</button>
            <button mat-flat-button color="primary"
                    [disabled]="!hasChanges() || isSaving"
                    (click)="confirmSelection()"
                    class="flex-1">
                Update Roles
            </button>
        </div>
    </div>
</div>
`,
    styles: [`
        :host ::ng-deep .cdk-virtual-scroll-viewport {
            height: 100%;
        }

        :host ::ng-deep .cdk-virtual-scroll-content-wrapper {
            display: flex;
            flex-direction: column;
        }

        .role-item {
            min-height: 120px;
        }

        .role-viewport {
            padding-bottom: 1rem;
        }

        .icon-size-5 { width: 1.25rem; height: 1.25rem; }
        .icon-size-6 { width: 1.5rem; height: 1.5rem; }

        .line-clamp-2 {
            display: -webkit-box;
            -webkit-line-clamp: 2;
            -webkit-box-orient: vertical;
            overflow: hidden;
        }
    `]
})
export class RoleManageDialogComponent implements OnInit, OnDestroy {

    searchText = '';
    filteredRoles: UserRole[] = [];

    selectedRoleIds = new Set<string>();
    initialRoleIds = new Set<string>();
    roleUserCounts = new Map<string, number>();

    isFiltering = false;
    isSaving = false;

    private search$ = new Subject<string>();
    private destroy$ = new Subject<void>();

    constructor(
        private dialogRef: MatDialogRef<RoleManageDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RoleManageDialogData
    ) {}

    ngOnInit(): void {
        this.data.user.roles?.forEach(r => {
            this.selectedRoleIds.add(r.roleId);
            this.initialRoleIds.add(r.roleId);
        });

        if (this.data.allUsers) {
            this.data.allUsers.forEach(u =>
                u.roles?.forEach(r =>
                    this.roleUserCounts.set(r.roleId,
                        (this.roleUserCounts.get(r.roleId) || 0) + 1
                    )
                )
            );
        }

        this.filteredRoles = [...this.data.availableRoles];

        this.search$
            .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
            .subscribe(v => this.filterRoles(v));
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    onSearchChange(v: string): void {
        this.isFiltering = true;
        this.search$.next(v);
    }

    clearSearch(): void {
        this.searchText = '';
        this.filterRoles('');
    }

    filterRoles(term: string): void {
        const t = term.toLowerCase();
        this.filteredRoles = t
            ? this.data.availableRoles.filter(r =>
                r.name.toLowerCase().includes(t) ||
                r.description?.toLowerCase().includes(t))
            : [...this.data.availableRoles];
        this.isFiltering = false;
    }

    toggleRole(id: string): void {
        this.selectedRoleIds.has(id)
            ? this.selectedRoleIds.delete(id)
            : this.selectedRoleIds.add(id);
    }

    isRoleSelected(id: string): boolean {
        return this.selectedRoleIds.has(id);
    }

    hasChanges(): boolean {
        return (
            [...this.selectedRoleIds].some(id => !this.initialRoleIds.has(id)) ||
            [...this.initialRoleIds].some(id => !this.selectedRoleIds.has(id))
        );
    }

    getRoleUserCount(id: string): number {
        return this.roleUserCounts.get(id) || 0;
    }

    trackByRoleId(_: number, role: any): string {
        return role.roleId || role.id;
    }

    confirmSelection(): void {
        this.isSaving = true;
        setTimeout(() => {
            this.dialogRef.close(Array.from(this.selectedRoleIds));
        }, 300);
    }
}
