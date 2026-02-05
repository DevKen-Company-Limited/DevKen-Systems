import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';

import {
    UserRole,
    UserWithRoles,
    AssignRoleRequest,
    AssignMultipleRolesRequest,
    RemoveRoleRequest,
    UpdateUserRolesRequest,
    ApiResponse,
    PaginatedResponse,
    UserSearchRequest,
    UserSearchResult
} from '../Types/roles';

@Injectable({
    providedIn: 'root'
})
export class RoleAssignmentService {

    private _httpClient = inject(HttpClient);
    private _apiBaseUrl = inject(API_BASE_URL);
    private _apiUrl = `${this._apiBaseUrl}/api/role-assignments`;

    // ------------------------------------------------------
    // State Management
    // ------------------------------------------------------

    private _availableRoles = new BehaviorSubject<UserRole[]>([]);
    private _selectedUser = new BehaviorSubject<UserWithRoles | null>(null);
    private _searchResults = new BehaviorSubject<UserSearchResult[]>([]);

    public availableRoles$ = this._availableRoles.asObservable();
    public selectedUser$ = this._selectedUser.asObservable();
    public searchResults$ = this._searchResults.asObservable();

    // ------------------------------------------------------
    // User Search & Retrieval
    // ------------------------------------------------------

    /**
     * Get all users with their roles (paginated)
     */
    getAllUsersWithRoles(
        pageNumber = 1,
        pageSize = 20
    ): Observable<ApiResponse<PaginatedResponse<UserWithRoles>>> {
        const params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        return this._httpClient.get<ApiResponse<PaginatedResponse<UserWithRoles>>>(
            `${this._apiUrl}/all-users`,
            { params }
        );
    }

    /**
     * Search for users by email, name, or username
     */
    searchUsers(searchTerm: string): Observable<ApiResponse<UserSearchResult[]>> {
        const params = new HttpParams().set('searchTerm', searchTerm);
        
        return this._httpClient
            .get<ApiResponse<UserSearchResult[]>>(
                `${this._apiUrl}/search-users`,
                { params }
            )
            .pipe(
                tap(response => {
                    if (response.success) {
                        this._searchResults.next(response.data);
                    }
                })
            );
    }

    /**
     * Clear search results
     */
    clearSearchResults(): void {
        this._searchResults.next([]);
    }

    // ------------------------------------------------------
    // Role Assignment
    // ------------------------------------------------------

    /**
     * Assign a single role to a user
     */
    assignRole(
        request: AssignRoleRequest
    ): Observable<ApiResponse<UserWithRoles>> {
        return this._httpClient
            .post<ApiResponse<UserWithRoles>>(
                `${this._apiUrl}/assign-role`,
                request
            )
            .pipe(
                tap(response => {
                    if (
                        response.success &&
                        this._selectedUser.value?.userId === request.userId
                    ) {
                        this._selectedUser.next(response.data);
                    }
                })
            );
    }

    /**
     * Assign multiple roles to a user
     */
    assignMultipleRoles(
        request: AssignMultipleRolesRequest
    ): Observable<ApiResponse<UserWithRoles>> {
        return this._httpClient
            .post<ApiResponse<UserWithRoles>>(
                `${this._apiUrl}/assign-multiple-roles`,
                request
            )
            .pipe(
                tap(response => {
                    if (
                        response.success &&
                        this._selectedUser.value?.userId === request.userId
                    ) {
                        this._selectedUser.next(response.data);
                    }
                })
            );
    }

    /**
     * Remove a role from a user
     */
    removeRole(
        request: RemoveRoleRequest
    ): Observable<ApiResponse<UserWithRoles>> {
        return this._httpClient
            .post<ApiResponse<UserWithRoles>>(
                `${this._apiUrl}/remove-role`,
                request
            )
            .pipe(
                tap(response => {
                    if (
                        response.success &&
                        this._selectedUser.value?.userId === request.userId
                    ) {
                        this._selectedUser.next(response.data);
                    }
                })
            );
    }

    /**
     * Replace all roles for a user
     */
    updateUserRoles(
        request: UpdateUserRolesRequest
    ): Observable<ApiResponse<UserWithRoles>> {
        return this._httpClient
            .put<ApiResponse<UserWithRoles>>(
                `${this._apiUrl}/update-user-roles`,
                request
            )
            .pipe(
                tap(response => {
                    if (
                        response.success &&
                        this._selectedUser.value?.userId === request.userId
                    ) {
                        this._selectedUser.next(response.data);
                    }
                })
            );
    }

    // ------------------------------------------------------
    // Queries
    // ------------------------------------------------------

    /**
     * Get user with assigned roles & permissions
     */
    getUserWithRoles(
        userId: string
    ): Observable<ApiResponse<UserWithRoles>> {
        return this._httpClient
            .get<ApiResponse<UserWithRoles>>(
                `${this._apiUrl}/user/${userId}/roles`
            )
            .pipe(
                tap(response => {
                    if (response.success) {
                        this._selectedUser.next(response.data);
                    }
                })
            );
    }

    /**
     * Get users assigned to a role (paginated)
     */
    getUsersByRole(
        roleId: string,
        pageNumber = 1,
        pageSize = 20
    ): Observable<ApiResponse<PaginatedResponse<UserWithRoles>>> {

        const params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        return this._httpClient.get<ApiResponse<PaginatedResponse<UserWithRoles>>>(
            `${this._apiUrl}/role/${roleId}/users`,
            { params }
        );
    }

    /**
     * Get all available roles for current tenant
     */
    getAvailableRoles(): Observable<ApiResponse<UserRole[]>> {
        return this._httpClient
            .get<ApiResponse<UserRole[]>>(
                `${this._apiUrl}/available-roles`
            )
            .pipe(
                tap(response => {
                    if (response.success) {
                        this._availableRoles.next(response.data);
                    }
                })
            );
    }

    /**
     * Check if a user has a role
     */
    userHasRole(
        userId: string,
        roleId: string
    ): Observable<ApiResponse<{ userId: string; roleId: string; hasRole: boolean }>> {
        return this._httpClient.get<ApiResponse<{ userId: string; roleId: string; hasRole: boolean }>>(
            `${this._apiUrl}/user/${userId}/has-role/${roleId}`
        );
    }

    /**
     * Remove all roles from a user
     */
    removeAllRoles(
        userId: string
    ): Observable<ApiResponse<{ userId: string }>> {
        return this._httpClient
            .delete<ApiResponse<{ userId: string }>>(
                `${this._apiUrl}/user/${userId}/roles`
            )
            .pipe(
                tap(response => {
                    if (
                        response.success &&
                        this._selectedUser.value?.userId === userId
                    ) {
                        this._selectedUser.next({
                            ...this._selectedUser.value,
                            roles: [],
                            permissions: []
                        });
                    }
                })
            );
    }

    // ------------------------------------------------------
    // Utilities
    // ------------------------------------------------------

    /**
     * Clear selected user from state
     */
    clearSelectedUser(): void {
        this._selectedUser.next(null);
    }
}