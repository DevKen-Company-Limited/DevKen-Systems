/**
 * Role Permission Types
 */

/**
 * Permission data transfer object
 */
export interface Permission {
    permissionId: string;
    id: string;
    key: string;
    displayName: string;
    groupName?: string;
    description?: string;
     userCount?: number
    isAssigned: boolean;
}
export interface UserWithPermission {
    userId: string;
    email: string;
    fullName: string;
    roleNames: string[];
}
export interface PagedResponse<T = any> {
    statusCode: any;
    success: boolean;
    data?: {
        items: T[];
        totalCount: number;
        pageNumber: number;
        pageSize: number;
        totalPages: number;
    };
    message?: string;
    errors?: string[];
}
export interface RoleUserCounts {
    [roleId: string]: number;
}
/**
 * Role with its assigned permissions
 */
export interface RoleWithPermissions {
    roleId: string;
    roleName: string;
    description?: string;
    isSystemRole: boolean;
    tenantId?: string;
    permissions: Permission[];
    totalPermissions: number;
      userCount?: number;  // Add this field
    userCountDisplay?: string;  // Add this field
}

/**
 * Result of role permission operations
 */
export interface RolePermissionResult {
    success: boolean;
    message: string;
    errors: string[];
    data?: RoleWithPermissions;
}

/**
 * Request to update role permissions
 */
export interface UpdateRolePermissionsRequest {
    roleId: string;
    permissionIds: string[];
    tenantId?: string;
}

/**
 * Request to add permissions to role
 */
export interface AddPermissionsRequest {
    roleId: string;
    permissionIds: string[];
    tenantId?: string;
}

/**
 * Request to remove permission from role
 */
export interface RemovePermissionRequest {
    roleId: string;
    permissionId: string;
    tenantId?: string;
}

/**
 * Request to clone permissions between roles
 */
export interface ClonePermissionsRequest {
    sourceRoleId: string;
    targetRoleId: string;
    tenantId?: string;
}

/**
 * Generic API Response wrapper
 */
export interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
}

/**
 * Paginated response wrapper
 */
export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}