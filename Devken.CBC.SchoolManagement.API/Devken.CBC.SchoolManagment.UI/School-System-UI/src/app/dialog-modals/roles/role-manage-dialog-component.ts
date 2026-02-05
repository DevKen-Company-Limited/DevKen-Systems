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
        <div class="flex flex-col h-[80vh] max-h-[800px]">
            <!-- Header -->
            <div class="flex items-center justify-between px-6 py-4 border-b bg-gradient-to-r from-primary-500 to-primary-600 flex-shrink-0">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center">
                        <mat-icon class="text-white icon-size-6" [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
                    </div>
                    <div>
                        <h2 class="text-xl font-semibold text-white">Manage Roles</h2>
                        <div class="text-sm text-white/80">{{ data.user.fullName }}</div>
                    </div>
                </div>
                <button mat-icon-button [mat-dialog-close] class="text-white">
                    <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                </button>
            </div>

            <!-- User Info -->
            <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800 flex-shrink-0">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-3">
                        <div class="w-12 h-12 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center shadow-sm">
                            <span class="text-white font-bold text-lg">
                                {{ getUserInitials(data.user.fullName) }}
                            </span>
                        </div>
                        <div>
                            <div class="font-semibold text-lg">{{ data.user.fullName }}</div>
                            <div class="text-sm text-secondary flex items-center gap-1">
                                <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:envelope'"></mat-icon>
                                {{ data.user.email }}
                            </div>
                        </div>
                    </div>

                    <div class="text-right">
                        <div class="text-2xl font-bold text-primary">{{ getSelectedCount() }}</div>
                        <div class="text-xs text-secondary">Roles Selected</div>
                    </div>
                </div>

                <!-- Current Roles - Collapsible -->
                <div class="mt-4 pt-4 border-t">
                    <button 
                        mat-button 
                        class="w-full text-left -ml-3"
                        (click)="showCurrentRoles = !showCurrentRoles">
                        <div class="flex items-center justify-between">
                            <div class="text-sm font-medium">Current Roles ({{ data.user.roles.length }})</div>
                            <mat-icon class="icon-size-5 transition-transform" 
                                     [class.rotate-180]="showCurrentRoles">
                                heroicons_outline:chevron-down
                            </mat-icon>
                        </div>
                    </button>
                    
                    <div *ngIf="showCurrentRoles" class="flex flex-wrap gap-2 mt-2 max-h-24 overflow-y-auto">
                        <mat-chip
                            *ngFor="let role of data.user.roles; trackBy: trackByRoleId"
                            class="bg-primary-100 dark:bg-primary-900 text-xs">
                            <mat-icon class="icon-size-4 mr-1" [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
                            {{ role.roleName }}
                        </mat-chip>

                        <span *ngIf="data.user.roles.length === 0" class="text-sm text-secondary italic">
                            No roles assigned
                        </span>
                    </div>
                </div>
            </div>

            <mat-divider></mat-divider>

            <!-- Search & Quick Actions -->
            <div class="px-6 py-4 flex-shrink-0">
                <mat-form-field class="w-full">
                    <mat-label>Search roles</mat-label>
                    <mat-icon matPrefix [svgIcon]="'heroicons_outline:magnifying-glass'"></mat-icon>
                    <input
                        matInput
                        [ngModel]="searchText"
                        (ngModelChange)="onSearchChange($event)"
                        placeholder="Type to filter roles..."
                        autocomplete="off" />
                    <button
                        *ngIf="searchText"
                        mat-icon-button
                        matSuffix
                        (click)="clearSearch()">
                        <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                    </button>
                </mat-form-field>

                <!-- Quick Actions -->
                <div class="flex gap-2 mt-2">
                    <button 
                        mat-stroked-button 
                        class="text-xs"
                        (click)="selectAll()"
                        [disabled]="filteredRoles.length === selectedRoleIds.size">
                        <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:check'"></mat-icon>
                        Select All
                    </button>
                    <button 
                        mat-stroked-button 
                        class="text-xs"
                        (click)="deselectAll()"
                        [disabled]="selectedRoleIds.size === 0">
                        <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                        Clear All
                    </button>
                    <button 
                        mat-stroked-button 
                        class="text-xs"
                        (click)="resetToInitial()"
                        [disabled]="!hasChanges()">
                        <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:arrow-path'"></mat-icon>
                        Reset
                    </button>
                </div>
            </div>

            <mat-divider></mat-divider>

            <!-- Role List with Virtual Scrolling -->
            <div class="flex-1 overflow-hidden px-6">
                <!-- Loading State -->
                <div *ngIf="isFiltering" class="flex items-center justify-center py-12">
                    <mat-spinner diameter="40"></mat-spinner>
                </div>

                <!-- Empty State -->
                <div *ngIf="!isFiltering && filteredRoles.length === 0" class="text-center py-12 text-secondary">
                    <mat-icon class="icon-size-12 mb-2" [svgIcon]="'heroicons_outline:shield-exclamation'"></mat-icon>
                    <div>{{ searchText ? 'No roles match your search' : 'No roles available' }}</div>
                </div>

                <!-- Virtual Scroll List -->
                <cdk-virtual-scroll-viewport 
                    *ngIf="!isFiltering && filteredRoles.length > 0"
                    [itemSize]="76" 
                    class="h-full">
                    
                    <div *cdkVirtualFor="let role of filteredRoles; trackBy: trackByRoleId"
                         class="flex items-start p-4 mb-2 rounded-lg border-2 cursor-pointer transition-all hover:shadow-md"
                         [class.border-primary-500]="isRoleSelected(role.roleId)"
                         [class.bg-primary-50]="isRoleSelected(role.roleId)"
                         [class.dark:bg-primary-45]="isRoleSelected(role.roleId)"
                         [class.border-gray-200]="!isRoleSelected(role.roleId)"
                         [class.dark:border-gray-700]="!isRoleSelected(role.roleId)"
                         (click)="toggleRole(role.roleId)">
                        <mat-checkbox
                            class="mr-3"
                            [checked]="isRoleSelected(role.roleId)"
                            (click)="$event.stopPropagation()"
                            (change)="toggleRole(role.roleId)">
                        </mat-checkbox>

                        <div class="flex-1 min-w-0">
                            <div class="font-semibold flex items-center gap-2">
                                <mat-icon 
                                    class="icon-size-5 flex-shrink-0" 
                                    [svgIcon]="role.isSystemRole ? 'heroicons_outline:lock-closed' : 'heroicons_outline:shield-check'">
                                </mat-icon>
                                <span class="truncate">{{ role.roleName }}</span>
                            </div>
                            <div *ngIf="role.description" class="text-sm text-secondary mt-1 truncate">
                                {{ role.description }}
                            </div>
                            <div class="flex items-center gap-2 mt-1">
                                <span *ngIf="role.isSystemRole" class="text-xs px-2 py-0.5 rounded bg-amber-100 dark:bg-amber-900 text-amber-800 dark:text-amber-200">
                                    System Role
                                </span>
                                <span class="text-xs text-secondary">
                                    {{ role.permissionCount || 0 }} permissions
                                </span>
                            </div>
                        </div>
                    </div>
                    
                </cdk-virtual-scroll-viewport>
            </div>

            <mat-divider></mat-divider>

            <!-- Footer -->
            <div class="flex items-center justify-between px-6 py-4 bg-gray-50 dark:bg-gray-800 flex-shrink-0">
                <div>
                    <div class="text-sm font-medium">
                        {{ getSelectedCount() }} of {{ data.availableRoles.length }} roles selected
                    </div>
                    <div class="text-xs text-secondary">{{ getChangesSummary() }}</div>
                </div>

                <div class="flex gap-2">
                    <button mat-stroked-button [mat-dialog-close]>Cancel</button>
                    <button
                        mat-flat-button
                        color="primary"
                        [disabled]="!hasChanges() || isSaving"
                        (click)="confirmSelection()">
                        <mat-spinner *ngIf="isSaving" diameter="20" class="mr-2"></mat-spinner>
                        {{ isSaving ? 'Updating...' : 'Update Roles' }}
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
            width: 100%;
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

    private _searchSubject = new Subject<string>();
    private _destroy$ = new Subject<void>();

    constructor(
        private _dialogRef: MatDialogRef<RoleManageDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RoleManageDialogData
    ) {}

    ngOnInit(): void {
        // Initialize selected roles
        this.data.user.roles.forEach(r => {
            this.selectedRoleIds.add(r.roleId);
            this.initialRoleIds.add(r.roleId);
        });
        
        this.filteredRoles = [...this.data.availableRoles];

        // Setup debounced search
        this._searchSubject.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            takeUntil(this._destroy$)
        ).subscribe(searchTerm => {
            this.performFilter(searchTerm);
        });
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    getUserInitials(name: string): string {
        if (!name) return '??';
        const n = name.split(' ');
        return n.length > 1 ? (n[0][0] + n[n.length - 1][0]).toUpperCase() : name.slice(0, 2).toUpperCase();
    }

    isRoleSelected(id: string): boolean {
        return this.selectedRoleIds.has(id);
    }

    toggleRole(id: string): void {
        if (this.isRoleSelected(id)) {
            this.selectedRoleIds.delete(id);
        } else {
            this.selectedRoleIds.add(id);
        }
    }

    onSearchChange(value: string): void {
        this.searchText = value;
        this.isFiltering = true;
        this._searchSubject.next(value);
    }

    clearSearch(): void {
        this.searchText = '';
        this.performFilter('');
    }

    performFilter(searchTerm: string): void {
        const t = searchTerm.toLowerCase().trim();
        
        if (!t) {
            this.filteredRoles = [...this.data.availableRoles];
        } else {
            this.filteredRoles = this.data.availableRoles.filter(r =>
                r.roleName.toLowerCase().includes(t) ||
                r.description?.toLowerCase().includes(t)
            );
        }
        
        this.isFiltering = false;
    }

    selectAll(): void {
        this.filteredRoles.forEach(role => this.selectedRoleIds.add(role.roleId));
    }

    deselectAll(): void {
        this.selectedRoleIds.clear();
    }

    resetToInitial(): void {
        this.selectedRoleIds.clear();
        this.initialRoleIds.forEach(id => this.selectedRoleIds.add(id));
    }

    getSelectedCount(): number {
        return this.selectedRoleIds.size;
    }

    hasChanges(): boolean {
        if (this.selectedRoleIds.size !== this.initialRoleIds.size) {
            return true;
        }
        return [...this.selectedRoleIds].some(id => !this.initialRoleIds.has(id));
    }

    getChangesSummary(): string {
        const added = [...this.selectedRoleIds].filter(id => !this.initialRoleIds.has(id)).length;
        const removed = [...this.initialRoleIds].filter(id => !this.selectedRoleIds.has(id)).length;
        
        if (!added && !removed) return 'No changes';
        
        const parts: string[] = [];
        if (added) parts.push(`+${added} added`);
        if (removed) parts.push(`-${removed} removed`);
        
        return parts.join(', ');
    }

    trackByRoleId(index: number, role: UserRole): string {
        return role.roleId;
    }

    confirmSelection(): void {
        this.isSaving = true;
        // Add a small delay to show the loading state
        setTimeout(() => {
            this._dialogRef.close([...this.selectedRoleIds]);
        }, 100);
    }
}