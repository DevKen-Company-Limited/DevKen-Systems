import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { Permission, RoleWithPermissions, UpdateRolePermissionsRequest, AddPermissionsRequest, ClonePermissionsRequest, UserWithPermission, PagedResponse } from '../Types/role-permissions';
import { UserWithRoles, ApiResponse } from '../Types/roles';


@Injectable({
    providedIn: 'root'
})
export class RolePermissionService {

    private readonly _http = inject(HttpClient);
    private readonly _apiBaseUrl = inject(API_BASE_URL);
    private readonly _apiUrl = `${this._apiBaseUrl}/api/rolepermissions`;
    private readonly _roleAssignmentsApiUrl = `${this._apiBaseUrl}/api/role-assignments`;

    // ------------------------------------------------------
    // State
    // ------------------------------------------------------

    private readonly _allPermissions$ = new BehaviorSubject<Permission[]>([]);
    private readonly _groupedPermissions$ = new BehaviorSubject<Record<string, Permission[]>>({});
    private readonly _selectedRole$ = new BehaviorSubject<RoleWithPermissions | null>(null);
    private readonly _allRoles$ = new BehaviorSubject<RoleWithPermissions[]>([]);
    private readonly _allUsers$ = new BehaviorSubject<UserWithRoles[]>([]);

    public readonly allPermissions$ = this._allPermissions$.asObservable();
    public readonly groupedPermissions$ = this._groupedPermissions$.asObservable();
    public readonly selectedRole$ = this._selectedRole$.asObservable();
    public readonly allRoles$ = this._allRoles$.asObservable();
    public readonly allUsers$ = this._allUsers$.asObservable();

    // ------------------------------------------------------
    // Role Permissions Endpoints
    // ------------------------------------------------------

    /** Get all available permissions */
    getAllPermissions(): Observable<ApiResponse<Permission[]>> {
        return this._http
            .get<ApiResponse<Permission[]>>(`${this._apiUrl}/permissions`)
            .pipe(
                tap(res => {
                    if (res.success) {
                        this._allPermissions$.next(res.data);
                    }
                })
            );
    }

    /** Get permissions grouped by category */
    getAllPermissionsGrouped(): Observable<ApiResponse<Record<string, Permission[]>>> {
        return this._http
            .get<ApiResponse<Record<string, Permission[]>>>(`${this._apiUrl}/permissions/grouped`)
            .pipe(
                tap(res => {
                    if (res.success) {
                        this._groupedPermissions$.next(res.data);
                    }
                })
            );
    }

    /** Get permissions for a specific role */
    getRolePermissions(roleId: string): Observable<ApiResponse<Permission[]>> {
        return this._http.get<ApiResponse<Permission[]>>(
            `${this._apiUrl}/roles/${roleId}/permissions`
        );
    }

