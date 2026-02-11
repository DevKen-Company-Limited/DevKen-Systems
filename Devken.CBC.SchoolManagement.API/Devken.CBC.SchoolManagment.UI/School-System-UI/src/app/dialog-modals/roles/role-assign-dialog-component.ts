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

export interface RoleAssignDialogData {
    user: UserWithRoles;
    availableRoles: UserRole[];
    allUsers?: UserWithRoles[];  // Optional: to calculate user counts per role
    mode: 'assign' | 'update';
}

@Component({
    selector: 'app-role-assign-dialog',
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
                <h2 class="text-xl font-semibold">
                    {{ data.mode === 'assign' ? 'Assign Roles' : 'Update Roles' }}
                </h2>
                <p class="text-xs text-secondary">Manage user role assignments</p>
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
                <div class="text-2xl font-bold text-primary-600 dark:text-primary-400">{{ data.user.roles?.length || 0 }}</div>
            </div>
        </div>

        <!-- Current Roles Chips -->
        <div class="mt-3 flex flex-wrap gap-2" *ngIf="data.user.roles && data.user.roles.length > 0">
            <mat-chip *ngFor="let role of data.user.roles; trackBy: trackByRoleId" 
                      class="bg-primary-100 text-primary-800 dark:bg-primary-900/30 dark:text-primary-300">
                {{ role.roleName }}
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
                [class.border-gray-200]="!isRoleSelected(role.id) && !userHasRole(role.id)"
                [class.dark:border-gray-700]="!isRoleSelected(role.id) && !userHasRole(role.id)"
                [class.border-primary-300]="isRoleSelected(role.id)"
                [class.dark:border-primary-600]="isRoleSelected(role.id)"
                [class.bg-primary-50]="isRoleSelected(role.id)"
                [class.dark:bg-primary-900/20]="isRoleSelected(role.id)"
                [class.bg-green-50]="userHasRole(role.id)"
                [class.dark:bg-green-900/20]="userHasRole(role.id)"
                [class.border-green-300]="userHasRole(role.id)"
                [class.dark:border-green-700]="userHasRole(role.id)"
                [class.hover:bg-gray-50]="!isRoleSelected(role.id) && !userHasRole(role.id)"
                [class.dark:hover:bg-gray-800]="!isRoleSelected(role.id) && !userHasRole(role.id)">

                <!-- Checkbox -->
                <mat-checkbox
                    [disabled]="userHasRole(role.id)"
                    [checked]="isRoleSelected(role.id)"
                    (change)="toggleRole(role.id, $event.checked)"
                    class="mt-0.5"
                    [matTooltip]="userHasRole(role.id) ? 'Already assigned to user' : ''">
                </mat-checkbox>

                <div class="ml-3 flex-1 min-w-0">
                    <div class="flex items-start justify-between gap-3">
                        <div class="flex-1 min-w-0">
                            <h5 class="font-semibold text-gray-900 dark:text-gray-100 flex items-center gap-2">
                                {{ role.name }}
                                <mat-icon 
                                    *ngIf="userHasRole(role.id)"
                                    class="icon-size-5 text-green-600 dark:text-green-400"
                                    [svgIcon]="'heroicons_outline:check-circle'"
                                    matTooltip="Currently assigned">
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
                                *ngIf="userHasRole(role.id)"
                                class="inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100">
                                <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:check'"></mat-icon>
                                Assigned
                            </span>
                            <span 
                                *ngIf="!userHasRole(role.id) && isRoleSelected(role.id)"
                                class="inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-100">
                                <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:plus'"></mat-icon>
                                New
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
            <span class="font-medium text-gray-900 dark:text-gray-100">
                {{ getSelectedCount() }} 
            </span>
            <span class="text-secondary">
                new {{ getSelectedCount() === 1 ? 'role' : 'roles' }} selected
            </span>
        </div>
        <div class="flex gap-2">
            <button mat-stroked-button [mat-dialog-close]>
                Cancel
            </button>
            <button
                mat-flat-button
                color="primary"
                [disabled]="getSelectedCount() === 0"
                (click)="confirmSelection()">
                <mat-icon class="mr-1" [svgIcon]="'heroicons_outline:check'"></mat-icon>
                {{ data.mode === 'assign' ? 'Assign' : 'Update' }} ({{ getSelectedCount() }})
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
export class RoleAssignDialogComponent implements OnInit {

    rolesForm: FormGroup;
    searchText = '';
    filteredRoles: UserRole[] = [];

    /** Map of roleId -> user count */
    private roleUserCounts = new Map<string, number>();

    /** Roles already assigned to the user (cannot change) */
    private assignedRoleIds = new Set<string>();

    /** Roles newly selected in this dialog */
    private selectedRoleIds = new Set<string>();

    constructor(
        private _dialogRef: MatDialogRef<RoleAssignDialogComponent>,
        private _formBuilder: FormBuilder,
        @Inject(MAT_DIALOG_DATA) public data: RoleAssignDialogData
    ) {
        this.rolesForm = this._formBuilder.group({
            roles: this._formBuilder.array([])
        });
    }

    ngOnInit(): void {
        // Mark roles already assigned - user.roles has 'roleId' property
        if (this.data.user.roles && Array.isArray(this.data.user.roles)) {
            this.data.user.roles.forEach(role => {
                this.assignedRoleIds.add(role.roleId);
            });
        }

        // Calculate user counts per role if allUsers is provided
        if (this.data.allUsers && Array.isArray(this.data.allUsers)) {
            this.calculateRoleUserCounts();
        }

        this.filteredRoles = [...this.data.availableRoles];

        // Initialize form array
        const rolesArray = this.rolesForm.get('roles') as FormArray;
        this.data.availableRoles.forEach(() => rolesArray.push(new FormControl(false)));
    }

    /** Calculate how many users have each role */
    private calculateRoleUserCounts(): void {
        this.roleUserCounts.clear();
        
        this.data.allUsers.forEach(user => {
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

    /** Get user initials */
    getUserInitials(fullName: string): string {
        if (!fullName) return '??';
        const names = fullName.trim().split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }

    /** Check if user already has this role */
    userHasRole(roleId: string): boolean {
        return this.assignedRoleIds.has(roleId);
    }

    /** Check if role is selected (either already assigned or newly selected) */
    isRoleSelected(roleId: string): boolean {
        return this.assignedRoleIds.has(roleId) || this.selectedRoleIds.has(roleId);
    }

    /** Toggle role selection for newly assigned roles only */
    toggleRole(roleId: string, checked: boolean): void {
        if (this.assignedRoleIds.has(roleId)) return; // Skip already assigned

        if (checked) {
            this.selectedRoleIds.add(roleId);
        } else {
            this.selectedRoleIds.delete(roleId);
        }
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

    /** Number of newly selected roles */
    getSelectedCount(): number {
        return this.selectedRoleIds.size;
    }

    /** Track by function for available roles (uses 'id' property) */
    trackByAvailableRoleId(index: number, role: UserRole): string {
        return role.id;
    }

    /** Track by function for assigned roles (uses 'roleId' property) */
    trackByRoleId(index: number, role: any): string {
        return role.roleId;
    }

    /** Close dialog and return only newly selected role IDs */
    confirmSelection(): void {
        const newRoleIds = Array.from(this.selectedRoleIds);
        this._dialogRef.close(newRoleIds);
    }
}