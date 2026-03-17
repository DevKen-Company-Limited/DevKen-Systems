import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';
import { BookDto, CreateBookRequest, UpdateBookRequest } from 'app/Library/book/Types/book.types';

@Injectable({ providedIn: 'root' })
export class BookService
  implements
    ICrudService<CreateBookRequest, UpdateBookRequest, BookDto>,
    IListService<BookDto> {

  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/books`;

  getAll(schoolId?: string): Observable<ApiResponse<BookDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookDto[]>>(this._base, { params });
  }

  getByCategory(categoryId: string): Observable<ApiResponse<BookDto[]>> {
    return this._http.get<ApiResponse<BookDto[]>>(`${this._base}/category/${categoryId}`);
  }

  getByAuthor(authorId: string): Observable<ApiResponse<BookDto[]>> {
    return this._http.get<ApiResponse<BookDto[]>>(`${this._base}/author/${authorId}`);
  }

  getById(id: string): Observable<ApiResponse<BookDto>> {
    return this._http.get<ApiResponse<BookDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateBookRequest): Observable<ApiResponse<BookDto>> {
    return this._http.post<ApiResponse<BookDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookRequest): Observable<ApiResponse<BookDto>> {
    return this._http.put<ApiResponse<BookDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }
}