import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { UserRole, UserWithRoles } from 'app/core/DevKenService/Types/roles';

export interface RoleManageDialogData {
    user: UserWithRoles;
    availableRoles: UserRole[];
    allUsers?: UserWithRoles[];  // Optional: to calculate user counts per role
}

@Component({
    selector: 'app-role-manage-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatInputModule,
        MatDividerModule,
        MatTooltipModule,
        MatChipsModule
    ],
    template: `
<div class="flex flex-col max-h-[90vh]">

    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b bg-gradient-to-r from-primary-50 to-primary-100 dark:from-primary-900/20 dark:to-primary-800/20">
        <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-primary-500 dark:bg-primary-600 flex items-center justify-center">
                <mat-icon class="text-white" [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
            </div>
            <div>
                <h2 class="text-xl font-semibold">Manage User Roles</h2>
                <p class="text-xs text-secondary">Assign or remove role assignments</p>
            </div>
        </div>
        <button mat-icon-button [mat-dialog-close] class="text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700">
            <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
        </button>
    </div>

    <!-- User Info -->
    <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800">
        <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center shadow-md">
                    <span class="text-white font-bold text-lg">{{ getUserInitials(data.user.fullName) }}</span>
                </div>
                <div>
                    <p class="font-semibold text-gray-900 dark:text-gray-100">{{ data.user.fullName }}</p>
                    <p class="text-sm text-secondary flex items-center gap-1">
                        <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:envelope'"></mat-icon>
                        {{ data.user.email }}
                    </p>
                </div>
            </div>
            <div class="text-right">
                <div class="text-sm font-medium text-gray-600 dark:text-gray-400">Current Roles</div>
                <div class="text-2xl font-bold text-primary-600 dark:text-primary-400">{{ getCurrentRoleCount() }}</div>
            </div>
        </div>

        <!-- Current Roles Chips -->
        <div class="mt-3 flex flex-wrap gap-2" *ngIf="getCurrentRoleCount() > 0">
            <mat-chip *ngFor="let roleId of assignedRoleIds; trackBy: trackByRoleId" 
                      class="bg-primary-100 text-primary-800 dark:bg-primary-900/30 dark:text-primary-300">
                {{ getRoleName(roleId) }}
            </mat-chip>
        </div>
    </div>

    <mat-divider></mat-divider>

    <!-- Search -->
    <div class="px-6 py-4">
        <mat-form-field class="w-full">
            <mat-label>Search roles</mat-label>
            <input
                matInput
                [(ngModel)]="searchText"
                (ngModelChange)="filterRoles()"
                placeholder="Type to filter roles..."
                autocomplete="off">
            <mat-icon matPrefix [svgIcon]="'heroicons_outline:magnifying-glass'"></mat-icon>
            <button *ngIf="searchText" matSuffix mat-icon-button (click)="clearSearch()" matTooltip="Clear search">
                <mat-icon>close</mat-icon>
            </button>
        </mat-form-field>
    </div>

    <!-- Roles List -->
    <div class="flex-auto overflow-y-auto px-6 pb-4" style="max-height: 50vh;">
        <div *ngIf="filteredRoles.length === 0" class="text-center py-12 text-secondary">
            <mat-icon class="icon-size-16 mb-3 text-gray-400" [svgIcon]="'heroicons_outline:shield-exclamation'"></mat-icon>
            <p class="text-lg font-medium">No roles found</p>
            <p class="text-sm">Try adjusting your search criteria</p>
        </div>

        <div class="space-y-2">
            <div
                *ngFor="let role of filteredRoles; trackBy: trackByAvailableRoleId"
                class="group relative flex items-start p-4 rounded-lg border transition-all duration-200"
                [class.border-gray-200]="!isRoleCurrentlyAssigned(role.id)"
                [class.dark:border-gray-700]="!isRoleCurrentlyAssigned(role.id)"
                [class.border-primary-300]="isRoleCurrentlyAssigned(role.id) && !isMarkedForRemoval(role.id)"
                [class.dark:border-primary-600]="isRoleCurrentlyAssigned(role.id) && !isMarkedForRemoval(role.id)"
                [class.bg-primary-50]="isRoleCurrentlyAssigned(role.id) && !isMarkedForRemoval(role.id)"
                [class.dark:bg-primary-45]="isRoleCurrentlyAssigned(role.id) && !isMarkedForRemoval(role.id)"
                [class.bg-red-50]="isMarkedForRemoval(role.id)"
                [class.dark:bg-red-45]="isMarkedForRemoval(role.id)"
                [class.border-red-300]="isMarkedForRemoval(role.id)"
                [class.dark:border-red-700]="isMarkedForRemoval(role.id)"
                [class.hover:bg-gray-50]="!isRoleCurrentlyAssigned(role.id)"
                [class.dark:hover:bg-gray-800]="!isRoleCurrentlyAssigned(role.id)">

                <!-- Checkbox -->
                <mat-checkbox
                    [checked]="isRoleCurrentlyAssigned(role.id) && !isMarkedForRemoval(role.id)"
                    (change)="toggleRole(role.id, $event.checked)"
                    class="mt-0.5">
                </mat-checkbox>

                <div class="ml-3 flex-1 min-w-0">
                    <div class="flex items-start justify-between gap-3">
                        <div class="flex-1 min-w-0">
                            <h5 class="font-semibold text-gray-900 dark:text-gray-100 flex items-center gap-2">
                                {{ role.name }}
                                <mat-icon 
                                    *ngIf="originallyAssignedRoleIds.has(role.id) && !isMarkedForRemoval(role.id)"
                                    class="icon-size-5 text-primary-600 dark:text-primary-400"
                                    [svgIcon]="'heroicons_outline:check-circle'"
                                    matTooltip="Currently assigned">
                                </mat-icon>
                                <mat-icon 
                                    *ngIf="isNewlyAdded(role.id)"
                                    class="icon-size-5 text-green-600 dark:text-green-400"
                                    [svgIcon]="'heroicons_outline:plus-circle'"
                                    matTooltip="Will be added">
                                </mat-icon>
                                <mat-icon 
                                    *ngIf="isMarkedForRemoval(role.id)"
                                    class="icon-size-5 text-red-600 dark:text-red-400"
                                    [svgIcon]="'heroicons_outline:minus-circle'"
                                    matTooltip="Will be removed">
                                </mat-icon>
                            </h5>

                            <p *ngIf="role.description" class="text-sm text-secondary mt-1 line-clamp-2">
                                {{ role.description }}
                            </p>

                            <div class="mt-2 flex items-center gap-3 flex-wrap">
                                <span *ngIf="role.isSystemRole"
                                      class="inline-flex items-center px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-100">
                                    <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:lock-closed'"></mat-icon>
                                    System Role
                                </span>

                                <span *ngIf="getRoleUserCount(role.id) > 0"
                                      class="inline-flex items-center px-2 py-0.5 text-xs rounded-full bg-gray-200 text-gray-800 dark:bg-gray-700 dark:text-gray-200"
                                      [matTooltip]="getRoleUserCount(role.id) + ' user(s) have this role'">
                                    <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:users'"></mat-icon>
                                    {{ getRoleUserCount(role.id) }} {{ getRoleUserCount(role.id) === 1 ? 'user' : 'users' }}
                                </span>
                            </div>
                        </div>

                        <!-- Status Badge -->
                        <div class="flex-shrink-0">
                            <span 
                                *ngIf="originallyAssignedRoleIds.has(role.id) && !isMarkedForRemoval(role.id)"
                                class="inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-100">
                                <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:check'"></mat-icon>
                                Assigned
                            </span>
                            <span 
                                *ngIf="isNewlyAdded(role.id)"
                                class="inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100">
                                <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:plus'"></mat-icon>
                                New
                            </span>
                            <span 
                                *ngIf="isMarkedForRemoval(role.id)"
                                class="inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-100">
                                <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:minus'"></mat-icon>
                                Remove
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <mat-divider></mat-divider>

    <!-- Footer -->
    <div class="flex items-center justify-between px-6 py-4 bg-gray-50 dark:bg-gray-800">
        <div class="text-sm">
            <span *ngIf="rolesToAdd.size > 0" class="mr-3">
                <span class="font-medium text-green-600 dark:text-green-400">+{{ rolesToAdd.size }}</span>
                <span class="text-secondary"> to add</span>
            </span>
            <span *ngIf="rolesToRemove.size > 0">
                <span class="font-medium text-red-600 dark:text-red-400">-{{ rolesToRemove.size }}</span>
                <span class="text-secondary"> to remove</span>
            </span>
            <span *ngIf="rolesToAdd.size === 0 && rolesToRemove.size === 0" class="text-secondary">
                No changes
            </span>
        </div>
        <div class="flex gap-2">
            <button mat-stroked-button [mat-dialog-close]>
                Cancel
            </button>
            <button
                mat-flat-button
                color="primary"
                [disabled]="!hasChanges()"
                (click)="confirmSelection()">
                <mat-icon class="mr-1" [svgIcon]="'heroicons_outline:check'"></mat-icon>
                Save Changes
            </button>
        </div>
    </div>

</div>
`,
    styles: [`
        :host { 
            display: block; 
        }
        .icon-size-3 { width: 0.75rem; height: 0.75rem; font-size: 0.75rem; }
        .icon-size-4 { width: 1rem; height: 1rem; font-size: 1rem; }
        .icon-size-5 { width: 1.25rem; height: 1.25rem; font-size: 1.25rem; }
        .icon-size-16 { width: 4rem; height: 4rem; font-size: 4rem; }
        .line-clamp-2 {
            display: -webkit-box;
            -webkit-line-clamp: 2;
            -webkit-box-orient: vertical;
            overflow: hidden;
        }
    `]
})
export class RoleManageDialogComponent implements OnInit {

