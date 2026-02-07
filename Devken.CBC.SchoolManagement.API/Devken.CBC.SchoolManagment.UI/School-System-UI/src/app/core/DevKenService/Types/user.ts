// export interface UserDto {
//   id: string;
//   firstName: string;
//   lastName: string;
//   email: string;
//   phoneNumber?: string;
//   isActive: boolean;
//   emailConfirmed: boolean;
//   createdAt: string;
//   roles: UserRoleDto[];
//   profileImageUrl?: string;
//   lastLoginAt?: string;
// }

// export interface UserRoleDto {
//   roleId: string;
//   roleName: string;
//   assignedAt: string;
// }

// export interface CreateUserRequest {
//   firstName: string;
//   lastName: string;
//   email: string;
//   phoneNumber?: string;
//   password: string;
//   roleIds: string[];
//   sendWelcomeEmail?: boolean;
// }

// export interface UpdateUserRequest {
//   firstName: string;
//   lastName: string;
//   email: string;
//   phoneNumber?: string;
//   isActive: boolean;
//   roleIds: string[];
// }

// export interface RoleDto {
//   id: string;
//   name: string;
//   description?: string;
//   isSystemRole: boolean;
// }

// export interface ApiResponse<T> {
//   success: boolean;
//   message: string;
//   data: T;
//   errors?: string[];
// }