// services/GradeService.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { GradeDto, CreateGradeRequest, UpdateGradeRequest } from 'app/grades/types/gradedto';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data:    T;
}

@Injectable({ providedIn: 'root' })
export class GradeService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base    = `${this.apiBase}/api/academic/grades`;

  constructor(private http: HttpClient) {}

  // ── CRUD ─────────────────────────────────────────────────────────────────

  getAll(
    schoolId?:   string | null,
    studentId?:  string | null,
    subjectId?:  string | null,
    termId?:     string | null,
  ): Observable<GradeDto[]> {
    let params = new HttpParams();
    if (schoolId)  params = params.set('schoolId',  schoolId);
    if (studentId) params = params.set('studentId', studentId);
    if (subjectId) params = params.set('subjectId', subjectId);
    if (termId)    params = params.set('termId',    termId);

    return this.http.get<ApiResponse<GradeDto[]>>(this.base, { params }).pipe(
      map(res => (res?.data ? res.data : Array.isArray(res) ? (res as any) : []))
    );
  }

  getById(id: string): Observable<GradeDto> {
    return this.http.get<ApiResponse<GradeDto>>(`${this.base}/${id}`).pipe(
      map(res => res?.data ?? (res as any))
    );
  }

  create(payload: CreateGradeRequest): Observable<ApiResponse<GradeDto>> {
    return this.http.post<ApiResponse<GradeDto>>(this.base, payload);
  }

  update(id: string, payload: UpdateGradeRequest): Observable<ApiResponse<GradeDto>> {
    return this.http.put<ApiResponse<GradeDto>>(`${this.base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.base}/${id}`);
  }

  finalize(id: string): Observable<ApiResponse<GradeDto>> {
    return this.http.patch<ApiResponse<GradeDto>>(`${this.base}/${id}/finalize`, {});
  }
}