import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';

import { ApiResponse } from 'app/Tenant/types/school';
import {
  CreateUserRequest,
  UpdateUserRequest,
  UserDto,
  RoleDto,
  PasswordResetResponse
} from '../Types/roles';

// ✅ Correct paginated response shape
interface PaginatedUsersResponse {
  users: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class UserService
  implements
    ICrudService<CreateUserRequest, UpdateUserRequest, UserDto>,
    IListService<UserDto>
{
  private _http = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url     = `${this._apiBase}/api/user-management`;

  // ─────────────────────────────────────────────────────────────
  // USERS LIST (Returns ONLY UserDto[] for component simplicity)
  // ─────────────────────────────────────────────────────────────

  getAll(
    page = 1,
    pageSize = 20,
    schoolId?: string
  ): Observable<ApiResponse<UserDto[]>> {

  // ─────────────────────────────────────────────────────────────
  // USERS LIST (Returns ONLY UserDto[] for component simplicity)
  // ─────────────────────────────────────────────────────────────

  getAll(
    page = 1,
    pageSize = 20,
    schoolId?: string
  ): Observable<ApiResponse<UserDto[]>> {

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }

    return this._http
      .get<ApiResponse<PaginatedUsersResponse>>(this._url, { params })
      .pipe(
        map(response => ({
          ...response,
          data: response.data?.users ?? []
        }))
      );
  }

  // ─────────────────────────────────────────────────────────────
  // CRUD
  // ─────────────────────────────────────────────────────────────

  getById(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.get<ApiResponse<UserDto>>(`${this._url}/${id}`);
  }

  /** Create a user. When called by SuperAdmin, payload must include schoolId. */
  create(payload: CreateUserRequest): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(this._url, payload);
  }

  update(
    id: string,
    payload: UpdateUserRequest
  ): Observable<ApiResponse<UserDto>> {
    return this._http.put<ApiResponse<UserDto>>(
      `${this._url}/${id}`,
      payload
    );
  }

  // ✅ FIXED — matches your component call
  deleteUser(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(
      `${this._url}/${id}`
    );
  }

  // Keep generic delete for interface compatibility
  delete(id: string): Observable<ApiResponse<null>> {
    return this.deleteUser(id);
  }

  // ─────────────────────────────────────────────────────────────
  // STATUS MANAGEMENT
  // ─────────────────────────────────────────────────────────────

  activateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(
      `${this._url}/${id}/activate`,
      {}
    );
  }

  deactivateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(
      `${this._url}/${id}/deactivate`,
      {}
    );
  }

  toggleActiveStatus(id: string): Observable<ApiResponse<UserDto>> {
    return this.getById(id).pipe(
      map(res => {
        if (!res.success || !res.data) {
          throw new Error('User not found');
        }
        return res.data;
      }),
      switchMap(user => {
        const endpoint = user.isActive
          ? `${this._url}/${id}/deactivate`
          : `${this._url}/${id}/activate`;
        return this._http.post<ApiResponse<UserDto>>(endpoint, {});
      })
    );
  }

  // ─────────────────────────────────────────────────────────────
  // PASSWORD & EMAIL
  // ─────────────────────────────────────────────────────────────

  resendWelcomeEmail(id: string): Observable<ApiResponse<null>> {
    return this._http.post<ApiResponse<null>>(
      `${this._url}/${id}/resend-welcome`,
      {}
    );
  }

  resetPassword(id: string): Observable<ApiResponse<PasswordResetResponse>> {
    return this._http.post<ApiResponse<PasswordResetResponse>>(
      `${this._url}/${id}/reset-password`,
      {}
    );
  }

  // ─────────────────────────────────────────────────────────────
  // ROLE MANAGEMENT
  // ─────────────────────────────────────────────────────────────

  assignRoles(
    userId: string,
    roleIds: string[]
  ): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(
      `${this._url}/${userId}/roles`,
      { roleIds }
    );
  }

  removeRole(
    userId: string,
    roleId: string
  ): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(
      `${this._url}/${userId}/roles/${roleId}`
    );
  }

  // ─────────────────────────────────────────────────────────────
  // ROLES (Dropdown Support)
  // ─────────────────────────────────────────────────────────────

  getAvailableRoles(): Observable<ApiResponse<RoleDto[]>> {
    return this._http.get<ApiResponse<RoleDto[]>>(
      `${this._url}/available-roles`
    );
  }

  getAvailableRolesBySchool(
    schoolId: string
  ): Observable<ApiResponse<RoleDto[]>> {
    const params = new HttpParams().set('schoolId', schoolId);

    return this._http.get<ApiResponse<RoleDto[]>>(
      `${this._url}/available-roles`,
      { params }
    );
  }

  /**
   * Roles scoped to a specific school.
   * Called by the dialog when a SuperAdmin selects a school so only
   * roles that belong to that school are offered.
   */
  getAvailableRolesBySchool(schoolId: string): Observable<ApiResponse<RoleDto[]>> {
    const params = new HttpParams().set('schoolId', schoolId);
    return this._http.get<ApiResponse<RoleDto[]>>(`${this._apiBase}/api/roles`, { params });
  }
}