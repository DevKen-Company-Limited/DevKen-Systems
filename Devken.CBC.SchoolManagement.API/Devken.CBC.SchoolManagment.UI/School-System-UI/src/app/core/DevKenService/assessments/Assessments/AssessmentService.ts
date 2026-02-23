// ═══════════════════════════════════════════════════════════════════
// assessment.service.ts
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import {
  AssessmentDto,
  AssessmentListItem,
  AssessmentType,
  CreateAssessmentRequest,
  UpdateAssessmentRequest,
} from 'app/assessment/types/AssessmentDtos';

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

  // ── Assessments ───────────────────────────────────────────────────────────

  /**
   * Returns the flat list for a given assessment type.
   * Mirrors GET /api/Assessments?assessmentType={type}
   */
getAll(assessmentType: AssessmentType): Observable<AssessmentListItem[]> {
  return this._http
    .get<ApiResponse<AssessmentListItem[]>>(this._base, {
      params: { assessmentType: String(assessmentType) },
    })
    .pipe(map(r => r?.data ?? []));
}


getById(id: string, assessmentType: AssessmentType): Observable<AssessmentDto> {
  return this._http
    .get<ApiResponse<AssessmentDto>>(`${this._base}/${id}`, {
      params: { type: String(assessmentType) },
    })
    .pipe(map(r => r?.data ?? (r as any)));
}

  getWithGrades(id: string): Observable<any> {
    return this._http
      .get<ApiResponse<any>>(`${this._base}/${id}/grades`)
      .pipe(map(r => r?.data ?? (r as any)));
  }

  getByClass(classId: string): Observable<AssessmentListItem[]> {
    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(`${this._base}/class/${classId}`)
      .pipe(map(r => r?.data ?? []));
  }

  getByTeacher(teacherId: string): Observable<AssessmentListItem[]> {
    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(`${this._base}/teacher/${teacherId}`)
      .pipe(map(r => r?.data ?? []));
  }

  getByTerm(termId: string, academicYearId: string): Observable<AssessmentListItem[]> {
    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(
        `${this._base}/term/${termId}/academic-year/${academicYearId}`
      )
      .pipe(map(r => r?.data ?? []));
  }

  getPublished(classId: string, termId: string): Observable<AssessmentListItem[]> {
    return this._http
      .get<ApiResponse<AssessmentListItem[]>>(
        `${this._base}/published/class/${classId}/term/${termId}`
      )
      .pipe(map(r => r?.data ?? []));
  }

  create(payload: CreateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.post<ApiResponse<AssessmentDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.put<ApiResponse<AssessmentDto>>(`${this._base}/${id}`, payload);
  }

  /**
   * PATCH /api/Assessments/{id}/publish
   * assessmentType is required by the backend PublishAssessmentRequest.
   */
  publish(id: string, assessmentType: AssessmentType): Observable<ApiResponse<null>> {
    return this._http.patch<ApiResponse<null>>(`${this._base}/${id}/publish`, { assessmentType });
  }

  /**
   * DELETE /api/Assessments/{id}?assessmentType={type}
   * Type is sent as query param so the backend routes to the right table.
   */
  delete(id: string, assessmentType: AssessmentType): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`, {
      params: { assessmentType: String(assessmentType) },
    });
  }

  // ── Lookups ───────────────────────────────────────────────────────────────

  getClasses(schoolId?: string): Observable<any[]> {
  const params = schoolId ? { schoolId } : {};
  return this._http
    .get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/class`, { params })
    .pipe(map(r => r?.data ?? []));
}

getTeachers(schoolId?: string): Observable<any[]> {
  const params = schoolId ? { schoolId } : {};
  return this._http
    .get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/teachers`, { params })
    .pipe(map(r => r?.data ?? []));
}

getSubjects(schoolId?: string): Observable<any[]> {
  const params = schoolId ? { schoolId } : {};
  return this._http
    .get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/subjects`, { params })
    .pipe(map(r => r?.data ?? []));
}

getTerms(schoolId?: string): Observable<any[]> {
  const params = schoolId ? { schoolId } : {};
  return this._http
    .get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/terms`, { params })
    .pipe(map(r => r?.data ?? []));
}

getAcademicYears(schoolId?: string): Observable<any[]> {
  const params = schoolId ? { schoolId } : {};
  return this._http
    .get<ApiResponse<any[]>>(`${this._apiBase}/api/academic/academicyear`, { params })
    .pipe(map(r => r?.data ?? []));
}

  getSchools(): Observable<any[]> {
    return this._http
      .get<ApiResponse<any[]>>(`${this._apiBase}/api/schools`)
      .pipe(map(r => r?.data ?? []));
  }

  getStrands(subjectId?: string): Observable<any[]> {
    const url = subjectId
      ? `${this._apiBase}/api/academic/strands?subjectId=${subjectId}`
      : `${this._apiBase}/api/academic/strands`;
    return this._http.get<ApiResponse<any[]>>(url).pipe(map(r => r?.data ?? []));
  }

  getSubStrands(strandId?: string): Observable<any[]> {
    const url = strandId
      ? `${this._apiBase}/api/academic/substrands?strandId=${strandId}`
      : `${this._apiBase}/api/academic/substrands`;
    return this._http.get<ApiResponse<any[]>>(url).pipe(map(r => r?.data ?? []));
  }

  getLearningOutcomes(subStrandId?: string): Observable<any[]> {
    const url = subStrandId
      ? `${this._apiBase}/api/academic/learningoutcomes?subStrandId=${subStrandId}`
      : `${this._apiBase}/api/academic/learningoutcomes`;
    return this._http.get<ApiResponse<any[]>>(url).pipe(map(r => r?.data ?? []));
  }
}