// import { inject, Injectable } from '@angular/core';
// import { HttpClient, HttpParams } from '@angular/common/http';
// import { Observable } from 'rxjs';
// import { API_BASE_URL } from 'app/app.config';
// import {
//   ClassDto,
//   ClassDetailDto,
//   CreateClassRequest,
//   UpdateClassRequest,
//   ApiResponse,
//   CBCLevel,
//   getAllCBCLevels,
//   getCBCLevelDisplay
// } from 'app/Classes/Types/Class';




import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import {
  ClassDto,
  ClassDetailDto,
  CreateClassRequest,
  UpdateClassRequest,
  ApiResponse,
  CBCLevel,
  getAllCBCLevels,
  getCBCLevelDisplay
} from 'app/Classes/Types/Class';
@Injectable({ providedIn: 'root' })
export class ClassService {
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/class`;
  private http = inject(HttpClient);

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

    return this.http.get<ApiResponse<ClassDto[]>>(this.baseUrl, { params });
  }

  /**
   * Get class by ID with optional details
   */
  getById(id: string, includeDetails: boolean = false): Observable<ApiResponse<ClassDto | ClassDetailDto>> {
    let params = new HttpParams();
    if (includeDetails) {
      params = params.set('includeDetails', 'true');
    }
    return this.http.get<ApiResponse<ClassDto | ClassDetailDto>>(`${this.baseUrl}/${id}`, { params });
  }

  /**
   * Get classes by academic year
   */
  getByAcademicYear(academicYearId: string, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<ClassDto[]>>(`${this.baseUrl}/by-academic-year/${academicYearId}`, { params });
  }

  /**
   * Get classes by CBC level
   */
  getByLevel(level: number, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<ClassDto[]>>(`${this.baseUrl}/by-level/${level}`, { params });
  }

  /**
   * Get classes by teacher
   */
  getByTeacher(teacherId: string, schoolId?: string): Observable<ApiResponse<ClassDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<ClassDto[]>>(`${this.baseUrl}/by-teacher/${teacherId}`, { params });
  }

  /**
   * Create new class
   */
  create(request: CreateClassRequest): Observable<ApiResponse<ClassDto>> {
    return this.http.post<ApiResponse<ClassDto>>(this.baseUrl, request);
  }

  /**
   * Update class
   */
  update(id: string, request: UpdateClassRequest): Observable<ApiResponse<ClassDto>> {
    return this.http.put<ApiResponse<ClassDto>>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Delete class
   */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Preview next code that will be generated from number series
   */
  previewNextCode(schoolId?: string): Observable<ApiResponse<{ nextCode: string }>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this.http.get<ApiResponse<{ nextCode: string }>>(`${this.baseUrl}/preview-next-code`, { params });
  }

  // ==================== Helper Methods ====================

  /**
   * Get all CBC levels
   */
  getAllCBCLevels(): { value: number; label: string }[] {
    return getAllCBCLevels();
  }

  /**
   * Get CBC level display name
   */
  getCBCLevelDisplay(level: number): string {
    return getCBCLevelDisplay(level);
  }

  /**
   * Generate class code from level and name (fallback for manual generation)
   */
  generateClassCode(level: number, name: string): string {
    const levelPrefix = this.getLevelPrefix(level);
    const namePart = name
      .trim()
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, '')
      .substring(0, 4);
    return `${levelPrefix}-${namePart}`;
  }

  /**
   * Get capacity utilization percentage
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
    if (utilization >= 90) return 'red';
    if (utilization >= 70) return 'amber';
    return 'green';
  }

  /**
   * Get level prefix for code generation
   */
  private getLevelPrefix(level: number): string {
    const prefixMap: { [key: number]: string } = {
      [CBCLevel.PrePrimary1]: 'PP1',
      [CBCLevel.PrePrimary2]: 'PP2',
      [CBCLevel.Grade1]: 'G1',
      [CBCLevel.Grade2]: 'G2',
      [CBCLevel.Grade3]: 'G3',
      [CBCLevel.Grade4]: 'G4',
      [CBCLevel.Grade5]: 'G5',
      [CBCLevel.Grade6]: 'G6',
      [CBCLevel.JuniorSecondary1]: 'JS1',
      [CBCLevel.JuniorSecondary2]: 'JS2',
      [CBCLevel.JuniorSecondary3]: 'JS3',
      [CBCLevel.SeniorSecondary1]: 'SS1',
      [CBCLevel.SeniorSecondary2]: 'SS2',
      [CBCLevel.SeniorSecondary3]: 'SS3'
    };
    return prefixMap[level] || `L${level}`;
  }
}