import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from 'app/Tenant/types/school';
import { CreateLibraryFineRequest, LibraryFineDto, PayFineRequest, PayMultipleFinesRequest, WaiveFineRequest } from 'app/Library/library-fines/Types/library-fine.types';
import { API_BASE_URL } from 'app/app.config';



@Injectable({ providedIn: 'root' })
export class LibraryFineService {
  private readonly apiBase = inject(API_BASE_URL);
      private readonly _base = `${this.apiBase}/api/library/fines`;

  constructor(private _http: HttpClient) {}

  // ── Standard CRUD ─────────────────────────────────────────────────────────

  getAll(schoolId?: string): Observable<ApiResponse<LibraryFineDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibraryFineDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<LibraryFineDto>> {
    return this._http.get<ApiResponse<LibraryFineDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateLibraryFineRequest): Observable<ApiResponse<LibraryFineDto>> {
    return this._http.post<ApiResponse<LibraryFineDto>>(this._base, payload);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`);
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  getUnpaid(schoolId?: string): Observable<ApiResponse<LibraryFineDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibraryFineDto[]>>(`${this._base}/unpaid`, { params });
  }

  getByMember(memberId: string): Observable<ApiResponse<LibraryFineDto[]>> {
    return this._http.get<ApiResponse<LibraryFineDto[]>>(`${this._base}/member/${memberId}`);
  }

  getTotalUnpaid(memberId: string): Observable<ApiResponse<{ totalUnpaidFines: number }>> {
    return this._http.get<ApiResponse<{ totalUnpaidFines: number }>>(`${this._base}/member/${memberId}/unpaid-total`);
  }

  getTotalPaid(memberId: string): Observable<ApiResponse<{ totalPaidFines: number }>> {
    return this._http.get<ApiResponse<{ totalPaidFines: number }>>(`${this._base}/member/${memberId}/paid-total`);
  }

  // ── Payment / Waive ───────────────────────────────────────────────────────

  payFine(payload: PayFineRequest): Observable<ApiResponse<LibraryFineDto>> {
    return this._http.post<ApiResponse<LibraryFineDto>>(`${this._base}/pay`, payload);
  }

  payMultiple(payload: PayMultipleFinesRequest): Observable<ApiResponse<LibraryFineDto[]>> {
    return this._http.post<ApiResponse<LibraryFineDto[]>>(`${this._base}/pay/multiple`, payload);
  }

  waiveFine(payload: WaiveFineRequest): Observable<ApiResponse<void>> {
    return this._http.post<ApiResponse<void>>(`${this._base}/waive`, payload);
  }
}