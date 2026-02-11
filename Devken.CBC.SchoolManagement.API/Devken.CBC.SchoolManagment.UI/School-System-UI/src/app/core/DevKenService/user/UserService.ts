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

@Injectable({
  providedIn: 'root'
})
export class UserService implements 
  ICrudService<CreateUserRequest, UpdateUserRequest, UserDto>, 
  IListService<UserDto> {
  
  private _http = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url = `${this._apiBase}/api/user-management`;

  /**
   * Get all users with pagination support
   * SuperAdmin can filter by schoolId
   */
  getAll(page = 1, pageSize = 20, schoolId?: string): Observable<ApiResponse<UserDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    
    return this._http.get<ApiResponse<PaginatedUsersResponse>>(`${this._url}`, { params })
      .pipe(
        map(response => ({
          ...response,
          data: response.data.users
        }))
      );
  }

  getById(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.get<ApiResponse<UserDto>>(`${this._url}/${id}`);
  }

  create(payload: CreateUserRequest): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(`${this._url}`, payload);
  }

  update(id: string, payload: UpdateUserRequest): Observable<ApiResponse<UserDto>> {
    return this._http.put<ApiResponse<UserDto>>(`${this._url}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`);
  }

  /**
   * Toggle user active status (activate/deactivate)
   * Uses the appropriate endpoint based on current status
   */
  toggleActiveStatus(id: string): Observable<ApiResponse<UserDto>> {
    return this.getById(id).pipe(
      map(userResponse => {
        if (!userResponse.success || !userResponse.data) {
          throw new Error('User not found');
        }
        return userResponse.data;
      }),
      switchMap(user => {
        const endpoint = user.isActive
          ? `${this._url}/${id}/deactivate`
          : `${this._url}/${id}/activate`;

        return this._http.post<ApiResponse<UserDto>>(endpoint, {});
      })
    );
  }

  /**
   * Activate a user account
   */
  activateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(`${this._url}/${id}/activate`, {});
  }

  /**
   * Deactivate a user account
   */
  deactivateUser(id: string): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(`${this._url}/${id}/deactivate`, {});
  }

  /**
   * Resend welcome email to user
   */
  resendWelcomeEmail(id: string): Observable<ApiResponse<null>> {
    return this._http.post<ApiResponse<null>>(`${this._url}/${id}/resend-welcome`, {});
  }

  /**
   * Reset user password (admin-initiated)
   */
  resetPassword(id: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._url}/${id}/reset-password`, {});
  }

  /**
   * Assign roles to a user
   */
  assignRoles(userId: string, roleIds: string[]): Observable<ApiResponse<UserDto>> {
    return this._http.post<ApiResponse<UserDto>>(
      `${this._url}/${userId}/roles`, 
      { roleIds }
    );
  }

  /**
   * Remove a role from a user
   */
  removeRole(userId: string, roleId: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(
      `${this._url}/${userId}/roles/${roleId}`
    );
  }

  /**
   * Get available roles for the current tenant
   */
  getAvailableRoles(): Observable<ApiResponse<RoleDto[]>> {
    return this._http.get<ApiResponse<RoleDto[]>>(`${this._apiBase}/api/roles`);
  }
}