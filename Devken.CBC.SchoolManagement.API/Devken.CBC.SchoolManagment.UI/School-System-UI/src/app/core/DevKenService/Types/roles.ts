// ============================================
// Role Assignment Types
// ============================================

/**
 * Base Role information
 */
export interface UserRole {
    userCount: number;
    roleId: string;
    roleName: string;
    description?: string;
    permissionCount: number;
    isSystemRole: boolean;
    permissions?: RolePermission[];
}
export interface UpdateUserRolesRequest {
    userId: string;
    roleIds: string[];
}

/**
 * Permission within a role
 */
export interface RolePermission {
    permissionId: string;
    permissionName: string;
    description?: string;
}

/**
 * User with their assigned roles
 */
export interface UserWithRoles {
    userId: string;
    email: string;
    fullName: string;
    userName: string;
    firstName: string;
    lastName: string;
    roles: UserRole[];
    permissions: string[];
    requirePasswordChange: boolean;
}

/**
 * Assign single role request
 */
export interface AssignRoleRequest {
    userId: string;
    roleId: string;
}

/**
 * Assign multiple roles request
 */
export interface AssignMultipleRolesRequest {
    userId: string;
    roleIds: string[];
}

/**
 * Remove role request
 */
export interface RemoveRoleRequest {
    userId: string;
    roleId: string;
}

/**
 * Update all user roles request
 */
export interface UpdateUserRolesRequest {
    userId: string;
    roleIds: string[];
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
 * Paginated response
 */
export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

/**
 * User search request
 */
export interface UserSearchRequest {
    searchTerm: string; // Email, name, or username
}

/**
 * User search result (simplified)
 */
export interface UserSearchResult {
    userId: string;
    email: string;
    fullName: string;
    userName: string;
}