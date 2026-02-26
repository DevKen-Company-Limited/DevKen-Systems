// ═══════════════════════════════════════════════════════════════════
// assessment.service.ts
//
// FIXES
// ─────
// FIX 1 – duration type mismatch (HTTP 400 "$.duration could not be
//   converted to System.Nullable`1[System.TimeSpan]")
//
//   C# CreateAssessmentRequest / UpdateAssessmentRequest declare
//   Duration as TimeSpan? which ASP.NET Core deserialises from a
//   JSON string in "HH:mm:ss" format.  The Angular form stores
//   duration as a plain number (minutes).  _prepareRequest() converts
//   minutes → "HH:mm:ss" before every POST / PUT.
//
//   AssessmentResponse.Duration returns int? (total minutes) so the
//   form keeps it as-is on load.  timeSpanToMinutes() handles the
//   edge case where a string might arrive.
//
// FIX 2 – "The request field is required" (HTTP 400)
//   Cascading consequence of FIX 1: when duration caused the entire
//   body to fail JSON-binding, ASP.NET Core reported the [FromBody]
//   parameter itself as missing.  Resolved by FIX 1.
//
// FIX 3 – getLearningOutcomes query param
//   Sends ?subStrandId= (not ?strandId=) matching the C# controller.
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import {
  AssessmentType,
  AssessmentListItem,
  AssessmentResponse,
  CreateAssessmentRequest,
  UpdateAssessmentRequest,
  AssessmentScoreResponse,
  UpsertScoreRequest,
  AssessmentSchemaResponse,
} from 'app/assessment/types/assessments';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

// ─────────────────────────────────────────────────────────────────────────
// DURATION HELPERS  (exported so the form step can import them)
//
// The API write contract:  Duration = TimeSpan?  → JSON "HH:mm:ss"
// The API read contract:   Duration = int?       → JSON number (minutes)
// ─────────────────────────────────────────────────────────────────────────

/**
 * Convert total minutes (number) to the "HH:mm:ss" TimeSpan string
 * that ASP.NET Core's TimeSpan? binder expects on POST / PUT.
 * Returns null for null/undefined/NaN input.
 */
export function minutesToTimeSpan(minutes: number | null | undefined): string | null {
  if (minutes == null || isNaN(Number(minutes))) return null;
  const m  = Number(minutes);
  const hh = Math.floor(m / 60).toString().padStart(2, '0');
  const mm = (m % 60).toString().padStart(2, '0');
  return `${hh}:${mm}:00`;
}

/**
 * Convert an "HH:mm:ss" TimeSpan string back to total minutes for
 * display in the form's number input.
 * Also handles a plain number (already minutes — pass through).
 */
export function timeSpanToMinutes(value: string | number | null | undefined): number | null {
  if (value == null) return null;
  if (typeof value === 'number') return value;   // already minutes
  const parts = String(value).split(':');
  if (parts.length < 2) return null;
  return parseInt(parts[0], 10) * 60 + parseInt(parts[1], 10);
}

@Injectable({ providedIn: 'root' })
export class AssessmentService {

  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _http    = inject(HttpClient);
  private readonly _base    = `${this._apiBase}/api/assessments`;

  // ──────────────────────────────────────────────────────────────
  // GET ALL
  // GET /api/assessments?type=&classId=&termId=&subjectId=&teacherId=&isPublished=
  // ──────────────────────────────────────────────────────────────
  getAll(
    type?:        AssessmentType,
    classId?:     string,
    termId?:      string,
    subjectId?:   string,
    teacherId?:   string,
    isPublished?: boolean,
  ): Observable<AssessmentListItem[]> {
    let params = new HttpParams();
    if (type        != null) params = params.set('type',        String(type));
    if (classId)             params = params.set('classId',     classId);
    if (termId)              params = params.set('termId',      termId);
    if (subjectId)           params = params.set('subjectId',   subjectId);
    if (teacherId)           params = params.set('teacherId',   teacherId);
    if (isPublished != null) params = params.set('isPublished', String(isPublished));
    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(this._base, { params })
      .pipe(map(r => r.data ?? []));
  }

