import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';
import { ApiResponse } from 'app/Tenant/types/school';
import { CreateUserRequest, UpdateUserRequest, UserDto, RoleDto } from '../Types/roles';

interface PaginatedUsersResponse {
  users: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class UserService implements
  ICrudService<CreateUserRequest, UpdateUserRequest, UserDto>,
  IListService<UserDto> {

  private _http    = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url     = `${this._apiBase}/api/user-management`;

  // ── CRUD ───────────────────────────────────────────────────────────────

  getAll(page = 1, pageSize = 20, schoolId?: string): Observable<ApiResponse<UserDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }

    return this._http
      .get<ApiResponse<PaginatedUsersResponse>>(this._url, { params })
      .pipe(map(response => ({ ...response, data: response.data?.users ?? [] })));
  }

  getById(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.get<ApiResponse<UserDto>>(`${this._url}/${id}`);
  }

  /**
   * Create a user.
   * When called by SuperAdmin the payload MUST include `schoolId`.
   * Regular users leave `schoolId` undefined — the backend substitutes their TenantId.
   */
  create(payload: CreateUserRequest): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(this._url, payload);
  }

  update(id: string, payload: UpdateUserRequest): Observable<ApiResponse<UserDto>> {
    return this._http.put<ApiResponse<UserDto>>(`${this._url}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`);
  }

  // ── Status toggles ─────────────────────────────────────────────────────

  activateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(`${this._url}/${id}/activate`, {});
  }

  deactivateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(`${this._url}/${id}/deactivate`, {});
  }

  toggleActiveStatus(id: string): Observable<ApiResponse<UserDto>> {
    return this.getById(id).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error('User not found');
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

  // ── Password / email ────────────────────────────────────────────────────

  resendWelcomeEmail(id: string): Observable<ApiResponse<null>> {
    return this._http.post<ApiResponse<null>>(`${this._url}/${id}/resend-welcome`, {});
  }

  resetPassword(id: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._url}/${id}/reset-password`, {});
  }

  // ── Role assignment ─────────────────────────────────────────────────────

  assignRoles(userId: string, roleIds: string[]): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(
      `${this._url}/${userId}/roles`,
      { roleIds }
    );
  }

  removeRole(userId: string, roleId: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(
      `${this._url}/${userId}/roles/${roleId}`
    );
  }

  // ── Roles for dropdown ──────────────────────────────────────────────────

  /**
   * Fetch roles for the calling user's own school (used by non-SuperAdmin dialog).
   * Hits: GET /api/user-management/available-roles
   */
  getAvailableRoles(): Observable<ApiResponse<RoleDto[]>> {
    return this._http.get<ApiResponse<RoleDto[]>>(
      `${this._url}/available-roles`
    );
  }

  /**
   * Fetch roles scoped to a specific school.
   * Used by the dialog after a SuperAdmin picks a school from the dropdown,
   * so only roles that belong to that school are offered.
   * Hits: GET /api/user-management/available-roles?schoolId={schoolId}
   */
  getAvailableRolesBySchool(schoolId: string): Observable<ApiResponse<RoleDto[]>> {
    const params = new HttpParams().set('schoolId', schoolId);
    return this._http.get<ApiResponse<RoleDto[]>>(
      `${this._url}/available-roles`,
      { params }
    );
  }
}