<<<<<<< HEAD
// ═══════════════════════════════════════════════════════════════════
// assessment.service.ts
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
=======
import { Injectable, inject } from '@angular/core';
>>>>>>> upstream/main
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
<<<<<<< HEAD
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
=======
import {
  AssessmentType,
  AssessmentResponse,
  AssessmentListItem,
  CreateAssessmentRequest,
  UpdateAssessmentRequest,
  TeacherLookup,
  SubjectLookup,
  ClassLookup,
  TermLookup,
  AcademicYearLookup,
  LearningOutcomeLookup,
} from 'app/assessment/types/AssessmentDtos';

/** Thin API-response wrapper the backend returns */
interface ApiResponse<T> { success: boolean; message: string; data: T; }

/** Minimal school shape needed for the SuperAdmin picker */
export interface SchoolLookup { id: string; name: string; slug?: string; }

@Injectable({ providedIn: 'root' })
export class AssessmentService {

  private readonly _http    = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);

  // Base URL — matches [Route("api/assessments")] on the controller
  private readonly _url = `${this._apiBase}/api/assessments`;

  // ── Private helpers ───────────────────────────────────────────────────────

  /** Unwrap the ApiResponse<T> envelope and return just data */
  private _data<T>(obs: Observable<ApiResponse<T>>): Observable<T> {
    return obs.pipe(map(r => r.data));
  }

  /** Build HttpParams with an optional schoolId query param */
  private _schoolParams(schoolId?: string): HttpParams {
    let p = new HttpParams();
    if (schoolId) p = p.set('schoolId', schoolId);
    return p;
  }

  // ── Assessments ───────────────────────────────────────────────────────────

  /**
   * GET /api/assessments?type={type}
   * Controller: GetAll([FromQuery] AssessmentTypeDto? type, ...)
   * Called per-type by the list component (forkJoin × 3).
   */
  getAll(type: AssessmentType): Observable<AssessmentListItem[]> {
    const params = new HttpParams().set('type', type.toString());
    return this._data(
      this._http.get<ApiResponse<AssessmentListItem[]>>(this._url, { params })
    );
  }

  /**
   * GET /api/assessments?type={type}&classId=...&termId=...  (all optional)
   * Controller: GetAll([FromQuery] AssessmentTypeDto? type, ...)
   * Use when you need fine-grained filtering from the list page.
   */
  getAllFiltered(params?: {
    type?:        AssessmentType;
    classId?:     string;
    termId?:      string;
    subjectId?:   string;
    teacherId?:   string;
    isPublished?: boolean;
  }): Observable<AssessmentListItem[]> {
    let httpParams = new HttpParams();
    if (params?.type        != null) httpParams = httpParams.set('type',        params.type.toString());
    if (params?.classId)             httpParams = httpParams.set('classId',     params.classId);
    if (params?.termId)              httpParams = httpParams.set('termId',      params.termId);
    if (params?.subjectId)           httpParams = httpParams.set('subjectId',   params.subjectId);
    if (params?.teacherId)           httpParams = httpParams.set('teacherId',   params.teacherId);
    if (params?.isPublished != null) httpParams = httpParams.set('isPublished', String(params.isPublished));

    return this._data(
      this._http.get<ApiResponse<AssessmentListItem[]>>(this._url, { params: httpParams })
    );
  }

  /**
   * GET /api/assessments/{id}?type={type}
   * Controller: GetById(Guid id, [FromQuery] AssessmentTypeDto type)
   */
  getById(id: string, type: AssessmentType): Observable<AssessmentResponse> {
    const params = new HttpParams().set('type', type.toString());
    return this._data(
      this._http.get<ApiResponse<AssessmentResponse>>(`${this._url}/${id}`, { params })
    );
  }

  /**
   * POST /api/assessments
   * Controller: Create([FromBody] CreateAssessmentRequest request)
   */
  create(payload: CreateAssessmentRequest): Observable<AssessmentResponse> {
    return this._data(
      this._http.post<ApiResponse<AssessmentResponse>>(this._url, payload)
    );
  }

  /**
   * PUT /api/assessments/{id}
   * Controller: Update(Guid id, [FromBody] UpdateAssessmentRequest request)
   */
  update(id: string, payload: UpdateAssessmentRequest): Observable<AssessmentResponse> {
    return this._data(
      this._http.put<ApiResponse<AssessmentResponse>>(`${this._url}/${id}`, payload)
    );
  }

  /**
   * DELETE /api/assessments/{id}?type={type}
   * Controller: Delete(Guid id, [FromQuery] AssessmentTypeDto type)
   */
  delete(id: string, type: AssessmentType): Observable<void> {
    const params = new HttpParams().set('type', type.toString());
    return this._data(
      this._http.delete<ApiResponse<void>>(`${this._url}/${id}`, { params })
    );
  }

  /**
   * PATCH /api/assessments/{id}/publish
   * Controller: Publish(Guid id, [FromBody] PublishAssessmentRequest request)
   * Backend expects AssessmentType in the body as { assessmentType: number }.
   */
  publish(id: string, type: AssessmentType): Observable<void> {
    return this._data(
      this._http.patch<ApiResponse<void>>(
        `${this._url}/${id}/publish`,
        { assessmentType: type }  // maps to PublishAssessmentRequest.AssessmentType
      )
    );
  }

  /**
   * GET /api/assessments/{id}/scores?type={type}
   * Controller: GetScores(Guid id, [FromQuery] AssessmentTypeDto type)
   */
  getScores(id: string, type: AssessmentType): Observable<any[]> {
    const params = new HttpParams().set('type', type.toString());
    return this._data(
      this._http.get<ApiResponse<any[]>>(`${this._url}/${id}/scores`, { params })
    );
  }

  /**
   * POST /api/assessments/scores
   * Controller: UpsertScore([FromBody] UpsertScoreRequest request)
   */
  upsertScore(payload: any): Observable<any> {
    return this._data(
      this._http.post<ApiResponse<any>>(`${this._url}/scores`, payload)
    );
  }

  /**
   * DELETE /api/assessments/scores/{scoreId}?type={type}
   * Controller: DeleteScore(Guid scoreId, [FromQuery] AssessmentTypeDto type)
   */
  deleteScore(scoreId: string, type: AssessmentType): Observable<void> {
    const params = new HttpParams().set('type', type.toString());
    return this._data(
      this._http.delete<ApiResponse<void>>(`${this._url}/scores/${scoreId}`, { params })
    );
>>>>>>> upstream/main
  }

  // ── Lookups ───────────────────────────────────────────────────────────────

