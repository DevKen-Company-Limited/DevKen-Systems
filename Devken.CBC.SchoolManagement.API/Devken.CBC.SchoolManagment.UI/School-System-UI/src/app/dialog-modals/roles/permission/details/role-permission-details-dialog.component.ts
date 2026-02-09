import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { RoleWithPermissions, Permission } from 'app/core/DevKenService/Types/role-permissions';

export interface RolePermissionDetailsDialogData {
    role: RoleWithPermissions;
    groupedPermissions: Record<string, Permission[]>;
}

@Component({
    selector: 'app-role-permission-details-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatExpansionModule,
        MatDividerModule,
        MatChipsModule
    ],
    templateUrl: './role-permission-details-dialog.component.html',
    styleUrls: ['./role-permission-details-dialog.component.scss']
})
export class RolePermissionDetailsDialogComponent {
    role: RoleWithPermissions;
    groupedPermissions: Record<string, Permission[]>;
    rolePermissionsGrouped: Record<string, Permission[]> = {};
    permissionGroups: string[] = [];

    constructor(
        public dialogRef: MatDialogRef<RolePermissionDetailsDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RolePermissionDetailsDialogData
    ) {
        this.role = data.role;
        this.groupedPermissions = data.groupedPermissions;
        
        // Group role's permissions by category
        this.groupRolePermissions();
        this.permissionGroups = Object.keys(this.rolePermissionsGrouped);
    }

    /**
     * Group the role's permissions by category
     */
    private groupRolePermissions(): void {
        if (!this.role.permissions) {
            return;
        }

        this.role.permissions.forEach(permission => {
            const group = permission.groupName || 'Other';
            if (!this.rolePermissionsGrouped[group]) {
                this.rolePermissionsGrouped[group] = [];
            }
            this.rolePermissionsGrouped[group].push(permission);
        });
    }

    /**
     * Get permission count for a group
     */
    getGroupCount(group: string): number {
        return this.rolePermissionsGrouped[group]?.length || 0;
    }

    /**
     * Get group badge color
     */
    getGroupBadgeColor(groupName: string): string {
        const colors = [
            'bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200',
            'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200',
            'bg-purple-100 dark:bg-purple-900 text-purple-800 dark:text-purple-200',
            'bg-orange-100 dark:bg-orange-900 text-orange-800 dark:text-orange-200',
            'bg-pink-100 dark:bg-pink-900 text-pink-800 dark:text-pink-200',
            'bg-indigo-100 dark:bg-indigo-900 text-indigo-800 dark:text-indigo-200',
        ];
        
        const hash = groupName.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
        return colors[hash % colors.length];
    }

    /**
     * Close dialog
     */
    close(): void {
        this.dialogRef.close();
    }
}