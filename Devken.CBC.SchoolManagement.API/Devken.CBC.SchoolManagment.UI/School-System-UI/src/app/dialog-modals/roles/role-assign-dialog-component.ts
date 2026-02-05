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
import { UserRole, UserWithRoles } from 'app/core/DevKenService/Types/roles';

export interface RoleAssignDialogData {
    user: UserWithRoles;
    availableRoles: UserRole[];
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
        MatTooltipModule
    ],
    template: `
        <div class="flex flex-col max-h-screen">
            <!-- Header -->
            <div class="flex items-center justify-between px-6 py-4 border-b">
                <h2 class="text-2xl font-semibold">
                    {{ data.mode === 'assign' ? 'Assign Roles' : 'Update Roles' }}
                </h2>
                <button mat-icon-button [mat-dialog-close]>
                    <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                </button>
            </div>

            <!-- User Info -->
            <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800">
                <div class="flex items-center">
                    <div class="flex items-center justify-center w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-800">
                        <mat-icon class="text-primary-600 dark:text-primary-400" [svgIcon]="'heroicons_outline:user'"></mat-icon>
                    </div>
                    <div class="ml-3">
                        <!-- ✅ Using fullName instead of firstName + lastName -->
                        <p class="font-semibold">{{ data.user.fullName }}</p>
                        <p class="text-sm text-secondary">{{ data.user.email }}</p>
                    </div>
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
                        placeholder="Type to filter roles..."
                        (ngModelChange)="filterRoles()">
                    <mat-icon matPrefix [svgIcon]="'heroicons_outline:magnifying-glass'"></mat-icon>
                </mat-form-field>
            </div>

            <!-- Role List -->
            <div class="flex-auto overflow-y-auto px-6 pb-4">
                <div *ngIf="filteredRoles.length === 0" class="text-center py-8 text-secondary">
                    <mat-icon class="icon-size-12 mb-2" [svgIcon]="'heroicons_outline:shield-exclamation'"></mat-icon>
                    <p>No roles found</p>
                </div>

                <form [formGroup]="rolesForm">
                    <div formArrayName="roles" class="space-y-2">
                        <div
                            *ngFor="let role of filteredRoles; let i = index"
                            class="flex items-start p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                            <mat-checkbox
                                [formControlName]="getRoleFormControlIndex(role.roleId)"
                                [checked]="isRoleSelected(role.roleId)"
                                (change)="toggleRole(role.roleId, $event.checked)"
                                class="mt-1">
                            </mat-checkbox>
                            <div class="ml-3 flex-1">
                                <!-- ✅ Using roleName instead of name -->
                                <h5 class="font-semibold">{{ role.roleName }}</h5>
                                <p *ngIf="role.description" class="text-sm text-secondary mt-1">{{ role.description }}</p>
                                <div class="mt-2 flex items-center gap-3">
                                    <!-- ✅ Using permissionCount -->
                                    <p class="text-xs text-secondary">
                                        <mat-icon class="icon-size-4 align-middle" [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
                                        {{ role.permissionCount }} permission(s)
                                    </p>
                                    <!-- System role badge -->
                                    <span *ngIf="role.isSystemRole" 
                                          class="px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100">
                                        System Role
                                    </span>
                                </div>
                            </div>
                            <mat-icon
                                *ngIf="userHasRole(role.roleId)"
                                class="text-primary ml-2"
                                matTooltip="Currently assigned"
                                [svgIcon]="'heroicons_outline:check-circle'">
                            </mat-icon>
                        </div>
                    </div>
                </form>
            </div>

            <mat-divider></mat-divider>

            <!-- Footer -->
            <div class="flex items-center justify-between px-6 py-4 bg-gray-50 dark:bg-gray-800">
                <div class="text-sm text-secondary">
                    {{ getSelectedCount() }} role(s) selected
                </div>
                <div class="flex gap-2">
                    <button
                        mat-stroked-button
                        [mat-dialog-close]>
                        Cancel
                    </button>
                    <button
                        mat-flat-button
                        [color]="'primary'"
                        [disabled]="getSelectedCount() === 0"
                        (click)="confirmSelection()">
                        <mat-icon [svgIcon]="'heroicons_outline:check'"></mat-icon>
                        <span class="ml-2">{{ data.mode === 'assign' ? 'Assign' : 'Update' }}</span>
                    </button>
                </div>
            </div>
        </div>
    `,
    styles: [`
        :host {
            display: block;
        }

        .icon-size-12 {
            @apply w-12 h-12;
        }

        .icon-size-4 {
            @apply w-4 h-4;
        }

        .overflow-y-auto {
            max-height: 400px;
        }

        mat-checkbox {
            ::ng-deep .mdc-checkbox {
                flex-shrink: 0;
            }
        }
    `]
})
export class RoleAssignDialogComponent implements OnInit {
    rolesForm: FormGroup;
    searchText = '';
    filteredRoles: UserRole[] = [];
    selectedRoleIds: Set<string> = new Set();
    roleFormControlMap: Map<string, number> = new Map();

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
        this.initializeForm();
        this.filteredRoles = [...this.data.availableRoles];
    }

    private initializeForm(): void {
        const rolesArray = this.rolesForm.get('roles') as FormArray;
        
        // ✅ Using roleId instead of id
        this.data.availableRoles.forEach((role, index) => {
            rolesArray.push(new FormControl(false));
            this.roleFormControlMap.set(role.roleId, index);
        });
    }

    getRoleFormControlIndex(roleId: string): number {
        return this.roleFormControlMap.get(roleId) || 0;
    }

    isRoleSelected(roleId: string): boolean {
        return this.selectedRoleIds.has(roleId);
    }

    userHasRole(roleId: string): boolean {
        // ✅ Using roleId instead of id
        return this.data.user.roles.some(r => r.roleId === roleId);
    }

    toggleRole(roleId: string, checked: boolean): void {
        if (checked) {
            this.selectedRoleIds.add(roleId);
        } else {
            this.selectedRoleIds.delete(roleId);
        }
    }

    filterRoles(): void {
        const searchLower = this.searchText.toLowerCase();
        // ✅ Using roleName instead of name
        this.filteredRoles = this.data.availableRoles.filter(role =>
            role.roleName.toLowerCase().includes(searchLower) ||
            (role.description && role.description.toLowerCase().includes(searchLower))
        );
    }

    getSelectedCount(): number {
        return this.selectedRoleIds.size;
    }

    confirmSelection(): void {
        const selectedRoleIds = Array.from(this.selectedRoleIds);
        this._dialogRef.close(selectedRoleIds);
    }
}