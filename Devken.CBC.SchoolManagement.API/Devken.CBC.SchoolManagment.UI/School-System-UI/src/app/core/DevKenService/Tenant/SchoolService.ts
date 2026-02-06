import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { ApiResponse, CreateSchoolRequest, SchoolDto, UpdateSchoolRequest } from 'app/Tenant/types/school';

@Injectable({
  providedIn: 'root'
})
export class SchoolService {
  private _http = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url = `${this._apiBase}/api/schools`;

  getAll(): Observable<ApiResponse<SchoolDto[]>> {
    return this._http.get<ApiResponse<SchoolDto[]>>(`${this._url}`);
  }

  getById(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(`${this._url}/${id}`);
  }

  create(payload: CreateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.post<ApiResponse<SchoolDto>>(`${this._url}`, payload);
  }

  update(id: string, payload: UpdateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.put<ApiResponse<SchoolDto>>(`${this._url}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`);
  }

  // Optional: fetch by slug if your backend exposes it
  getBySlug(slug: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(`${this._url}/slug/${encodeURIComponent(slug)}`);
  }
}