  // ──────────────────────────────────────────────────────────────
  // GET BY ID   GET /api/assessments/{id}?type=1
  // ──────────────────────────────────────────────────────────────
  getById(id: string, type: AssessmentType): Observable<AssessmentResponse> {
    const params = new HttpParams().set('type', String(type));
    return this._http
      .get<ApiResponse<AssessmentResponse>>(`${this._base}/${id}`, { params })
      .pipe(map(r => r.data));
  }

  // ──────────────────────────────────────────────────────────────
  // CREATE   POST /api/assessments
  // FIX 1: _prepareRequest converts duration minutes → "HH:mm:ss"
  // ──────────────────────────────────────────────────────────────
  create(request: CreateAssessmentRequest): Observable<AssessmentResponse> {
    return this._http
      .post<ApiResponse<AssessmentResponse>>(this._base, this._prepareRequest(request))
      .pipe(map(r => r.data));
  }

  // ──────────────────────────────────────────────────────────────
  // UPDATE   PUT /api/assessments/{id}
  // FIX 1: _prepareRequest converts duration minutes → "HH:mm:ss"
  // ──────────────────────────────────────────────────────────────
  update(id: string, request: UpdateAssessmentRequest): Observable<AssessmentResponse> {
    return this._http
      .put<ApiResponse<AssessmentResponse>>(`${this._base}/${id}`, this._prepareRequest(request))
      .pipe(map(r => r.data));
  }

  // ──────────────────────────────────────────────────────────────
  // PUBLISH   PATCH /api/assessments/{id}/publish
  // Body: { assessmentType: <number> }  — C# deserialises int enums correctly.
  // ──────────────────────────────────────────────────────────────
  publish(
    id:   string,
    type: AssessmentType,
  ): Observable<{ success: boolean; message: string }> {
    return this._http.patch<{ success: boolean; message: string }>(
      `${this._base}/${id}/publish`,
      { assessmentType: type },
    );
  }

  // ──────────────────────────────────────────────────────────────
  // DELETE   DELETE /api/assessments/{id}?type=1
  // ──────────────────────────────────────────────────────────────
  delete(
    id:   string,
    type: AssessmentType,
  ): Observable<{ success: boolean; message: string }> {
    const params = new HttpParams().set('type', String(type));
    return this._http.delete<{ success: boolean; message: string }>(
      `${this._base}/${id}`,
      { params },
    );
  }

  // ──────────────────────────────────────────────────────────────
  // GET SCORES   GET /api/assessments/{id}/scores?type=1
  // ──────────────────────────────────────────────────────────────
  getScores(id: string, type: AssessmentType): Observable<AssessmentScoreResponse[]> {
    const params = new HttpParams().set('type', String(type));
    return this._http
      .get<ApiResponse<AssessmentScoreResponse[]>>(
        `${this._base}/${id}/scores`,
        { params },
      )
      .pipe(map(r => r.data ?? []));
  }

  // ──────────────────────────────────────────────────────────────
  // UPSERT SCORE   POST /api/assessments/scores
  // ──────────────────────────────────────────────────────────────
  upsertScore(request: UpsertScoreRequest): Observable<AssessmentScoreResponse> {
    return this._http
      .post<ApiResponse<AssessmentScoreResponse>>(`${this._base}/scores`, request)
      .pipe(map(r => r.data));
  }

  // ──────────────────────────────────────────────────────────────
  // DELETE SCORE   DELETE /api/assessments/scores/{scoreId}?type=1
  // ──────────────────────────────────────────────────────────────
  deleteScore(
    scoreId: string,
    type:    AssessmentType,
  ): Observable<{ success: boolean; message: string }> {
    const params = new HttpParams().set('type', String(type));
    return this._http.delete<{ success: boolean; message: string }>(
      `${this._base}/scores/${scoreId}`,
      { params },
    );
  }

  // ──────────────────────────────────────────────────────────────
  // GET SCHEMA   GET /api/assessments/schema/{type}
  // ──────────────────────────────────────────────────────────────
  getSchema(type: AssessmentType): Observable<AssessmentSchemaResponse> {
    return this._http
      .get<ApiResponse<AssessmentSchemaResponse>>(`${this._base}/schema/${type}`)
      .pipe(map(r => r.data));
  }

  // ════════════════════════════════════════════════════════════════
  // LOOKUPS — all accept optional schoolId for SuperAdmin cascade.
  // ════════════════════════════════════════════════════════════════

