import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import {
  BookPublisherResponseDto,
  CreateBookPublisherDto,
  UpdateBookPublisherDto,
} from 'app/Library/book-publisher/Types/book-publisher.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class BookPublisherService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base = `${this.apiBase}/api/library/bookpublishers`;

  constructor(private http: HttpClient) {}

  getAll(schoolId?: string): Observable<ApiResponse<BookPublisherResponseDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<BookPublisherResponseDto[]>>(this.base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookPublisherResponseDto>> {
    return this.http.get<ApiResponse<BookPublisherResponseDto>>(`${this.base}/${id}`);
  }

  create(dto: CreateBookPublisherDto): Observable<ApiResponse<BookPublisherResponseDto>> {
    return this.http.post<ApiResponse<BookPublisherResponseDto>>(this.base, dto);
  }

  update(id: string, dto: UpdateBookPublisherDto): Observable<ApiResponse<BookPublisherResponseDto>> {
    return this.http.put<ApiResponse<BookPublisherResponseDto>>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.base}/${id}`);
  }
}