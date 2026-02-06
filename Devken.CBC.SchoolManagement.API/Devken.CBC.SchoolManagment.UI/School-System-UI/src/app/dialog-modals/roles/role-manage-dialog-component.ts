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
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { UserRole, UserWithRoles } from 'app/core/DevKenService/Types/roles';

export interface RoleManageDialogData {
    user: UserWithRoles;
    availableRoles: UserRole[];
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
        ScrollingModule
    ],
    template: `
<div class="flex flex-col h-[80vh] max-h-[820px] bg-white dark:bg-gray-900 rounded-lg overflow-hidden">

    <!-- HEADER -->
    <div class="flex items-center justify-between px-6 py-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white">
        <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center">
                <mat-icon [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
            </div>
            <div>
                <h2 class="text-lg font-semibold">Manage User Roles</h2>
                <p class="text-xs text-white/80">{{ data.user.fullName }}</p>
            </div>
        </div>

        <button mat-icon-button [mat-dialog-close]>
            <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
        </button>
    </div>

    <!-- USER INFO -->
    <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800">
        <div class="flex justify-between items-center">
            <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-full bg-primary-600 text-white flex items-center justify-center text-lg font-bold">
                    {{ getUserInitials(data.user.fullName) }}
                </div>

                <div>
                    <div class="font-semibold">{{ data.user.fullName }}</div>
                    <div class="text-xs text-secondary flex items-center gap-1">
                        <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:envelope'"></mat-icon>
                        {{ data.user.email }}
                    </div>
                </div>
            </div>

            <div class="text-right">
                <div class="text-2xl font-bold text-primary">{{ selectedRoleIds.size }}</div>
                <div class="text-xs text-secondary">Selected</div>
            </div>
        </div>

        <!-- CURRENT ROLES -->
        <div class="mt-4 border-t pt-3">
            <button mat-button class="w-full justify-between" (click)="showCurrentRoles = !showCurrentRoles">
                <span class="text-sm font-medium">Current Roles ({{ data.user.roles.length }})</span>
                <mat-icon [class.rotate-180]="showCurrentRoles">expand_more</mat-icon>
            </button>

            <div *ngIf="showCurrentRoles" class="flex flex-wrap gap-2 mt-2 max-h-28 overflow-auto">
                <mat-chip *ngFor="let role of data.user.roles; trackBy: trackByRoleId">
                    {{ role.roleName }}
                </mat-chip>

                <span *ngIf="!data.user.roles.length" class="text-xs italic text-secondary">
                    No roles assigned
                </span>
            </div>
        </div>
    </div>

    <mat-divider></mat-divider>

    <!-- SEARCH -->
    <div class="px-6 py-4">
        <mat-form-field class="w-full">
            <mat-label>Search roles</mat-label>
            <input
                matInput
                [(ngModel)]="searchText"
                (ngModelChange)="onSearchChange($event)"
                placeholder="Search by name or description"
            />
            <button *ngIf="searchText" matSuffix mat-icon-button (click)="clearSearch()">
                <mat-icon>close</mat-icon>
            </button>
        </mat-form-field>

        <div class="flex gap-2 mt-2">
            <button mat-stroked-button (click)="selectAll()">Select All</button>
            <button mat-stroked-button (click)="deselectAll()">Clear</button>
            <button mat-stroked-button color="warn" (click)="resetToInitial()" [disabled]="!hasChanges()">Reset</button>
        </div>
    </div>

    <mat-divider></mat-divider>

    <!-- ROLE LIST -->
    <div class="flex-1 px-6 overflow-hidden">
        <div *ngIf="isFiltering" class="flex justify-center py-10">
            <mat-spinner diameter="36"></mat-spinner>
        </div>

        <cdk-virtual-scroll-viewport
            *ngIf="!isFiltering && filteredRoles.length"
            itemSize="80"
            class="h-full">

            <div
                *cdkVirtualFor="let role of filteredRoles; trackBy: trackByRoleId"
                class="flex items-start gap-3 p-4 mb-2 border rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800"
                [class.border-primary-500]="isRoleSelected(role.roleId)"
                (click)="toggleRole(role.roleId)">

                <mat-checkbox
                    [checked]="isRoleSelected(role.roleId)"
                    (click)="$event.stopPropagation()"
                    (change)="toggleRole(role.roleId)">
                </mat-checkbox>

                <div class="flex-1">
                    <div class="font-semibold flex items-center gap-2">
                        <mat-icon
                            class="icon-size-5"
                            [svgIcon]="role.isSystemRole ? 'heroicons_outline:lock-closed' : 'heroicons_outline:shield-check'">
                        </mat-icon>
                        {{ role.roleName }}
                    </div>

                    <div class="text-xs text-secondary mt-1">
                        {{ role.description || 'No description provided' }}
                    </div>

                    <div class="flex gap-2 mt-1">
                        <span *ngIf="role.isSystemRole"
                              class="text-xs px-2 py-0.5 rounded bg-amber-100 text-amber-800">
                            System Role
                        </span>
                        <span class="text-xs text-secondary">
                            {{ role.permissionCount || 0 }} permissions
                        </span>
                    </div>
                </div>
            </div>
        </cdk-virtual-scroll-viewport>

        <div *ngIf="!isFiltering && !filteredRoles.length" class="text-center py-12 text-secondary">
            No roles found
        </div>
    </div>

    <mat-divider></mat-divider>

    <!-- FOOTER -->
    <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800 flex justify-between items-center">
        <div>
            <div class="text-sm font-medium">
                {{ selectedRoleIds.size }} of {{ data.availableRoles.length }} selected
            </div>
            <div class="text-xs text-secondary">{{ getChangesSummary() }}</div>
        </div>

        <div class="flex gap-2">
            <button mat-stroked-button [mat-dialog-close]>Cancel</button>
            <button mat-flat-button color="primary"
                    [disabled]="!hasChanges() || isSaving"
                    (click)="confirmSelection()">
                <mat-spinner *ngIf="isSaving" diameter="18" class="mr-2"></mat-spinner>
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
    `]
})
export class RoleManageDialogComponent implements OnInit, OnDestroy {