  /** GET /api/academic/class[?schoolId=] */
  getClasses(schoolId?: string): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/class`, { params: this._schoolParam(schoolId) })
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/academic/teachers[?schoolId=] */
  getTeachers(schoolId?: string): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/teachers`, { params: this._schoolParam(schoolId) })
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/academic/subjects[?schoolId=] */
  getSubjects(schoolId?: string): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/subjects`, { params: this._schoolParam(schoolId) })
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/academic/terms[?schoolId=] */
  getTerms(schoolId?: string): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/terms`, { params: this._schoolParam(schoolId) })
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/academic/AcademicYear[?schoolId=] */
  getAcademicYears(schoolId?: string): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/academic/AcademicYear`, { params: this._schoolParam(schoolId) })
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/schools  (SuperAdmin only) */
  getSchools(): Observable<any[]> {
    return this._http
      .get<any>(`${this._apiBase}/api/schools`)
      .pipe(map(r => r.data ?? r));
  }

  /** GET /api/academic/students[?classId=&schoolId=] */
  getStudents(classId?: string, schoolId?: string): Observable<any[]> {
    let params = new HttpParams();
    if (classId)  params = params.set('classId',  classId);
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http
      .get<any>(`${this._apiBase}/api/academic/students`, { params })
      .pipe(map(r => r.data ?? r));
  }

  /**
   * GET /api/curriculum/strands[?subjectId=&schoolId=]
   * Curriculum strands — optionally scoped by subject and school.
   */
  getStrands(subjectId?: string, schoolId?: string): Observable<any[]> {
    let params = new HttpParams();
    if (subjectId) params = params.set('subjectId', subjectId);
    if (schoolId)  params = params.set('schoolId',  schoolId);
    return this._http
      .get<any>(`${this._apiBase}/api/curriculum/strands`, { params })
      .pipe(map(r => r.data ?? r));
  }

  /**
   * GET /api/curriculum/substrands[?strandId=&schoolId=]
   * Sub-strands — filtered by parent strandId.
   */
  getSubStrands(strandId?: string, schoolId?: string): Observable<any[]> {
    let params = new HttpParams();
    if (strandId) params = params.set('strandId', strandId);
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http
      .get<any>(`${this._apiBase}/api/curriculum/substrands`, { params })
      .pipe(map(r => r.data ?? r));
  }

  /**
   * GET /api/curriculum/learningoutcomes[?subStrandId=&schoolId=]
   *
   * FIX 3: The C# controller accepts [FromQuery] Guid? subStrandId — NOT strandId.
   * Previous implementation incorrectly sent ?strandId= which returned all
   * outcomes rather than outcomes scoped to the selected sub-strand.
   */
  getLearningOutcomes(subStrandId?: string, schoolId?: string): Observable<any[]> {
    let params = new HttpParams();
    if (subStrandId) params = params.set('subStrandId', subStrandId); // ← was 'strandId' — FIXED
    if (schoolId)    params = params.set('schoolId',    schoolId);
    return this._http
      .get<any>(`${this._apiBase}/api/curriculum/learningoutcomes`, { params })
      .pipe(map(r => r.data ?? r));
  }

  // ── Private helpers ──────────────────────────────────────────────

  private _schoolParam(schoolId?: string): HttpParams | undefined {
    return schoolId ? new HttpParams().set('schoolId', schoolId) : undefined;
  }

  /**
   * FIX 1: Pre-process a create/update request before sending to the API.
   *
   * The only transformation needed right now is duration:
   *   Form stores:  data.duration = 150          (number, minutes)
   *   API expects:  "duration": "02:30:00"        (TimeSpan string)
   *
   * If duration is already a "HH:mm:ss" string (edge case — e.g. user
   * re-submits without touching the field after a failed save), it is
   * left unchanged.  null / undefined are passed through as-is so the
   * field is simply omitted from the payload by JSON serialisation.
   */
  private _prepareRequest(
    request: CreateAssessmentRequest | UpdateAssessmentRequest,
  ): any {
    const payload: any = { ...request };

    if (payload.duration != null) {
      if (typeof payload.duration === 'number') {
        payload.duration = minutesToTimeSpan(payload.duration);
      }
      // Already a "HH:mm:ss" string — leave it alone
    }

    return payload;
  }
}