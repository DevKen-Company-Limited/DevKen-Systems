import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { ApiResponse, CreateSchoolRequest, PagedResponse, SchoolDto, SchoolStats, UpdateSchoolRequest, UpdateSchoolStatusRequest } from 'app/Tenant/types/school';


@Injectable({
  providedIn: 'root'
})
export class SchoolService {
  private readonly _http = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _url = `${this._apiBase}/api/schools`;

  // ------------------------------------------------------
  // CRUD Operations
  // ------------------------------------------------------

  /** Get all schools */
  getAll(): Observable<ApiResponse<SchoolDto[]>> {
    return this._http.get<ApiResponse<SchoolDto[]>>(`${this._url}`);
  }

  /** Get paginated list of schools */
  getPaginated(pageNumber: number = 1, pageSize: number = 20): Observable<PagedResponse<SchoolDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this._http.get<PagedResponse<SchoolDto>>(`${this._url}/paginated`, { params });
  }

  /** Get school by ID */
  getById(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(`${this._url}/${id}`);
  }

  /** Create a new school */
  create(payload: CreateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.post<ApiResponse<SchoolDto>>(`${this._url}`, payload);
  }

  /** Update school */
  update(id: string, payload: UpdateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.put<ApiResponse<SchoolDto>>(`${this._url}/${id}`, payload);
  }

  /** Delete school */
  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`);
  }

  // ------------------------------------------------------
  // Status Management
  // ------------------------------------------------------

  /** Activate a school */
  activate(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.patch<ApiResponse<SchoolDto>>(
      `${this._url}/${id}/activate`, 
      {}
    );
  }

  /** Deactivate a school */
  deactivate(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.patch<ApiResponse<SchoolDto>>(
      `${this._url}/${id}/deactivate`, 
      {}
    );
  }

  /** Update school status (active/inactive) */
  updateStatus(id: string, isActive: boolean): Observable<ApiResponse<SchoolDto>> {
    const payload: UpdateSchoolStatusRequest = { isActive };
    return this._http.patch<ApiResponse<SchoolDto>>(
      `${this._url}/${id}/status`, 
      payload
    );
  }

  /** Bulk update school statuses */
  bulkUpdateStatus(schoolIds: string[], isActive: boolean): Observable<ApiResponse<any>> {
    const payload = { schoolIds, isActive };
    return this._http.patch<ApiResponse<any>>(
      `${this._url}/bulk-status`,
      payload
    );
  }

  // ------------------------------------------------------
  // Search & Filter
  // ------------------------------------------------------

  /** Search schools by name, email, or phone */
  search(term: string): Observable<ApiResponse<SchoolDto[]>> {
    const params = new HttpParams().set('searchTerm', term);
    return this._http.get<ApiResponse<SchoolDto[]>>(
      `${this._url}/search`,
      { params }
    );
  }

  /** Filter schools by status */
  filterByStatus(isActive: boolean): Observable<ApiResponse<SchoolDto[]>> {
    const params = new HttpParams().set('isActive', isActive.toString());
    return this._http.get<ApiResponse<SchoolDto[]>>(
      `${this._url}/filter`,
      { params }
    );
  }

  /** Get school by slug */
  getBySlug(slug: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(
      `${this._url}/slug/${encodeURIComponent(slug)}`
    );
  }

  /** Check if slug is available */
  checkSlugAvailability(slug: string): Observable<ApiResponse<{ available: boolean }>> {
    const params = new HttpParams().set('slug', slug);
    return this._http.get<ApiResponse<{ available: boolean }>>(
      `${this._url}/check-slug`,
      { params }
    );
  }

  // ------------------------------------------------------
  // Statistics & Analytics
  // ------------------------------------------------------

  /** Get school statistics */
  getStats(): Observable<ApiResponse<SchoolStats>> {
    return this._http.get<ApiResponse<SchoolStats>>(`${this._url}/stats`);
  }

  /** Get school counts by status */
  getCountsByStatus(): Observable<ApiResponse<{ active: number, inactive: number, total: number }>> {
    return this._http.get<ApiResponse<{ active: number, inactive: number, total: number }>>(
      `${this._url}/counts`
    );
  }

  /** Get recent activity */
  getRecentActivity(limit: number = 10): Observable<ApiResponse<SchoolDto[]>> {
    const params = new HttpParams().set('limit', limit.toString());
    return this._http.get<ApiResponse<SchoolDto[]>>(
      `${this._url}/recent`,
      { params }
    );
  }

  // ------------------------------------------------------
  // Bulk Operations
  // ------------------------------------------------------

  /** Bulk create schools (import) */
  bulkCreate(schools: CreateSchoolRequest[]): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(
      `${this._url}/bulk`,
      { schools }
    );
  }

  /** Bulk delete schools */
  bulkDelete(schoolIds: string[]): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(
      `${this._url}/bulk-delete`,
      { schoolIds }
    );
  }

  /** Export schools to CSV/Excel */
  export(format: 'csv' | 'excel' = 'csv'): Observable<Blob> {
    const params = new HttpParams().set('format', format);
    return this._http.get(`${this._url}/export`, {
      params,
      responseType: 'blob'
    });
  }

  // ------------------------------------------------------
  // Utility Methods
  // ------------------------------------------------------

  /** Validate school data before submission */
  validateSchoolData(data: Partial<CreateSchoolRequest>): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!data.name?.trim()) {
      errors.push('School name is required');
    }

    if (!data.slugName?.trim()) {
      errors.push('Slug is required');
    } else if (!/^[a-z0-9-]+$/.test(data.slugName)) {
      errors.push('Slug can only contain lowercase letters, numbers, and hyphens');
    }

    if (data.email && !this.isValidEmail(data.email)) {
      errors.push('Invalid email format');
    }

    if (data.phoneNumber && !this.isValidPhoneNumber(data.phoneNumber)) {
      errors.push('Invalid phone number format');
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /** Generate slug from school name */
  generateSlug(name: string): string {
    return name
      .toLowerCase()
      .replace(/[^\w\s-]/g, '') // Remove special characters
      .replace(/\s+/g, '-')     // Replace spaces with hyphens
      .replace(/-+/g, '-')      // Remove duplicate hyphens
      .trim();
  }

  // ------------------------------------------------------
  // Private Helper Methods
  // ------------------------------------------------------

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  private isValidPhoneNumber(phone: string): boolean {
    // Basic phone validation - can be customized based on requirements
    const phoneRegex = /^[\d\s\-\+\(\)]{10,}$/;
    return phoneRegex.test(phone.replace(/\s/g, ''));
  }
}