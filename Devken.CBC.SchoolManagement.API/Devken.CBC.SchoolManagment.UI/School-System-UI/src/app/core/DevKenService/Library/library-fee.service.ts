// app/core/DevKenService/Library/library-fee.service.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  LibraryFeeDto,
  CreateLibraryFeeRequest,
  UpdateLibraryFeeRequest,
  RecordLibraryFeePaymentRequest,
  WaiveLibraryFeeRequest,
  LibraryFeeFilterRequest,
} from 'app/Library/library-fee/Types/library-fee.types';

@Injectable({ providedIn: 'root' })
export class LibraryFeeService {
  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/libraryfees`;

  getAll(schoolId?: string): Observable<ApiResponse<LibraryFeeDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibraryFeeDto[]>>(this._base, { params });
  }

  getFiltered(filter: LibraryFeeFilterRequest): Observable<ApiResponse<LibraryFeeDto[]>> {
    let params = new HttpParams();
    if (filter.schoolId)  params = params.set('schoolId',  filter.schoolId);
    if (filter.memberId)  params = params.set('memberId',  filter.memberId);
    if (filter.feeStatus) params = params.set('feeStatus', filter.feeStatus);
    if (filter.feeType)   params = params.set('feeType',   filter.feeType);
    if (filter.fromDate)  params = params.set('fromDate',  filter.fromDate);
    if (filter.toDate)    params = params.set('toDate',    filter.toDate);
    return this._http.get<ApiResponse<LibraryFeeDto[]>>(`${this._base}/filter`, { params });
  }

  getByMember(memberId: string): Observable<ApiResponse<LibraryFeeDto[]>> {
    return this._http.get<ApiResponse<LibraryFeeDto[]>>(
      `${this._base}/member/${memberId}`
    );
  }

  getOutstandingBalance(memberId: string): Observable<ApiResponse<{ memberId: string; outstandingBalance: number }>> {
    return this._http.get<ApiResponse<{ memberId: string; outstandingBalance: number }>>(
      `${this._base}/member/${memberId}/balance`
    );
  }

  getById(id: string): Observable<ApiResponse<LibraryFeeDto>> {
    return this._http.get<ApiResponse<LibraryFeeDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateLibraryFeeRequest): Observable<ApiResponse<LibraryFeeDto>> {
    return this._http.post<ApiResponse<LibraryFeeDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateLibraryFeeRequest): Observable<ApiResponse<LibraryFeeDto>> {
    return this._http.put<ApiResponse<LibraryFeeDto>>(`${this._base}/${id}`, payload);
  }

  recordPayment(id: string, payload: RecordLibraryFeePaymentRequest): Observable<ApiResponse<LibraryFeeDto>> {
    return this._http.post<ApiResponse<LibraryFeeDto>>(`${this._base}/${id}/pay`, payload);
  }

  waive(id: string, payload: WaiveLibraryFeeRequest): Observable<ApiResponse<LibraryFeeDto>> {
    return this._http.post<ApiResponse<LibraryFeeDto>>(`${this._base}/${id}/waive`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }
}