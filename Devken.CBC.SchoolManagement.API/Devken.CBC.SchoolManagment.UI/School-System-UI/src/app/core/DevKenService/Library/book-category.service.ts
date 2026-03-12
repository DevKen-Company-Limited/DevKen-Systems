import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import {
  BookCategoryResponseDto,
  CreateBookCategoryDto,
  UpdateBookCategoryDto,
} from 'app/Library/book-category/Types/book-category.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class BookCategoryService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base = `${this.apiBase}/api/library/bookcategories`;

  constructor(private http: HttpClient) {}

  getAll(schoolId?: string): Observable<ApiResponse<BookCategoryResponseDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<BookCategoryResponseDto[]>>(this.base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookCategoryResponseDto>> {
    return this.http.get<ApiResponse<BookCategoryResponseDto>>(`${this.base}/${id}`);
  }

  create(dto: CreateBookCategoryDto): Observable<ApiResponse<BookCategoryResponseDto>> {
    return this.http.post<ApiResponse<BookCategoryResponseDto>>(this.base, dto);
  }

  update(id: string, dto: UpdateBookCategoryDto): Observable<ApiResponse<BookCategoryResponseDto>> {
    return this.http.put<ApiResponse<BookCategoryResponseDto>>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.base}/${id}`);
  }
}