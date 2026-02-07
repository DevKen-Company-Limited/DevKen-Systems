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
}

export interface UserRole {
  roleName: any;
  roleId: string;
  name: string; // Changed from roleName to name for consistency
  description?: string;
  isSystemRole: boolean;
  userCount?: number;
  permissionCount?: number;
  permissions?: RolePermission[];
}

export interface UserRoleDto {
  roleId: string;
  name: string; // Changed from roleName to name to match API
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
  isActive: boolean;
  isEmailVerified: boolean; // Changed from emailConfirmed
  createdOn: string; // Changed from createdAt
  updatedOn?: string; // Changed from lastLoginAt
  profileImageUrl?: string;
  roles: UserRoleDto[];
  tenantId?: string; // Added for multi-tenant support
  schoolId?: string; // Added for SuperAdmin to see school assignment
  schoolName?: string; // Added for displaying school name
}

export interface UserWithRoles {
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
// User Requests
// ============================================

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  password: string;
  roleIds: string[];
  sendWelcomeEmail?: boolean;
  schoolId?: string; // Added for SuperAdmin to specify school
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  roleIds: string[];
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
  searchTerm: string; // email, name, or username
}

export interface UserSearchResult {
  userId: string;
  email: string;
  userName: string;
  fullName: string;
}