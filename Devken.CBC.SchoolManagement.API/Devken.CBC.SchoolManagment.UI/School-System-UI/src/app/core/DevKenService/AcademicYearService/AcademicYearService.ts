import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { 
  AcademicYearDto, 
  CreateAcademicYearRequest, 
  UpdateAcademicYearRequest,
  ApiResponse 
} from 'app/Academics/AcademicYear/Types/AcademicYear';

@Injectable({ providedIn: 'root' })
export class AcademicYearService {
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/academicyear`;
  private http = inject(HttpClient);

  /**
   * Get all academic years with optional school filter
   */
  getAll(schoolId?: string): Observable<ApiResponse<AcademicYearDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<AcademicYearDto[]>>(this.baseUrl, { params });
  }

  /**
   * Get academic year by ID
   */
  getById(id: string): Observable<ApiResponse<AcademicYearDto>> {
    return this.http.get<ApiResponse<AcademicYearDto>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Get current academic year for school
   */
  getCurrent(schoolId?: string): Observable<ApiResponse<AcademicYearDto>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<AcademicYearDto>>(`${this.baseUrl}/current`, { params });
  }

  /**
   * Get open academic years for school
   */
  getOpen(schoolId?: string): Observable<ApiResponse<AcademicYearDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<AcademicYearDto[]>>(`${this.baseUrl}/open`, { params });
  }

  /**
   * Create new academic year
   */
  create(request: CreateAcademicYearRequest): Observable<ApiResponse<AcademicYearDto>> {
    return this.http.post<ApiResponse<AcademicYearDto>>(this.baseUrl, request);
  }

  /**
   * Update academic year
   */
  update(id: string, request: UpdateAcademicYearRequest): Observable<ApiResponse<AcademicYearDto>> {
    return this.http.put<ApiResponse<AcademicYearDto>>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Set academic year as current
   */
  setAsCurrent(id: string): Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(`${this.baseUrl}/${id}/set-current`, {});
  }

  /**
   * Close academic year
   */
  close(id: string): Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(`${this.baseUrl}/${id}/close`, {});
  }

  /**
   * Delete academic year
   */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Preview next code that will be generated
   */
  previewNextCode(schoolId?: string): Observable<ApiResponse<{ nextCode: string }>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<{ nextCode: string }>>(`${this.baseUrl}/preview-next-code`, { params });
  }
}