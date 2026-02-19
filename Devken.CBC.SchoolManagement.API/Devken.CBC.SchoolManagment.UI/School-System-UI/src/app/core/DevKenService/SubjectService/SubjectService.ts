// services/SubjectService.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { SubjectDto, CreateSubjectRequest, UpdateSubjectRequest } from 'app/Subject/Types/subjectdto';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class SubjectService {

  private readonly apiBase = inject(API_BASE_URL);
  private readonly base    = `${this.apiBase}/api/academic/subjects`;

  constructor(private http: HttpClient) {}

  // ── CRUD ─────────────────────────────────────────────────────────────────

  getAll(
    schoolId?:    string | null,
    level?:       number | null,
    subjectType?: number | null,
    isActive?:    boolean | null,
  ): Observable<SubjectDto[]> {
    let params = new HttpParams();
    if (schoolId)    params = params.set('schoolId',    schoolId);
    if (level)       params = params.set('level',       level.toString());
    if (subjectType) params = params.set('subjectType', subjectType.toString());
    if (isActive !== null && isActive !== undefined)
                     params = params.set('isActive',    isActive.toString());

    return this.http.get<ApiResponse<SubjectDto[]>>(this.base, { params }).pipe(
      map(res => (res?.data ? res.data : Array.isArray(res) ? (res as any) : []))
    );
  }

  getById(id: string): Observable<SubjectDto> {
    return this.http.get<ApiResponse<SubjectDto>>(`${this.base}/${id}`).pipe(
      map(res => res?.data ?? (res as any))
    );
  }

  getByCode(code: string): Observable<SubjectDto> {
    return this.http.get<ApiResponse<SubjectDto>>(`${this.base}/by-code/${code}`).pipe(
      map(res => res?.data ?? (res as any))
    );
  }

  create(payload: CreateSubjectRequest): Observable<ApiResponse<SubjectDto>> {
    return this.http.post<ApiResponse<SubjectDto>>(this.base, payload);
  }

  update(id: string, payload: UpdateSubjectRequest): Observable<ApiResponse<SubjectDto>> {
    return this.http.put<ApiResponse<SubjectDto>>(`${this.base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.base}/${id}`);
  }

  toggleActive(id: string, isActive: boolean): Observable<ApiResponse<SubjectDto>> {
    return this.http.patch<ApiResponse<SubjectDto>>(
      `${this.base}/${id}/toggle-active`,
      isActive,                           // backend expects [FromBody] bool
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  // ── Bulk ─────────────────────────────────────────────────────────────────

  bulkUpdateStatus(ids: string[], isActive: boolean): Observable<ApiResponse<void>> {
    return this.http.patch<ApiResponse<void>>(`${this.base}/bulk/status`, { ids, isActive });
  }

  bulkDelete(ids: string[]): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.base}/bulk/delete`, { ids });
  }
}