import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';
import { CreateBookCopyRequest, UpdateBookCopyRequest, MarkBookCopyStatusRequest } from 'app/Library/book-copy/Types/book-copy.types';
import { BookCopyDto } from 'app/Library/book/Types/book.types';


@Injectable({ providedIn: 'root' })
export class BookCopyService
  implements
    ICrudService<CreateBookCopyRequest, UpdateBookCopyRequest, BookCopyDto>,
    IListService<BookCopyDto> {

  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/bookcopies`;

  getAll(schoolId?: string): Observable<ApiResponse<BookCopyDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookCopyDto[]>>(this._base, { params });
  }

  getByBook(bookId: string): Observable<ApiResponse<BookCopyDto[]>> {
    return this._http.get<ApiResponse<BookCopyDto[]>>(`${this._base}/book/${bookId}`);
  }

  getByBranch(branchId: string): Observable<ApiResponse<BookCopyDto[]>> {
    return this._http.get<ApiResponse<BookCopyDto[]>>(`${this._base}/branch/${branchId}`);
  }

  getById(id: string): Observable<ApiResponse<BookCopyDto>> {
    return this._http.get<ApiResponse<BookCopyDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateBookCopyRequest): Observable<ApiResponse<BookCopyDto>> {
    return this._http.post<ApiResponse<BookCopyDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookCopyRequest): Observable<ApiResponse<BookCopyDto>> {
    return this._http.put<ApiResponse<BookCopyDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }

  markLost(id: string, payload: MarkBookCopyStatusRequest): Observable<ApiResponse<BookCopyDto>> {
    return this._http.patch<ApiResponse<BookCopyDto>>(`${this._base}/${id}/mark-lost`, payload);
  }

  markDamaged(id: string, payload: MarkBookCopyStatusRequest): Observable<ApiResponse<BookCopyDto>> {
    return this._http.patch<ApiResponse<BookCopyDto>>(`${this._base}/${id}/mark-damaged`, payload);
  }

  markAvailable(id: string): Observable<ApiResponse<BookCopyDto>> {
    return this._http.patch<ApiResponse<BookCopyDto>>(`${this._base}/${id}/mark-available`, {});
  }
}