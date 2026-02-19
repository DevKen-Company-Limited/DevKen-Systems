// ═══════════════════════════════════════════════════════════════════
// assessment.service.ts
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { AssessmentDto, CreateAssessmentRequest, UpdateAssessmentRequest } from 'app/assessment/types/AssessmentDtos';


interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class AssessmentService {
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _base    = `${this._apiBase}/api/Assessments`;

  constructor(private _http: HttpClient) {}

  // ── Assessments ──────────────────────────────────────────────────────────

  getAll(): Observable<AssessmentDto[]> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(this._base).pipe(
      map(r => r?.data ?? [])
    );
  }

  getById(id: string): Observable<AssessmentDto> {
    return this._http.get<ApiResponse<AssessmentDto>>(`${this._base}/${id}`).pipe(
      map(r => r?.data ?? r as any)
    );
  }

  getWithGrades(id: string): Observable<any> {
    return this._http.get<ApiResponse<any>>(`${this._base}/${id}/grades`).pipe(
      map(r => r?.data ?? r as any)
    );
  }

  getByClass(classId: string): Observable<AssessmentDto[]> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(`${this._base}/class/${classId}`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getByTeacher(teacherId: string): Observable<AssessmentDto[]> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(`${this._base}/teacher/${teacherId}`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getByTerm(termId: string, academicYearId: string): Observable<AssessmentDto[]> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(
      `${this._base}/term/${termId}/academic-year/${academicYearId}`
    ).pipe(map(r => r?.data ?? []));
  }

  getPublished(classId: string, termId: string): Observable<AssessmentDto[]> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(
      `${this._base}/published/class/${classId}/term/${termId}`
    ).pipe(map(r => r?.data ?? []));
  }

  create(payload: CreateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.post<ApiResponse<AssessmentDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.put<ApiResponse<AssessmentDto>>(`${this._base}/${id}`, payload);
  }

  publish(id: string, isPublished: boolean): Observable<ApiResponse<null>> {
    return this._http.patch<ApiResponse<null>>(`${this._base}/${id}/publish`, { isPublished });
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`);
  }

  // ── Lookups ───────────────────────────────────────────────────────────────

  getClasses(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/class`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getTeachers(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/teachers`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getSubjects(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/subjects`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getTerms(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/terms`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getAcademicYears(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/academicyear`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getSchools(): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/schools`).pipe(
      map(r => r?.data ?? [])
    );
  }
}