    searchText = '';
    filteredRoles: UserRole[] = [];

    /** Map of roleId -> user count */
    private roleUserCounts = new Map<string, number>();

    /** Roles originally assigned to the user (when dialog opened) - NEVER CHANGE */
    originallyAssignedRoleIds = new Set<string>();

    /** Current set of assigned roles (updated as user toggles) */
    assignedRoleIds = new Set<string>();

    /** Roles to add (not in original, but currently assigned) */
    rolesToAdd = new Set<string>();

    /** Roles to remove (in original, but not currently assigned) */
    rolesToRemove = new Set<string>();

    constructor(
        private _dialogRef: MatDialogRef<RoleManageDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RoleManageDialogData
    ) {}

    ngOnInit(): void {
        // Store original role assignments - user.roles has 'roleId' property
        if (this.data.user.roles && Array.isArray(this.data.user.roles)) {
            this.data.user.roles.forEach(role => {
                this.originallyAssignedRoleIds.add(role.roleId);
                this.assignedRoleIds.add(role.roleId);
            });
        }

        console.log('Original roles:', Array.from(this.originallyAssignedRoleIds));

        // Calculate user counts per role if allUsers is provided
        if (this.data.allUsers && Array.isArray(this.data.allUsers)) {
            this.calculateRoleUserCounts();
        }

        this.filteredRoles = [...this.data.availableRoles];
    }

