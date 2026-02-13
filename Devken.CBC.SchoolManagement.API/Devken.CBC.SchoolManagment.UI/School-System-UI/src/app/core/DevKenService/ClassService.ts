import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { ClassDto, ClassDetailDto, CreateClassRequest, UpdateClassRequest, getCBCLevelDisplay, CBCLevel } from 'app/Classes/Types/Class';
import { ApiResponse } from './Types/role-permissions';



@Injectable({
  providedIn: 'root'
})
export class ClassService {
  private readonly _http = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _url = `${this._apiBase}/api/academic/Class`;

  // ==================== Class CRUD ====================

  /**
   * Get all classes with optional filters
   */
  getAll(
    schoolId?: string,
    academicYearId?: string,
    level?: number,
    activeOnly?: boolean
  ): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    
    if (schoolId) params = params.set('schoolId', schoolId);
    if (academicYearId) params = params.set('academicYearId', academicYearId);
    if (level !== undefined) params = params.set('level', level.toString());
    if (activeOnly !== undefined) params = params.set('activeOnly', activeOnly.toString());

    return this._http.get<ApiResponse<ClassDto[]>>(this._url, { params }).pipe(
      tap(response => console.log('Classes API response:', response)),
      catchError(this.handleError<ClassDto[]>('getAll'))
    );
  }

  /**
   * Get class by ID
   */
  getById(id: string, includeDetails: boolean = false): Observable<ApiResponse<ClassDto | ClassDetailDto>> {
    let params = new HttpParams();
    if (includeDetails) params = params.set('includeDetails', 'true');

    return this._http.get<ApiResponse<ClassDto | ClassDetailDto>>(`${this._url}/${id}`, { params }).pipe(
      catchError(this.handleError<ClassDto | ClassDetailDto>('getById'))
    );
  }

  /**
   * Get classes by academic year
   */
  getByAcademicYear(academicYearId: string, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);

    return this._http.get<ApiResponse<ClassDto[]>>(`${this._url}/by-academic-year/${academicYearId}`, { params }).pipe(
      catchError(this.handleError<ClassDto[]>('getByAcademicYear'))
    );
  }

  /**
   * Get classes by CBC level
   */
  getByLevel(level: number, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);

    return this._http.get<ApiResponse<ClassDto[]>>(`${this._url}/by-level/${level}`, { params }).pipe(
      catchError(this.handleError<ClassDto[]>('getByLevel'))
    );
  }

  /**
   * Get classes by teacher
   */
  getByTeacher(teacherId: string, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);

    return this._http.get<ApiResponse<ClassDto[]>>(`${this._url}/by-teacher/${teacherId}`, { params }).pipe(
      catchError(this.handleError<ClassDto[]>('getByTeacher'))
    );
  }

  /**
   * Create new class
   */
  create(payload: CreateClassRequest): Observable<ApiResponse<ClassDto>> {
    return this._http.post<ApiResponse<ClassDto>>(this._url, payload).pipe(
      catchError(this.handleError<ClassDto>('create'))
    );
  }

  /**
   * Update existing class
   */
  update(id: string, payload: UpdateClassRequest): Observable<ApiResponse<ClassDto>> {
    return this._http.put<ApiResponse<ClassDto>>(`${this._url}/${id}`, payload).pipe(
      catchError(this.handleError<ClassDto>('update'))
    );
  }

  /**
   * Delete class
   */
  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`).pipe(
      catchError(this.handleError<null>('delete'))
    );
  }

  // ==================== Utility Methods ====================

  /**
   * Get CBC level display name
   */
  getCBCLevelDisplay(level: number): string {
    return getCBCLevelDisplay(level);
  }

  /**
   * Get all CBC levels for dropdown
   */
  getAllCBCLevels(): { value: number; label: string }[] {
    return Object.entries(CBCLevel).map(([key, value]) => ({
      value,
      label: this.getCBCLevelDisplay(value)
    }));
  }

  /**
   * Generate class code from level and name
   */
  generateClassCode(level: number, name: string): string {
    const levelPrefix = this.getLevelPrefix(level);
    const cleanName = name
      .replace(/[^\w\s]/g, '')
      .trim()
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase();
    
    return `${levelPrefix}-${cleanName}`;
  }

  /**
   * Get level prefix for code generation
   */
  private getLevelPrefix(level: number): string {
    if (level === CBCLevel.PrePrimary1) return 'PP1';
    if (level === CBCLevel.PrePrimary2) return 'PP2';
    if (level >= CBCLevel.Grade1 && level <= CBCLevel.Grade6) {
      return `G${level - CBCLevel.Grade1 + 1}`;
    }
    if (level >= CBCLevel.JuniorSecondary1 && level <= CBCLevel.JuniorSecondary3) {
      return `JS${level - CBCLevel.JuniorSecondary1 + 1}`;
    }
    if (level >= CBCLevel.SeniorSecondary1 && level <= CBCLevel.SeniorSecondary3) {
      return `SS${level - CBCLevel.SeniorSecondary1 + 1}`;
    }
    return 'CLS';
  }

  /**
   * Calculate capacity utilization percentage
   */
  getCapacityUtilization(currentEnrollment: number, capacity: number): number {
    if (capacity === 0) return 0;
    return Math.round((currentEnrollment / capacity) * 100);
  }

  /**
   * Get capacity status color
   */
  getCapacityStatusColor(currentEnrollment: number, capacity: number): string {
    const utilization = this.getCapacityUtilization(currentEnrollment, capacity);
    
    if (utilization >= 100) return 'warn';
    if (utilization >= 90) return 'accent';
    return 'primary';
  }

  // ==================== Error Handling ====================

  private handleError<T>(operation = 'operation') {
    return (error: any): Observable<ApiResponse<T>> => {
      console.error(`${operation} failed:`, error);

      let errorMessage = 'An error occurred';
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.message) {
        errorMessage = error.message;
      } else if (error.status === 0) {
        errorMessage = 'Unable to connect to server';
      } else if (error.status === 401) {
        errorMessage = 'Unauthorized access';
      } else if (error.status === 403) {
        errorMessage = 'Forbidden - insufficient permissions';
      } else if (error.status === 404) {
        errorMessage = 'Resource not found';
      } else if (error.status >= 500) {
        errorMessage = 'Server error occurred';
      }

       return of({
        success: false,
        message: errorMessage,
        data: null as unknown as T,
        errors: error.error?.errors || [errorMessage]
      } as ApiResponse<T>);
    };
  }
}