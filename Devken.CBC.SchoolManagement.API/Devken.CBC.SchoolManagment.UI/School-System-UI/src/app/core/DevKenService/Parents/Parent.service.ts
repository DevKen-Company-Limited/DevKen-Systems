import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  ParentSummaryDto, ParentQueryDto, ParentDto,
  CreateParentDto, UpdateParentDto,
} from 'app/Academics/Parents/Types/Parent.types';

@Injectable({ providedIn: 'root' })
export class ParentService {
  // ✅ Fixed: was /api/parents, controller route is /api/academic/parents
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/parents`;
  private http    = inject(HttpClient);

  /** GET /api/academic/parents */
  getAll(schoolId?: string): Observable<ApiResponse<ParentSummaryDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<ParentSummaryDto[]>>(this.baseUrl);
  }

  /** GET /api/academic/parents?searchTerm=...&relationship=... */
  query(params: ParentQueryDto): Observable<ApiResponse<ParentSummaryDto[]>> {
    let httpParams = new HttpParams();
    if (params.searchTerm)                 httpParams = httpParams.set('searchTerm',          params.searchTerm);
    if (params.relationship != null)       httpParams = httpParams.set('relationship',         String(params.relationship));
    if (params.isPrimaryContact  != null)  httpParams = httpParams.set('isPrimaryContact',     String(params.isPrimaryContact));
    if (params.isEmergencyContact != null) httpParams = httpParams.set('isEmergencyContact',   String(params.isEmergencyContact));
    if (params.hasPortalAccess   != null)  httpParams = httpParams.set('hasPortalAccess',      String(params.hasPortalAccess));
    if (params.isActive          != null)  httpParams = httpParams.set('isActive',             String(params.isActive));
    return this.http.get<ApiResponse<ParentSummaryDto[]>>(this.baseUrl, { params: httpParams });
  }

  /** GET /api/academic/parents/{id} */
  getById(id: string): Observable<ApiResponse<ParentDto>> {
    return this.http.get<ApiResponse<ParentDto>>(`${this.baseUrl}/${id}`);
  }

  /** GET /api/academic/parents/by-student/{studentId} */
  getByStudent(studentId: string): Observable<ApiResponse<ParentDto[]>> {
    return this.http.get<ApiResponse<ParentDto[]>>(`${this.baseUrl}/by-student/${studentId}`);
  }

  /** POST /api/academic/parents */
  create(payload: CreateParentDto): Observable<ApiResponse<ParentDto>> {
    return this.http.post<ApiResponse<ParentDto>>(this.baseUrl, payload);
  }

  /** PUT /api/academic/parents/{id} */
  update(id: string, payload: UpdateParentDto): Observable<ApiResponse<ParentDto>> {
    return this.http.put<ApiResponse<ParentDto>>(`${this.baseUrl}/${id}`, payload);
  }

  /** PATCH /api/academic/parents/{id}/activate */
  activate(id: string): Observable<ApiResponse<ParentDto>> {
    return this.http.patch<ApiResponse<ParentDto>>(`${this.baseUrl}/${id}/activate`, {});
  }

  /** PATCH /api/academic/parents/{id}/deactivate */
  deactivate(id: string): Observable<ApiResponse<ParentDto>> {
    return this.http.patch<ApiResponse<ParentDto>>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  /** DELETE /api/academic/parents/{id} */
  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/${id}`);
  }
}