    /** Get role with permissions */
    getRoleWithPermissions(roleId: string): Observable<ApiResponse<RoleWithPermissions>> {
        return this._http
            .get<ApiResponse<RoleWithPermissions>>(`${this._apiUrl}/roles/${roleId}`)
            .pipe(
                tap(res => {
                    if (res.success) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Check if role has a permission */
    roleHasPermission(
        roleId: string,
        permissionId: string
    ): Observable<ApiResponse<boolean>> {
        return this._http.get<ApiResponse<boolean>>(
            `${this._apiUrl}/roles/${roleId}/permissions/${permissionId}/check`
        );
    }

    // ------------------------------------------------------
    // Role Permissions Command Endpoints
    // ------------------------------------------------------

    /** Replace all permissions for a role */
    updateRolePermissions(
        request: UpdateRolePermissionsRequest
    ): Observable<ApiResponse<any>> {
        return this._http
            .put<ApiResponse<any>>(`${this._apiUrl}/roles/permissions`, request)
            .pipe(
                tap(res => {
                    if (
                        res.success &&
                        res.data &&
                        this._selectedRole$.value?.roleId === request.roleId
                    ) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Add multiple permissions to a role */
    addPermissionsToRole(
        request: AddPermissionsRequest
    ): Observable<ApiResponse<any>> {
        return this._http
            .post<ApiResponse<any>>(`${this._apiUrl}/roles/permissions/add`, request)
            .pipe(
                tap(res => {
                    if (
                        res.success &&
                        res.data &&
                        this._selectedRole$.value?.roleId === request.roleId
                    ) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Add a single permission to a role */
    addPermissionToRole(
        roleId: string,
        permissionId: string
    ): Observable<ApiResponse<any>> {
        return this._http
            .post<ApiResponse<any>>(
                `${this._apiUrl}/roles/${roleId}/permissions/${permissionId}`,
                {}
            )
            .pipe(
                tap(res => {
                    if (
                        res.success &&
                        res.data &&
                        this._selectedRole$.value?.roleId === roleId
                    ) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Remove a permission from a role */
    removePermissionFromRole(
        roleId: string,
        permissionId: string
    ): Observable<ApiResponse<any>> {
        return this._http
            .delete<ApiResponse<any>>(
                `${this._apiUrl}/roles/${roleId}/permissions/${permissionId}`
            )
            .pipe(
                tap(res => {
                    if (
                        res.success &&
                        res.data &&
                        this._selectedRole$.value?.roleId === roleId
                    ) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Remove all permissions from a role */
    removeAllPermissions(roleId: string): Observable<ApiResponse<any>> {
        return this._http
            .delete<ApiResponse<any>>(`${this._apiUrl}/roles/${roleId}/permissions`)
            .pipe(
                tap(res => {
                    if (
                        res.success &&
                        res.data &&
                        this._selectedRole$.value?.roleId === roleId
                    ) {
                        this._selectedRole$.next(res.data);
                    }
                })
            );
    }

    /** Clone permissions from one role to another */
    cloneRolePermissions(
        request: ClonePermissionsRequest
    ): Observable<ApiResponse<any>> {
        return this._http.post<ApiResponse<any>>(
            `${this._apiUrl}/roles/permissions/clone`,
            request
        );
    }

    /** Get all roles with their permissions */
    getAllRolesWithPermissions(): Observable<ApiResponse<RoleWithPermissions[]>> {
        return this._http
            .get<ApiResponse<RoleWithPermissions[]>>(`${this._apiUrl}/roles`)
            .pipe(
                tap(res => {
                    if (res.success) {
                        this._allRoles$.next(res.data);
                    }
                })
            );
    }

    // ------------------------------------------------------
    // Role Assignments Endpoints (User-Role Management)
    // ------------------------------------------------------

    /** Get all users with their roles */
    getAllUsers(pageNumber: number = 1, pageSize: number = 1000): Observable<PagedResponse<UserWithRoles>> {
        let params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        return this._http
            .get<PagedResponse<UserWithRoles>>(`${this._roleAssignmentsApiUrl}/all-users`, { params })
            .pipe(
                tap(res => {
                    if (res.success && res.data) {
                        this._allUsers$.next(res.data.items || []);
                    }
                })
            );
    }

    /** Search users by name or email */
    searchUsers(searchTerm: string): Observable<ApiResponse<UserWithRoles[]>> {
        let params = new HttpParams().set('searchTerm', searchTerm);
        return this._http.get<ApiResponse<UserWithRoles[]>>(
            `${this._roleAssignmentsApiUrl}/search-users`,
            { params }
        );
    }

    /** Get user with roles */
    getUserWithRoles(userId: string): Observable<ApiResponse<UserWithRoles>> {
        return this._http.get<ApiResponse<UserWithRoles>>(
            `${this._roleAssignmentsApiUrl}/user/${userId}/roles`
        );
    }

    /** 
     * Get users by role - Returns all users in a specific role
     * Handles pagination internally and returns flattened array
     */
    getUsersByRole(roleId: string): Observable<ApiResponse<UserWithPermission[]>> {
        // Request large page size to get all users at once
        let params = new HttpParams()
            .set('pageNumber', '1')
            .set('pageSize', '10000'); // Large page size to get all users

        return this._http.get<PagedResponse<UserWithPermission>>(
            `${this._roleAssignmentsApiUrl}/role/${roleId}/users`,
            { params }
        ).pipe(
            map(pagedResponse => {
                // Transform PagedResponse to ApiResponse with flattened array
                return {
                    success: pagedResponse.success,
                    message: pagedResponse.message,
                    data: pagedResponse.data?.items || [],
                    statusCode: pagedResponse.statusCode
                } as ApiResponse<UserWithPermission[]>;
            })
        );
    }

    /** 
     * Get users by role with pagination support
     * Use this if you need pagination control
     */
    getUsersByRolePaged(roleId: string, pageNumber: number = 1, pageSize: number = 20): Observable<PagedResponse<UserWithPermission>> {
        let params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        return this._http.get<PagedResponse<UserWithPermission>>(
            `${this._roleAssignmentsApiUrl}/role/${roleId}/users`,
            { params }
        );
    }

    /** Get available roles for assignment */
    getAvailableRoles(): Observable<ApiResponse<RoleWithPermissions[]>> {
        return this._http.get<ApiResponse<RoleWithPermissions[]>>(
            `${this._roleAssignmentsApiUrl}/available-roles`
        );
    }

    /** Check if user has a specific role */
    userHasRole(userId: string, roleId: string): Observable<ApiResponse<boolean>> {
        return this._http.get<ApiResponse<boolean>>(
            `${this._roleAssignmentsApiUrl}/user/${userId}/has-role/${roleId}`
        );
    }

    /** Assign role to user */
    assignRoleToUser(userId: string, roleId: string): Observable<ApiResponse<any>> {
        return this._http.post<ApiResponse<any>>(
            `${this._roleAssignmentsApiUrl}/assign-role`,
            { userId, roleId }
        );
    }

    /** Assign multiple roles to user */
    assignMultipleRolesToUser(userId: string, roleIds: string[]): Observable<ApiResponse<any>> {
        return this._http.post<ApiResponse<any>>(
            `${this._roleAssignmentsApiUrl}/assign-multiple-roles`,
            { userId, roleIds }
        );
    }

    /** Remove role from user */
    removeRoleFromUser(userId: string, roleId: string): Observable<ApiResponse<any>> {
        return this._http.post<ApiResponse<any>>(
            `${this._roleAssignmentsApiUrl}/remove-role`,
            { userId, roleId }
        );
    }

    /** Update user roles (replace all roles) */
    updateUserRoles(userId: string, roleIds: string[]): Observable<ApiResponse<any>> {
        return this._http.put<ApiResponse<any>>(
            `${this._roleAssignmentsApiUrl}/update-user-roles`,
            { userId, roleIds }
        );
    }

    /** Remove all roles from user */
    removeAllRolesFromUser(userId: string): Observable<ApiResponse<any>> {
        return this._http.delete<ApiResponse<any>>(
            `${this._roleAssignmentsApiUrl}/user/${userId}/roles`
        );
    }

    // ------------------------------------------------------
    // Permission Users Endpoints
    // ------------------------------------------------------

    /** 
     * Get users who have a specific permission
     * Returns flattened array of users
     */
    getUsersWithPermission(permissionId: string): Observable<ApiResponse<UserWithPermission[]>> {
        return this._http.get<ApiResponse<UserWithPermission[]>>(
            `${this._apiUrl}/permissions/${permissionId}/users`
        );
    }

    /** Get user counts for all roles */
    getUserCountsForAllRoles(): Observable<ApiResponse<Record<string, number>>> {
        return this._http.get<ApiResponse<Record<string, number>>>(
            `${this._apiUrl}/roles/user-counts`
        );
    }

    /** Get permission statistics */
    getPermissionStats(): Observable<ApiResponse<{
        totalPermissions: number;
        permissionsWithUsers: number;
        totalUserAssignments: number;
    }>> {
        return this._http.get<ApiResponse<any>>(
            `${this._apiUrl}/permissions/stats`
        );
    }

    // ------------------------------------------------------
    // Utilities
    // ------------------------------------------------------

    clearSelectedRole(): void {
        this._selectedRole$.next(null);
    }

    setSelectedRole(role: RoleWithPermissions): void {
        this._selectedRole$.next(role);
    }

    /** Helper to convert GUID string to proper format if needed */
    formatGuid(guid: string): string {
        // If it's already a valid GUID, return as is
        if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(guid)) {
            return guid;
        }
        // Otherwise, return as string
        return guid;
    }
}