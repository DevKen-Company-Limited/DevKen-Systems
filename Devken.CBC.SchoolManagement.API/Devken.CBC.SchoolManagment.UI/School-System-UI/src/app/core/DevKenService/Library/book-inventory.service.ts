import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';
import { CreateBookInventoryRequest, UpdateBookInventoryRequest, BookInventoryDto } from 'app/Library/book-inventory/Types/book-inventory.types';


@Injectable({ providedIn: 'root' })
export class BookInventoryService
  implements
    ICrudService<CreateBookInventoryRequest, UpdateBookInventoryRequest, BookInventoryDto>,
    IListService<BookInventoryDto> {

  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/bookinventory`;

  getAll(schoolId?: string): Observable<ApiResponse<BookInventoryDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookInventoryDto[]>>(this._base, { params });
  }

  getByBook(bookId: string): Observable<ApiResponse<BookInventoryDto>> {
    return this._http.get<ApiResponse<BookInventoryDto>>(`${this._base}/book/${bookId}`);
  }

  getById(id: string): Observable<ApiResponse<BookInventoryDto>> {
    return this._http.get<ApiResponse<BookInventoryDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateBookInventoryRequest): Observable<ApiResponse<BookInventoryDto>> {
    return this._http.post<ApiResponse<BookInventoryDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookInventoryRequest): Observable<ApiResponse<BookInventoryDto>> {
    return this._http.put<ApiResponse<BookInventoryDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }

  recalculate(bookId: string): Observable<ApiResponse<BookInventoryDto>> {
    return this._http.post<ApiResponse<BookInventoryDto>>(
      `${this._base}/book/${bookId}/recalculate`, {});
  }
}