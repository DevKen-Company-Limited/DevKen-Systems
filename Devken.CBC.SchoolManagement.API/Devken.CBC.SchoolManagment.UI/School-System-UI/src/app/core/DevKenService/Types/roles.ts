// ============================================
// Common / Shared
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ============================================
// Roles & Permissions
// ============================================

export interface RolePermission {
  permissionId: string;
  permissionName: string;
  description?: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  schoolId?: string;
}

export interface UserRole {
  id: string;
  roleName: any;
  roleId: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  userCount?: number;
  permissionCount?: number;
  permissions?: RolePermission[];
}

export interface UserRoleDto {
  roleId: string;
  name: string;
  assignedAt?: string;
}

// ============================================
// Users
// ============================================

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  profileImageUrl?: string;

  isActive: boolean;
  isEmailVerified: boolean;
  requirePasswordChange: boolean;

  createdOn: string;
  updatedOn?: string;

  // Multi-tenant / school
  tenantId?: string;
  schoolId?: string;
  schoolName?: string;

  // Roles — backend may return either shape; component handles both via getRoles()
  roles?: UserRoleDto[];       // object array: [{ roleId, name }]
  roleNames?: string[];        // string array: ['Admin', 'Teacher']
  permissions?: string[];

  // Populated only on creation / password reset responses
  temporaryPassword?: string;
}

export interface UserWithRoles {
  isSuperAdmin: any;
  userId: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: UserRole[];
  permissions: string[];
  requirePasswordChange: boolean;
}

// ============================================
// Paginated Users Response
// ============================================

export interface PaginatedUsersResponse {
  users: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================
// Password Reset Response
// ============================================

export interface PasswordResetResponse {
  user: UserDto;
  temporaryPassword: string;
  message: string;
  resetAt: string;
  resetBy: string;
}

// ============================================
// User Requests
// ============================================


export interface CreateUserRequest {
  firstName:         string;
  lastName:          string;
  email:             string;
  phoneNumber?:      string;
  roleIds:           string[];
  sendWelcomeEmail?: boolean;
  /**
   * Required when a SuperAdmin creates a user.
   * The backend uses this as the new user's TenantId.
   * Omit for regular school-admin requests — the backend
   * uses the caller's own TenantId instead.
   */
  schoolId?:         string;
}

export interface UpdateUserRequest {
  firstName:     string;
  lastName:      string;
  phoneNumber?:  string;
  roleIds:       string[];
  isActive:      boolean;
  // schoolId intentionally excluded — school cannot be changed after creation
}

// ============================================
// Role Assignment Requests
// ============================================

export interface AssignRoleRequest {
  userId: string;
  roleId: string;
}

export interface AssignRolesRequest {
  roleIds: string[];
}

export interface AssignMultipleRolesRequest {
  userId: string;
  roleIds: string[];
}

export interface RemoveRoleRequest {
  userId: string;
  roleId: string;
}

export interface UpdateUserRolesRequest {
  userId: string;
  roleIds: string[];
}

// ============================================
// Search
// ============================================

export interface UserSearchRequest {
  searchTerm: string;
}

export interface UserSearchResult {
  userId: string;
  email: string;
  userName: string;
  fullName: string;
}