    /** Calculate how many users have each role */
    private calculateRoleUserCounts(): void {
        this.roleUserCounts.clear();
        
        this.data.allUsers!.forEach(user => {
            if (user.roles && Array.isArray(user.roles)) {
                user.roles.forEach(role => {
                    const count = this.roleUserCounts.get(role.roleId) || 0;
                    this.roleUserCounts.set(role.roleId, count + 1);
                });
            }
        });
    }

    /** Get number of users with this role */
    getRoleUserCount(roleId: string): number {
        return this.roleUserCounts.get(roleId) || 0;
    }

    /** Get role name by ID */
    getRoleName(roleId: string): string {
        const role = this.data.availableRoles.find(r => r.id === roleId);
        return role ? role.name : 'Unknown Role';
    }

    /** Get user initials */
    getUserInitials(fullName: string): string {
        if (!fullName) return '??';
        const names = fullName.trim().split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }

    /** Get current role count (original + new - removed) */
    getCurrentRoleCount(): number {
        return this.assignedRoleIds.size;
    }

    /** Check if role is currently assigned (could be original or newly added) */
    isRoleCurrentlyAssigned(roleId: string): boolean {
        return this.assignedRoleIds.has(roleId);
    }

    /** Check if role was newly added (not in original) */
    isNewlyAdded(roleId: string): boolean {
        return !this.originallyAssignedRoleIds.has(roleId) && this.assignedRoleIds.has(roleId);
    }

