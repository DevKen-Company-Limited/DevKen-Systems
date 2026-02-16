import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { CreateStudentRequest, StudentDto, UpdateStudentRequest } from 'app/administration/students/types/studentdto';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class StudentService {

  private readonly apiBase = inject(API_BASE_URL);
  private readonly base = `${this.apiBase}/api/academic/students`;

  constructor(private http: HttpClient) {}

  // ── CRUD Operations ──────────────────────────────────────────────────────
  
  /**
   * Get all students, optionally filtered by school (SuperAdmin)
   */
  getAll(schoolId?: string): Observable<StudentDto[]> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }

    return this.http.get<ApiResponse<StudentDto[]>>(this.base, { params }).pipe(
      map(response => {
        if (response && response.data) {
          return response.data;
        }
        // Fallback for direct array response
        return Array.isArray(response) ? response : [];
      })
    );
  }

  /**
   * Get a single student by ID
   */
  getById(id: string): Observable<StudentDto> {
    return this.http.get<ApiResponse<StudentDto>>(`${this.base}/${id}`).pipe(
      map(response => response?.data ?? response as any)
    );
  }

  /**
   * Create a new student
   */
  create(payload: CreateStudentRequest): Observable<ApiResponse<StudentDto>> {
    return this.http.post<ApiResponse<StudentDto>>(this.base, payload);
  }

  /**
   * Full update of a student
   */
  update(id: string, payload: UpdateStudentRequest): Observable<ApiResponse<StudentDto>> {
    return this.http.put<ApiResponse<StudentDto>>(`${this.base}/${id}`, payload);
  }

  /**
   * Partial update of a student (e.g., just toggle active status)
   */
  updatePartial(id: string, payload: Partial<StudentDto>): Observable<ApiResponse<StudentDto>> {
    return this.update(id, payload);
  }

  /**
   * Delete a student
   */
  delete(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.base}/${id}`);
  }

  /**
   * Toggle student active status
   */
toggleStatus(id: string, isActive: boolean): Observable<ApiResponse<StudentDto>> {
  return this.http.patch<ApiResponse<StudentDto>>(
    `${this.base}/${id}/toggle-status`,
    { isActive }  
  );
}


  /**
   * Upload student photo
   * IMPORTANT: Backend expects form field name 'file', not 'photo'
   */
  uploadPhoto(id: string, file: File): Observable<ApiResponse<{ photoUrl: string }>> {
    const formData = new FormData();
    formData.append('file', file);  // Backend controller parameter name is 'file'
    return this.http.post<ApiResponse<{ photoUrl: string }>>(`${this.base}/${id}/photo`, formData);
  }

  // ── Lookup Data Helpers ──────────────────────────────────────────────────
  
  /**
   * Get all schools (for SuperAdmin)
   */
  getSchools(): Observable<any[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiBase}/api/schools`).pipe(
      map(response => response?.data ?? [])
    );
  }

  /**
   * Get all classes
   */
  getClasses(): Observable<any[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiBase}/api/academic/class`).pipe(
      map(response => response?.data ?? [])
    );
  }

  /**
   * Get all academic years
   */
  getAcademicYears(): Observable<any[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiBase}/api/academic/academicyear`).pipe(
      map(response => response?.data ?? [])
    );
  }

  // ── Bulk Operations ──────────────────────────────────────────────────────
  
  /**
   * Bulk activate/deactivate students
   */
  bulkUpdateStatus(ids: string[], isActive: boolean): Observable<ApiResponse<void>> {
    return this.http.patch<ApiResponse<void>>(`${this.base}/bulk/status`, { ids, isActive });
  }

  /**
   * Bulk delete students
   */
  bulkDelete(ids: string[]): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.base}/bulk/delete`, { ids });
  }

  // ── Export Operations ────────────────────────────────────────────────────
  
  /**
   * Export students to Excel
   */
  exportToExcel(schoolId?: string): Observable<Blob> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    
    return this.http.get(`${this.base}/export/excel`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Export students to PDF
   */
  exportToPDF(schoolId?: string): Observable<Blob> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    
    return this.http.get(`${this.base}/export/pdf`, {
      params,
      responseType: 'blob'
    });
  }

  // ── Statistics ───────────────────────────────────────────────────────────
  
  /**
   * Get student statistics
   */
  getStatistics(schoolId?: string): Observable<any> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    
    return this.http.get<ApiResponse<any>>(`${this.base}/statistics`, { params }).pipe(
      map(response => response?.data ?? {})
    );
  }

  

  /**
   * Get enrollment trends
   */
  getEnrollmentTrends(schoolId?: string, period: 'week' | 'month' | 'year' = 'month'): Observable<any> {
    let params = new HttpParams().set('period', period);
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    
    return this.http.get<ApiResponse<any>>(`${this.base}/trends/enrollment`, { params }).pipe(
      map(response => response?.data ?? [])
    );
  }
}