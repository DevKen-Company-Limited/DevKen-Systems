import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { CreateAssessmentRequest, UpdateAssessmentRequest, AssessmentDto, UpdateAssessmentPublishRequest } from 'app/assessment/types/AssessmentDtos';
import { ICrudService } from 'app/shared/Services/ICrudService';


@Injectable({ providedIn: 'root' })
export class AssessmentService
  implements ICrudService<CreateAssessmentRequest, UpdateAssessmentRequest, AssessmentDto>
{
  private readonly _http    = inject(HttpClient);
  private readonly _baseUrl = inject(API_BASE_URL);

  private url(path = ''): string {
    return `${this._baseUrl}/api/assessments${path}`;
  }

  // ── ICrudService contract ────────────────────────────────────────────────

  create(payload: CreateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.post<ApiResponse<AssessmentDto>>(this.url(), payload);
  }

  update(id: string, payload: UpdateAssessmentRequest): Observable<ApiResponse<AssessmentDto>> {
    return this._http.put<ApiResponse<AssessmentDto>>(this.url(`/${id}`), payload);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(this.url(`/${id}`));
  }

  // ── Extra reads ──────────────────────────────────────────────────────────

  /** All assessments – SuperAdmin: all schools. Others: own school only. */
  getAll(schoolId?: string): Observable<ApiResponse<AssessmentDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<AssessmentDto[]>>(this.url(), { params });
  }

  getById(id: string): Observable<ApiResponse<AssessmentDto>> {
    return this._http.get<ApiResponse<AssessmentDto>>(this.url(`/${id}`));
  }

  getByClass(classId: string): Observable<ApiResponse<AssessmentDto[]>> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(this.url(`/class/${classId}`));
  }

  getByTeacher(teacherId: string): Observable<ApiResponse<AssessmentDto[]>> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(this.url(`/teacher/${teacherId}`));
  }

  getByTerm(termId: string, academicYearId: string): Observable<ApiResponse<AssessmentDto[]>> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(
      this.url(`/term/${termId}/academic-year/${academicYearId}`)
    );
  }

  getPublished(classId: string, termId: string): Observable<ApiResponse<AssessmentDto[]>> {
    return this._http.get<ApiResponse<AssessmentDto[]>>(
      this.url(`/published/class/${classId}/term/${termId}`)
    );
  }

  /** Publish or un-publish an assessment. */
  updatePublishStatus(
    id: string,
    payload: UpdateAssessmentPublishRequest
  ): Observable<ApiResponse<AssessmentDto>> {
    return this._http.patch<ApiResponse<AssessmentDto>>(this.url(`/${id}/publish`), payload);
  }
}