import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { ApiResponse } from 'app/Tenant/types/school';
import { RoleDto } from '../Types/roles';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  
  private _http = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url = `${this._apiBase}/api/roles`;

  /**
   * Get all available roles for the current user's context
   * SuperAdmin gets all roles
   * SchoolAdmin gets roles for their school
   */
  getAvailableRoles(): Observable<ApiResponse<RoleDto[]>> {
    return this._http.get<ApiResponse<RoleDto[]>>(`${this._url}`);
  }

  /**
   * Get role by ID
   */
  getById(id: string): Observable<ApiResponse<RoleDto>> {
    return this._http.get<ApiResponse<RoleDto>>(`${this._url}/${id}`);
  }

  /**
   * Get roles with users count
   */
  getRolesWithUserCount(): Observable<ApiResponse<RoleDto[]>> {
    return this._http.get<ApiResponse<RoleDto[]>>(`${this._url}/with-user-count`);
  }
}