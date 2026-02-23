// ═══════════════════════════════════════════════════════════════════
// summative-assessment.service.ts
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { SummativeAssessmentDto, CreateSummativeAssessmentRequest, UpdateSummativeAssessmentRequest, SummativeAssessmentScoreDto, CreateSummativeAssessmentScoreRequest, UpdateSummativeAssessmentScoreRequest } from 'app/assessment/types/summative-assessment.types';


interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class SummativeAssessmentService {
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _base    = `${this._apiBase}/api/SummativeAssessments`;

  constructor(private _http: HttpClient) {}

  // ── Assessments ──────────────────────────────────────────────────────────

  getAll(): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(this._base).pipe(
      map(r => r?.data ?? [])
    );
  }

  getById(id: string): Observable<SummativeAssessmentDto> {
    return this._http.get<ApiResponse<SummativeAssessmentDto>>(`${this._base}/${id}`).pipe(
      map(r => r?.data ?? r as any)
    );
  }

  getWithScores(id: string): Observable<SummativeAssessmentDto> {
    return this._http.get<ApiResponse<SummativeAssessmentDto>>(`${this._base}/${id}/scores`).pipe(
      map(r => r?.data ?? r as any)
    );
  }

  getByClass(classId: string): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(`${this._base}/class/${classId}`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getByTeacher(teacherId: string): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(`${this._base}/teacher/${teacherId}`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getByTerm(termId: string, academicYearId: string): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(
      `${this._base}/term/${termId}/academic-year/${academicYearId}`
    ).pipe(map(r => r?.data ?? []));
  }

  getByExamType(examType: string): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(`${this._base}/exam-type/${examType}`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getPublished(classId: string, termId: string): Observable<SummativeAssessmentDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentDto[]>>(
      `${this._base}/published/class/${classId}/term/${termId}`
    ).pipe(map(r => r?.data ?? []));
  }

  create(payload: CreateSummativeAssessmentRequest): Observable<ApiResponse<SummativeAssessmentDto>> {
    return this._http.post<ApiResponse<SummativeAssessmentDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateSummativeAssessmentRequest): Observable<ApiResponse<SummativeAssessmentDto>> {
    return this._http.put<ApiResponse<SummativeAssessmentDto>>(`${this._base}/${id}`, payload);
  }

  publish(id: string, isPublished: boolean): Observable<ApiResponse<null>> {
    return this._http.patch<ApiResponse<null>>(`${this._base}/${id}/publish`, { isPublished });
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`);
  }

  // ── Scores ───────────────────────────────────────────────────────────────

  getScoresByAssessment(assessmentId: string): Observable<SummativeAssessmentScoreDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentScoreDto[]>>(
      `${this._base}/${assessmentId}/score-entries`
    ).pipe(map(r => r?.data ?? []));
  }

  getScoresByStudent(studentId: string): Observable<SummativeAssessmentScoreDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentScoreDto[]>>(
      `${this._base}/scores/student/${studentId}`
    ).pipe(map(r => r?.data ?? []));
  }

  getScoresByStudentAndTerm(studentId: string, termId: string): Observable<SummativeAssessmentScoreDto[]> {
    return this._http.get<ApiResponse<SummativeAssessmentScoreDto[]>>(
      `${this._base}/scores/student/${studentId}/term/${termId}`
    ).pipe(map(r => r?.data ?? []));
  }

  getScoreById(scoreId: string): Observable<SummativeAssessmentScoreDto> {
    return this._http.get<ApiResponse<SummativeAssessmentScoreDto>>(
      `${this._base}/scores/${scoreId}`
    ).pipe(map(r => r?.data ?? r as any));
  }

  createScore(payload: CreateSummativeAssessmentScoreRequest): Observable<ApiResponse<SummativeAssessmentScoreDto>> {
    return this._http.post<ApiResponse<SummativeAssessmentScoreDto>>(`${this._base}/scores`, payload);
  }

  updateScore(scoreId: string, payload: UpdateSummativeAssessmentScoreRequest): Observable<ApiResponse<SummativeAssessmentScoreDto>> {
    return this._http.put<ApiResponse<SummativeAssessmentScoreDto>>(`${this._base}/scores/${scoreId}`, payload);
  }

  recalculatePositions(assessmentId: string): Observable<ApiResponse<null>> {
    return this._http.patch<ApiResponse<null>>(`${this._base}/${assessmentId}/recalculate-positions`, {});
  }

  deleteScore(scoreId: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/scores/${scoreId}`);
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

  getStudentsByClass(classId: string): Observable<any[]> {
    return this._http.get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/students`).pipe(
      map(r => (r?.data ?? []).filter((s: any) => s.currentClassId === classId))
    );
  }
}