    /** Check if role is marked for removal (in original but not current) */
    isMarkedForRemoval(roleId: string): boolean {
        return this.originallyAssignedRoleIds.has(roleId) && !this.assignedRoleIds.has(roleId);
    }

    /** Toggle role assignment */
    toggleRole(roleId: string, checked: boolean): void {
        if (checked) {
            // Add role
            this.assignedRoleIds.add(roleId);
        } else {
            // Remove role
            this.assignedRoleIds.delete(roleId);
        }

        // Recalculate what needs to be added/removed
        this.calculateChanges();
        
        console.log('Toggle role', roleId, 'checked:', checked);
        console.log('Current assigned roles:', Array.from(this.assignedRoleIds));
        console.log('Roles to add:', Array.from(this.rolesToAdd));
        console.log('Roles to remove:', Array.from(this.rolesToRemove));
    }

    /** Calculate which roles need to be added or removed */
    private calculateChanges(): void {
        this.rolesToAdd.clear();
        this.rolesToRemove.clear();

        // Find roles to add (in current but not in original)
        this.assignedRoleIds.forEach(roleId => {
            if (!this.originallyAssignedRoleIds.has(roleId)) {
                this.rolesToAdd.add(roleId);
            }
        });

        // Find roles to remove (in original but not in current)
        this.originallyAssignedRoleIds.forEach(roleId => {
            if (!this.assignedRoleIds.has(roleId)) {
                this.rolesToRemove.add(roleId);
            }
        });
    }

    /** Check if there are any changes */
    hasChanges(): boolean {
        return this.rolesToAdd.size > 0 || this.rolesToRemove.size > 0;
    }

    /** Filter roles by search text */
    filterRoles(): void {
        const value = this.searchText.toLowerCase().trim();
        if (!value) {
            this.filteredRoles = [...this.data.availableRoles];
            return;
        }
        
        this.filteredRoles = this.data.availableRoles.filter(role =>
            role.name.toLowerCase().includes(value) ||
            (role.description && role.description.toLowerCase().includes(value))
        );
    }

    /** Clear search */
    clearSearch(): void {
        this.searchText = '';
        this.filterRoles();
    }

    /** Track by function for available roles (uses 'id' property) */
    trackByAvailableRoleId(index: number, role: UserRole): string {
        return role.id;
    }

    /** Track by function for role IDs */
    trackByRoleId(index: number, roleId: string): string {
        return roleId;
    }

    /** 
     * CRITICAL: Return ALL current roles (existing + new)
     * Backend will handle removing old ones and adding only new ones without duplicates
     */
    confirmSelection(): void {
        if (!this.hasChanges()) {
            this._dialogRef.close(null);
            return;
        }

        // Return ALL currently assigned roles (original that weren't removed + new ones added)
        const allCurrentRoles = Array.from(this.assignedRoleIds);
        
        console.log('Confirming selection:');
        console.log('  - Original roles:', Array.from(this.originallyAssignedRoleIds));
        console.log('  - Current roles (ALL):', allCurrentRoles);
        console.log('  - Roles to add:', Array.from(this.rolesToAdd));
        console.log('  - Roles to remove:', Array.from(this.rolesToRemove));

        // Return ALL current role IDs - backend UpdateUserRoles will handle the sync
        this._dialogRef.close(allCurrentRoles);
    }
}