// term.service.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { CloseTermRequest, CreateTermRequest, TermDto, UpdateTermRequest } from 'app/Academics/Terms/Types/types';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class TermService {
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/terms`;
  private http = inject(HttpClient);

  /**
   * Get all terms with optional school filter
   */
  getAll(schoolId?: string): Observable<ApiResponse<TermDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<TermDto[]>>(this.baseUrl, { params });
  }

  /**
   * Get terms by academic year
   */
  getByAcademicYear(academicYearId: string): Observable<ApiResponse<TermDto[]>> {
    return this.http.get<ApiResponse<TermDto[]>>(
      `${this.baseUrl}/academic-year/${academicYearId}`
    );
  }

  /**
   * Get term by ID
   */
  getById(id: string): Observable<ApiResponse<TermDto>> {
    return this.http.get<ApiResponse<TermDto>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Get current active term
   */
  getCurrent(schoolId?: string): Observable<ApiResponse<TermDto>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<TermDto>>(`${this.baseUrl}/current`, { params });
  }

  /**
   * Get all active (not closed) terms
   */
  getActive(schoolId?: string): Observable<ApiResponse<TermDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<TermDto[]>>(`${this.baseUrl}/active`, { params });
  }

  /**
   * Create new term
   */
  create(request: CreateTermRequest): Observable<ApiResponse<TermDto>> {
    return this.http.post<ApiResponse<TermDto>>(this.baseUrl, request);
  }

  /**
   * Update existing term
   */
  update(id: string, request: UpdateTermRequest): Observable<ApiResponse<TermDto>> {
    return this.http.put<ApiResponse<TermDto>>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Delete term
   */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Set term as current
   */
  setCurrent(termId: string): Observable<ApiResponse<TermDto>> {
    return this.http.patch<ApiResponse<TermDto>>(
      `${this.baseUrl}/${termId}/set-current`, 
      null
    );
  }

  /**
   * Close term
   */
  close(termId: string, request: CloseTermRequest): Observable<ApiResponse<TermDto>> {
    return this.http.patch<ApiResponse<TermDto>>(
      `${this.baseUrl}/${termId}/close`, 
      request
    );
  }

  /**
   * Reopen closed term
   */
  reopen(termId: string): Observable<ApiResponse<TermDto>> {
    return this.http.patch<ApiResponse<TermDto>>(
      `${this.baseUrl}/${termId}/reopen`, 
      null
    );
  }
}