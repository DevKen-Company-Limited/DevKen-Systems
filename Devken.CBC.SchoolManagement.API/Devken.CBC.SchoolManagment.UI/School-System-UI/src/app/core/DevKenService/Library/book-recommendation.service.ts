// book-recommendation/book-recommendation.service.ts

import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from 'app/Tenant/types/school';
import { BookRecommendationDto, CreateBookRecommendationRequest, GenerateRecommendationsRequest, UpdateBookRecommendationRequest } from 'app/Library/book-recommendation/Types/book-recommendation.types';
import { API_BASE_URL } from 'app/app.config';


@Injectable({ providedIn: 'root' })
export class BookRecommendationService {
   private readonly apiBase = inject(API_BASE_URL);
        private readonly _base = `${this.apiBase}/api/library/BookRecommendations`;
  constructor(private _http: HttpClient) {}

  // ── Standard CRUD ─────────────────────────────────────────────────────────

  getAll(schoolId?: string): Observable<ApiResponse<BookRecommendationDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookRecommendationDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookRecommendationDto>> {
    return this._http.get<ApiResponse<BookRecommendationDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateBookRecommendationRequest): Observable<ApiResponse<BookRecommendationDto>> {
    return this._http.post<ApiResponse<BookRecommendationDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookRecommendationRequest): Observable<ApiResponse<BookRecommendationDto>> {
    return this._http.put<ApiResponse<BookRecommendationDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`);
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  getByStudent(studentId: string, schoolId?: string): Observable<ApiResponse<BookRecommendationDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookRecommendationDto[]>>(`${this._base}/student/${studentId}`, { params });
  }

  getByBook(bookId: string, schoolId?: string): Observable<ApiResponse<BookRecommendationDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookRecommendationDto[]>>(`${this._base}/book/${bookId}`, { params });
  }

  getTopRecommendations(studentId: string, topN: number = 10, schoolId?: string): Observable<ApiResponse<BookRecommendationDto[]>> {
    let params = new HttpParams().set('topN', topN.toString());
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookRecommendationDto[]>>(`${this._base}/student/${studentId}/top`, { params });
  }

  // ── Batch Operations ──────────────────────────────────────────────────────

  deleteByStudent(studentId: string, schoolId?: string): Observable<ApiResponse<void>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.delete<ApiResponse<void>>(`${this._base}/student/${studentId}`, { params });
  }

  deleteByBook(bookId: string, schoolId?: string): Observable<ApiResponse<void>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.delete<ApiResponse<void>>(`${this._base}/book/${bookId}`, { params });
  }

  // ── AI Generation ─────────────────────────────────────────────────────────

  generateRecommendations(payload: GenerateRecommendationsRequest): Observable<ApiResponse<BookRecommendationDto[]>> {
    return this._http.post<ApiResponse<BookRecommendationDto[]>>(`${this._base}/generate`, payload);
  }
}