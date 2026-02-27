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
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/parents`;
  private http    = inject(HttpClient);

  /** GET /api/academic/parents (with optional schoolId) */
  getAll(schoolId?: string): Observable<ApiResponse<ParentSummaryDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<ParentSummaryDto[]>>(this.baseUrl, { params });
  }

  /** GET /api/academic/parents with query filters and optional schoolId */
  query(filters: ParentQueryDto, schoolId?: string): Observable<ApiResponse<ParentSummaryDto[]>> {
    let httpParams = new HttpParams();

    if (filters.searchTerm)                  httpParams = httpParams.set('searchTerm',          filters.searchTerm);
    if (filters.relationship != null)        httpParams = httpParams.set('relationship',         String(filters.relationship));
    if (filters.isPrimaryContact  != null)   httpParams = httpParams.set('isPrimaryContact',     String(filters.isPrimaryContact));
    if (filters.isEmergencyContact != null)  httpParams = httpParams.set('isEmergencyContact',   String(filters.isEmergencyContact));
    if (filters.hasPortalAccess   != null)   httpParams = httpParams.set('hasPortalAccess',      String(filters.hasPortalAccess));
    if (filters.isActive          != null)   httpParams = httpParams.set('isActive',             String(filters.isActive));

    // ✅ Always pass schoolId for SuperAdmin so backend doesn't reject with 400
    if (schoolId) {
      httpParams = httpParams.set('schoolId', schoolId);
    }

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