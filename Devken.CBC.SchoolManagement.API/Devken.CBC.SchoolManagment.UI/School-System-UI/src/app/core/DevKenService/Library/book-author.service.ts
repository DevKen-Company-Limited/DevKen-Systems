import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import {
  BookAuthorResponseDto,
  CreateBookAuthorDto,
  UpdateBookAuthorDto,
} from 'app/Library/book-author/Types/book-author.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class BookAuthorService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base = `${this.apiBase}/api/library/bookauthors`;

  constructor(private http: HttpClient) {}

  getAll(schoolId?: string): Observable<ApiResponse<BookAuthorResponseDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<BookAuthorResponseDto[]>>(this.base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookAuthorResponseDto>> {
    return this.http.get<ApiResponse<BookAuthorResponseDto>>(`${this.base}/${id}`);
  }

  create(dto: CreateBookAuthorDto): Observable<ApiResponse<BookAuthorResponseDto>> {
    return this.http.post<ApiResponse<BookAuthorResponseDto>>(this.base, dto);
  }

  update(id: string, dto: UpdateBookAuthorDto): Observable<ApiResponse<BookAuthorResponseDto>> {
    return this.http.put<ApiResponse<BookAuthorResponseDto>>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.base}/${id}`);
  }
}