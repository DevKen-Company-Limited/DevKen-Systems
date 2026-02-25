// ═══════════════════════════════════════════════════════════════════
// AssessmentService.ts
// Uses: API_BASE_URL injection (NO environment)
// Matches: AssessmentsController endpoints exactly
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { AssessmentType, AssessmentListItem, AssessmentResponse, CreateAssessmentRequest, UpdateAssessmentRequest, AssessmentScoreResponse, UpsertScoreRequest, AssessmentSchemaResponse } from 'app/assessment/types/assessments';


interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class AssessmentService {

  // ✅ Base API URL via InjectionToken
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _http = inject(HttpClient);

  // ✅ Main controller base
  private readonly _base = `${this._apiBase}/api/assessments`;

  // ────────────────────────────────────────────────────────────────
  // GET ALL
  // GET /api/assessments?type=&classId=&termId=&subjectId=&teacherId=&isPublished=
  // ────────────────────────────────────────────────────────────────
  getAll(
    type?: AssessmentType,
    classId?: string,
    termId?: string,
    subjectId?: string,
    teacherId?: string,
    isPublished?: boolean,
  ): Observable<AssessmentListItem[]> {

    let params = new HttpParams();

    if (type != null)         params = params.set('type', String(type));
    if (classId)              params = params.set('classId', classId);
    if (termId)               params = params.set('termId', termId);
    if (subjectId)            params = params.set('subjectId', subjectId);
    if (teacherId)            params = params.set('teacherId', teacherId);
    if (isPublished != null)  params = params.set('isPublished', String(isPublished));

    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(this._base, { params })
      .pipe(map(r => r.data ?? []));
  }

  // ────────────────────────────────────────────────────────────────
  // GET BY ID
  // GET /api/assessments/{id}?type=Summative
  // ────────────────────────────────────────────────────────────────
  getById(id: string, type: AssessmentType): Observable<AssessmentResponse> {
    const params = new HttpParams().set('type', String(type));

    return this._http
      .get<ApiResponse<AssessmentResponse>>(`${this._base}/${id}`, { params })
      .pipe(map(r => r.data));
  }

  // ────────────────────────────────────────────────────────────────
  // CREATE
  // POST /api/assessments
  // ────────────────────────────────────────────────────────────────
  create(request: CreateAssessmentRequest): Observable<AssessmentResponse> {
    return this._http
      .post<ApiResponse<AssessmentResponse>>(this._base, request)
      .pipe(map(r => r.data));
  }

  // ────────────────────────────────────────────────────────────────
  // UPDATE
  // PUT /api/assessments/{id}
  // ────────────────────────────────────────────────────────────────
  update(id: string, request: UpdateAssessmentRequest): Observable<AssessmentResponse> {
    return this._http
      .put<ApiResponse<AssessmentResponse>>(`${this._base}/${id}`, request)
      .pipe(map(r => r.data));
  }

  // ────────────────────────────────────────────────────────────────
  // PUBLISH / UNPUBLISH
  // PATCH /api/assessments/{id}/publish
  // ────────────────────────────────────────────────────────────────
  publish(
    id: string,
    type: AssessmentType
  ): Observable<{ success: boolean; message: string }> {

    const assessmentType = AssessmentType[type] as string;

    return this._http.patch<{ success: boolean; message: string }>(
      `${this._base}/${id}/publish`,
      { assessmentType }
    );
  }

  // ────────────────────────────────────────────────────────────────
  // DELETE
  // DELETE /api/assessments/{id}?type=Formative
  // ────────────────────────────────────────────────────────────────
  delete(
    id: string,
    type: AssessmentType
  ): Observable<{ success: boolean; message: string }> {

    const params = new HttpParams().set('type', String(type));

    return this._http.delete<{ success: boolean; message: string }>(
      `${this._base}/${id}`,
      { params }
    );
  }

  // ────────────────────────────────────────────────────────────────
  // GET SCORES
  // GET /api/assessments/{id}/scores?type=Summative
  // ────────────────────────────────────────────────────────────────
  getScores(id: string, type: AssessmentType): Observable<AssessmentScoreResponse[]> {

    const params = new HttpParams().set('type', String(type));

    return this._http
      .get<ApiResponse<AssessmentScoreResponse[]>>(
        `${this._base}/${id}/scores`,
        { params }
      )
      .pipe(map(r => r.data ?? []));
  }

  // ────────────────────────────────────────────────────────────────
  // UPSERT SCORE
  // POST /api/assessments/scores
  // ────────────────────────────────────────────────────────────────
  upsertScore(request: UpsertScoreRequest): Observable<AssessmentScoreResponse> {
    return this._http
      .post<ApiResponse<AssessmentScoreResponse>>(
        `${this._base}/scores`,
        request
      )
      .pipe(map(r => r.data));
  }

  // ────────────────────────────────────────────────────────────────
  // DELETE SCORE
  // DELETE /api/assessments/scores/{scoreId}?type=Formative
  // ────────────────────────────────────────────────────────────────
  deleteScore(
    scoreId: string,
    type: AssessmentType
  ): Observable<{ success: boolean; message: string }> {

    const params = new HttpParams().set('type', String(type));

    return this._http.delete<{ success: boolean; message: string }>(
      `${this._base}/scores/${scoreId}`,
      { params }
    );
  }

  // ────────────────────────────────────────────────────────────────
  // GET SCHEMA
  // GET /api/assessments/schema/{type}
  // ────────────────────────────────────────────────────────────────
  getSchema(type: AssessmentType): Observable<AssessmentSchemaResponse> {
    return this._http
      .get<ApiResponse<AssessmentSchemaResponse>>(
        `${this._base}/schema/${type}`
      )
      .pipe(map(r => r.data));
  }

  // ════════════════════════════════════════════════════════════════
  // LOOKUPS (ALL use injected API base)
  // ════════════════════════════════════════════════════════════════

  getClasses(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/classes`)
      .pipe(map(r => r.data ?? r));
  }

  getTeachers(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/teachers`)
      .pipe(map(r => r.data ?? r));
  }

  getSubjects(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/subjects`)
      .pipe(map(r => r.data ?? r));
  }

  getTerms(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/terms`)
      .pipe(map(r => r.data ?? r));
  }

  getAcademicYears(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/AcademicYear`)
      .pipe(map(r => r.data ?? r));
  }

  getSchools(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/schools`)
      .pipe(map(r => r.data ?? r));
  }

  getStudents(classId?: string): Observable<any[]> {
    const params = classId
      ? new HttpParams().set('classId', classId)
      : undefined;

    return this._http
      .get<any>(`${this._apiBase}/api/academic/students`, { params })
      .pipe(map(r => r.data ?? r));
  }

  getLearningOutcomes(strandId?: string): Observable<any[]> {
    const params = strandId
      ? new HttpParams().set('strandId', strandId)
      : undefined;

    return this._http
      .get<any>(`${this._apiBase}/api/curriculum/learningoutcomes`, { params })
      .pipe(map(r => r.data ?? r));
  }

  getStrands(subjectId?: string): Observable<any[]> {
    const params = subjectId
      ? new HttpParams().set('subjectId', subjectId)
      : undefined;

    return this._http
      .get<any>(`${this._apiBase}/api/curriculum/strands`, { params })
      .pipe(map(r => r.data ?? r));
  }
}