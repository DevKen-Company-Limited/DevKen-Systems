import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  BookReservationDto,
  CreateBookReservationRequest,
  UpdateBookReservationRequest,
} from 'app/Library/book-reservation/Types/book-reservation.types';

@Injectable({ providedIn: 'root' })
export class BookReservationService {
  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/bookreservations`;

  getAll(schoolId?: string): Observable<ApiResponse<BookReservationDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookReservationDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookReservationDto>> {
    return this._http.get<ApiResponse<BookReservationDto>>(`${this._base}/${id}`);
  }

  getByBook(bookId: string): Observable<ApiResponse<BookReservationDto[]>> {
    return this._http.get<ApiResponse<BookReservationDto[]>>(
      `${this._base}/by-book/${bookId}`
    );
  }

  getByMember(memberId: string): Observable<ApiResponse<BookReservationDto[]>> {
    return this._http.get<ApiResponse<BookReservationDto[]>>(
      `${this._base}/by-member/${memberId}`
    );
  }

  create(payload: CreateBookReservationRequest): Observable<ApiResponse<BookReservationDto>> {
    return this._http.post<ApiResponse<BookReservationDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookReservationRequest): Observable<ApiResponse<BookReservationDto>> {
    return this._http.put<ApiResponse<BookReservationDto>>(`${this._base}/${id}`, payload);
  }

  fulfill(id: string): Observable<ApiResponse<BookReservationDto>> {
    return this._http.patch<ApiResponse<BookReservationDto>>(
      `${this._base}/${id}/fulfill`, {}
    );
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }
}