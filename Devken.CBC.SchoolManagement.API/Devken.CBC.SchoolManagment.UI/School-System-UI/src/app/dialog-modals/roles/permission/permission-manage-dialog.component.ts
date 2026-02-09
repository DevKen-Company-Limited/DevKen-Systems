import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenu } from "@angular/material/menu";
import { RoleWithPermissions, Permission } from 'app/core/DevKenService/Types/role-permissions';

export interface PermissionManageDialogData {
    role: RoleWithPermissions;
    groupedPermissions: Record<string, Permission[]>;
    allRoles: RoleWithPermissions[];
}

@Component({
    selector: 'app-permission-manage-dialog',
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
    MatTabsModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTooltipModule,
    MatDividerModule,
    MatMenu
],
    templateUrl: './permission-manage-dialog.component.html',
    styleUrls: ['./permission-manage-dialog.component.scss']
})
export class PermissionManageDialogComponent implements OnInit {
    role: RoleWithPermissions;
    groupedPermissions: Record<string, Permission[]>;
    allRoles: RoleWithPermissions[];
    
    selectedPermissionIds = new Set<string>();
    originalPermissionIds = new Set<string>();
    permissionGroups: string[] = [];
    searchTerm = '';
    
    // Group expansion state
    expandedGroups = new Set<string>();
    
    // Statistics
    stats = {
        selected: 0,
        total: 0,
        added: 0,
        removed: 0
    };

    // Template roles for quick apply
    templateRoles: RoleWithPermissions[] = [];

    constructor(
        public dialogRef: MatDialogRef<PermissionManageDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: PermissionManageDialogData
    ) {
        this.role = data.role;
        this.groupedPermissions = data.groupedPermissions;
        this.allRoles = data.allRoles || [];
    }

    ngOnInit(): void {
        // Initialize selected permissions from role
        if (this.role.permissions) {
            this.role.permissions.forEach(p => {
                this.selectedPermissionIds.add(p.id);
                this.originalPermissionIds.add(p.id);
            });
        }

        // Get permission groups
        this.permissionGroups = Object.keys(this.groupedPermissions);
        
        // Calculate initial stats
        this.updateStats();
        
        // Auto-expand groups that have selected permissions
        this.permissionGroups.forEach(group => {
            const hasSelected = this.groupedPermissions[group].some(p => 
                this.selectedPermissionIds.has(p.id)
            );
            if (hasSelected) {
                this.expandedGroups.add(group);
            }
        });

        // Get template roles (other roles that can be used as templates)
        this.templateRoles = this.allRoles.filter(r => 
            r.roleId !== this.role.roleId && r.permissions && r.permissions.length > 0
        );
    }

    /**
     * Toggle permission selection
     */
    togglePermission(permission: Permission): void {
        if (this.selectedPermissionIds.has(permission.id)) {
            this.selectedPermissionIds.delete(permission.id);
        } else {
            this.selectedPermissionIds.add(permission.id);
        }
        this.updateStats();
    }

    /**
     * Check if permission is selected
     */
    isPermissionSelected(permission: Permission): boolean {
        return this.selectedPermissionIds.has(permission.id);
    }

    /**
     * Select all permissions in a group
     */
    selectAllInGroup(group: string): void {
        this.groupedPermissions[group].forEach(p => {
            this.selectedPermissionIds.add(p.id);
        });
        this.updateStats();
    }

    /**
     * Deselect all permissions in a group
     */
    deselectAllInGroup(group: string): void {
        this.groupedPermissions[group].forEach(p => {
            this.selectedPermissionIds.delete(p.id);
        });
        this.updateStats();
    }

    /**
     * Check if all permissions in a group are selected
     */
    isGroupFullySelected(group: string): boolean {
        return this.groupedPermissions[group].every(p => 
            this.selectedPermissionIds.has(p.id)
        );
    }

    /**
     * Check if some (but not all) permissions in a group are selected
     */
    isGroupPartiallySelected(group: string): boolean {
        const permissions = this.groupedPermissions[group];
        const selectedCount = permissions.filter(p => 
            this.selectedPermissionIds.has(p.id)
        ).length;
        return selectedCount > 0 && selectedCount < permissions.length;
    }

    /**
     * Get selected count for a group
     */
    getGroupSelectedCount(group: string): number {
        return this.groupedPermissions[group].filter(p => 
            this.selectedPermissionIds.has(p.id)
        ).length;
    }

    /**
     * Get total count for a group
     */
    getGroupTotalCount(group: string): number {
        return this.groupedPermissions[group].length;
    }

    /**
     * Toggle group expansion
     */
    toggleGroupExpansion(group: string): void {
        if (this.expandedGroups.has(group)) {
            this.expandedGroups.delete(group);
        } else {
            this.expandedGroups.add(group);
        }
    }

    /**
     * Check if group is expanded
     */
    isGroupExpanded(group: string): boolean {
        return this.expandedGroups.has(group);
    }

    /**
     * Apply template from another role
     */
    applyTemplate(templateRole: RoleWithPermissions): void {
        if (!confirm(`Apply permissions from "${templateRole.roleName}"? This will replace your current selections.`)) {
            return;
        }

        // Clear current selections
        this.selectedPermissionIds.clear();

        // Add permissions from template
        if (templateRole.permissions) {
            templateRole.permissions.forEach(p => {
                this.selectedPermissionIds.add(p.id);
            });
        }

        this.updateStats();
    }

    /**
     * Select all permissions
     */
    selectAll(): void {
        Object.values(this.groupedPermissions).forEach(permissions => {
            permissions.forEach(p => {
                this.selectedPermissionIds.add(p.id);
            });
        });
        this.updateStats();
    }

    /**
     * Deselect all permissions
     */
    deselectAll(): void {
        this.selectedPermissionIds.clear();
        this.updateStats();
    }

    /**
     * Reset to original selection
     */
    reset(): void {
        this.selectedPermissionIds = new Set(this.originalPermissionIds);
        this.updateStats();
    }

    /**
     * Update statistics
     */
    private updateStats(): void {
        this.stats.selected = this.selectedPermissionIds.size;
        this.stats.total = Object.values(this.groupedPermissions)
            .reduce((sum, permissions) => sum + permissions.length, 0);
        
        // Calculate added and removed
        this.stats.added = Array.from(this.selectedPermissionIds)
            .filter(id => !this.originalPermissionIds.has(id)).length;
        this.stats.removed = Array.from(this.originalPermissionIds)
            .filter(id => !this.selectedPermissionIds.has(id)).length;
    }

    /**
     * Check if there are changes
     */
    hasChanges(): boolean {
        return this.stats.added > 0 || this.stats.removed > 0;
    }

    /**
     * Filter permissions based on search
     */
    filterPermissions(permissions: Permission[]): Permission[] {
        if (!this.searchTerm || !this.searchTerm.trim()) {
            return permissions;
        }

        const searchLower = this.searchTerm.toLowerCase().trim();
        return permissions.filter(p =>
            p.displayName.toLowerCase().includes(searchLower) ||
            p.key.toLowerCase().includes(searchLower) ||
            (p.description && p.description.toLowerCase().includes(searchLower))
        );
    }

    /**
     * Save changes
     */
    save(): void {
        this.dialogRef.close(Array.from(this.selectedPermissionIds));
    }

    /**
     * Cancel changes
     */
    cancel(): void {
        if (this.hasChanges()) {
            if (confirm('You have unsaved changes. Are you sure you want to cancel?')) {
                this.dialogRef.close();
            }
        } else {
            this.dialogRef.close();
        }
    }
}