    searchText = '';
    filteredRoles: UserRole[] = [];

    selectedRoleIds = new Set<string>();
    initialRoleIds = new Set<string>();

    showCurrentRoles = false;
    isFiltering = false;
    isSaving = false;

    private search$ = new Subject<string>();
    private destroy$ = new Subject<void>();

    constructor(
        private dialogRef: MatDialogRef<RoleManageDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RoleManageDialogData
    ) {}

    ngOnInit(): void {
        // Initialize selected roles from user
        this.data.user.roles.forEach(r => {
            this.selectedRoleIds.add(r.roleId);
            this.initialRoleIds.add(r.roleId);
        });

        this.filteredRoles = [...this.data.availableRoles];

        // Listen to search input with debounce
        this.search$
            .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
            .subscribe(value => this.filterRoles(value));
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    getUserInitials(name: string): string {
        if (!name) return '??';
        const parts = name.trim().split(' ');
        return (parts[0][0] + (parts[1]?.[0] || '')).toUpperCase();
    }

    onSearchChange(value: string): void {
        this.isFiltering = true;
        this.search$.next(value);
    }

    clearSearch(): void {
        this.searchText = '';
        this.filterRoles('');
    }

    filterRoles(term: string): void {
        const t = term.toLowerCase().trim();
        this.filteredRoles = t
            ? this.data.availableRoles.filter(r =>
                r.roleName.toLowerCase().includes(t) ||
                r.description?.toLowerCase().includes(t))
            : [...this.data.availableRoles];

        this.isFiltering = false;
    }

    toggleRole(roleId: string): void {
        this.selectedRoleIds.has(roleId)
            ? this.selectedRoleIds.delete(roleId)
            : this.selectedRoleIds.add(roleId);
    }

    isRoleSelected(id: string): boolean {
        return this.selectedRoleIds.has(id);
    }

    selectAll(): void {
        this.filteredRoles.forEach(r => this.selectedRoleIds.add(r.roleId));
    }

    deselectAll(): void {
        this.selectedRoleIds.clear();
    }

    resetToInitial(): void {
        this.selectedRoleIds = new Set(this.initialRoleIds);
    }

    hasChanges(): boolean {
        return this.getChangesSummary() !== 'No changes';
    }

    getChangesSummary(): string {
        const added = [...this.selectedRoleIds].filter(id => !this.initialRoleIds.has(id)).length;
        const removed = [...this.initialRoleIds].filter(id => !this.selectedRoleIds.has(id)).length;
        if (!added && !removed) return 'No changes';
        return `${added ? `+${added} added` : ''} ${removed ? `-${removed} removed` : ''}`.trim();
    }

    trackByRoleId(_: number, role: UserRole): string {
        return role.roleId;
    }

    /** 
     * CONFIRM SELECTION
     * Only send the **updated roles** (newly added roles) 
     * while ignoring roles already assigned to the user
     */
    confirmSelection(): void {
        this.isSaving = true;

        // Compute new roles only
        const updatedRoles = [...this.selectedRoleIds].filter(id => !this.initialRoleIds.has(id));

        setTimeout(() => {
            this.dialogRef.close(updatedRoles);
        }, 300);
    }
}
