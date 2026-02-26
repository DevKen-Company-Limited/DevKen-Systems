import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { CBCLevel } from 'app/Classes/Types/Class';
import { FeeStructureDto, CreateFeeStructureDto, UpdateFeeStructureDto } from 'app/finance/fee-structure/types/fee-structure.model';


export interface ApiResponse<T> {
  success:  boolean;
  message:  string;
  data:     T;
  errors?:  Record<string, string[]>;
  statusCode?: number;
}

@Injectable({ providedIn: 'root' })
export class FeeStructureService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base    = `${this.apiBase}/api/finance/feestructures`;

  constructor(private http: HttpClient) {}

  // ── READ ──────────────────────────────────────────────────────────────────

  /** Fetch all fee structures (optionally filtered by schoolId for SuperAdmin). */
  getAll(filters?: { schoolId?: string }): Observable<ApiResponse<FeeStructureDto[]>> {
    let params = new HttpParams();
    if (filters?.schoolId) params = params.set('schoolId', filters.schoolId);
    return this.http.get<ApiResponse<FeeStructureDto[]>>(this.base, { params });
  }

  /** Fetch a single fee structure by primary key. */
  getById(id: string): Observable<ApiResponse<FeeStructureDto>> {
    return this.http.get<ApiResponse<FeeStructureDto>>(`${this.base}/${id}`);
  }

  /** All fee structures for a specific FeeItem. */
  getByFeeItem(feeItemId: string): Observable<ApiResponse<FeeStructureDto[]>> {
    return this.http.get<ApiResponse<FeeStructureDto[]>>(
      `${this.base}/by-fee-item/${feeItemId}`
    );
  }

  /** All fee structures for a given academic year. */
  getByAcademicYear(academicYearId: string): Observable<ApiResponse<FeeStructureDto[]>> {
    return this.http.get<ApiResponse<FeeStructureDto[]>>(
      `${this.base}/by-academic-year/${academicYearId}`
    );
  }

  /** All fee structures for a given term. */
  getByTerm(termId: string): Observable<ApiResponse<FeeStructureDto[]>> {
    return this.http.get<ApiResponse<FeeStructureDto[]>>(
      `${this.base}/by-term/${termId}`
    );
  }

  /** All fee structures applicable to a specific CBC level (+ all-level records). */
  getByLevel(level: CBCLevel): Observable<ApiResponse<FeeStructureDto[]>> {
    return this.http.get<ApiResponse<FeeStructureDto[]>>(
      `${this.base}/by-level/${level}`
    );
  }

  // ── WRITE ─────────────────────────────────────────────────────────────────

  /** Create a new fee structure. */
  create(dto: CreateFeeStructureDto): Observable<ApiResponse<FeeStructureDto>> {
    return this.http.post<ApiResponse<FeeStructureDto>>(this.base, dto);
  }

  /** Update mutable fields on an existing fee structure. */
  update(id: string, dto: UpdateFeeStructureDto): Observable<ApiResponse<FeeStructureDto>> {
    return this.http.put<ApiResponse<FeeStructureDto>>(`${this.base}/${id}`, dto);
  }

  /** Toggle the IsActive flag. */
  toggleActive(id: string): Observable<ApiResponse<FeeStructureDto>> {
    return this.http.patch<ApiResponse<FeeStructureDto>>(
      `${this.base}/${id}/toggle-active`,
      null
    );
  }

  /** Hard-delete a fee structure. */
  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.base}/${id}`);
  }
}