<<<<<<< HEAD
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
=======
  /**
   * GET /api/schools
   * SuperAdmin only — populates the school picker in the identity step.
   */
  getSchools(): Observable<SchoolLookup[]> {
    return this._data(
      this._http.get<ApiResponse<SchoolLookup[]>>(`${this._apiBase}/api/schools`)
    );
  }

  /**
   * GET /api/academic/Teachers[?schoolId={id}]
   * schoolId passed only by SuperAdmin after selecting a school.
   */
  getTeachers(schoolId?: string): Observable<TeacherLookup[]> {
    return this._data(
      this._http.get<ApiResponse<TeacherLookup[]>>(
        `${this._apiBase}/api/academic/Teachers`,
        { params: this._schoolParams(schoolId) }
      )
    );
  }

  /**
   * GET /api/academic/Subjects[?schoolId={id}]
   */
  getSubjects(schoolId?: string): Observable<SubjectLookup[]> {
    return this._data(
      this._http.get<ApiResponse<SubjectLookup[]>>(
        `${this._apiBase}/api/academic/Subjects`,
        { params: this._schoolParams(schoolId) }
      )
    );
  }

  /**
   * GET /api/academic/Class[?schoolId={id}]
   */
  getClasses(schoolId?: string): Observable<ClassLookup[]> {
    return this._data(
      this._http.get<ApiResponse<ClassLookup[]>>(
        `${this._apiBase}/api/academic/Class`,
        { params: this._schoolParams(schoolId) }
      )
    );
  }

  /**
   * GET /api/academic/Terms[?schoolId={id}]
   */
  getTerms(schoolId?: string): Observable<TermLookup[]> {
    return this._data(
      this._http.get<ApiResponse<TermLookup[]>>(
        `${this._apiBase}/api/academic/Terms`,
        { params: this._schoolParams(schoolId) }
      )
    );
  }

  /**
   * GET /api/academic/AcademicYear[?schoolId={id}]
   */
  getAcademicYears(schoolId?: string): Observable<AcademicYearLookup[]> {
    return this._data(
      this._http.get<ApiResponse<AcademicYearLookup[]>>(
        `${this._apiBase}/api/academic/AcademicYear`,
        { params: this._schoolParams(schoolId) }
      )
    );
  }

  /**
   * GET /api/lookups/learning-outcomes
   * Not school-scoped — CBC learning outcomes are global curriculum data.
   */
  getLearningOutcomes(): Observable<LearningOutcomeLookup[]> {
    return this._data(
      this._http.get<ApiResponse<LearningOutcomeLookup[]>>(
        `${this._apiBase}/api/lookups/learning-outcomes`
      )
    ).pipe(
      map(res => res?.length
        ? res
        : [{ id: '0', outcome: 'No Learning Outcomes Available' }]
      )
>>>>>>> upstream/main
    );
  }
}