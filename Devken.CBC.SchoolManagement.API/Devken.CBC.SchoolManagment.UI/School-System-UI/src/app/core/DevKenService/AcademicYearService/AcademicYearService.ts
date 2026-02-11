import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable } from 'rxjs';
import { AcademicYearDto, CreateAcademicYearRequest, UpdateAcademicYearRequest } from 'app/AcademicYear/Types/AcademicYear';
import { ApiResponse } from '../Types/roles';

@Injectable({
  providedIn: 'root'
})
export class AcademicYearService {
  private _http = inject(HttpClient);
  private _apiBase = inject(API_BASE_URL);
  private _url = `${this._apiBase}/api/academic/academicyear`;

  getAll(schoolId?: string): Observable<ApiResponse<AcademicYearDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this._http.get<ApiResponse<AcademicYearDto[]>>(this._url, { params });
  }

  getById(id: string): Observable<ApiResponse<AcademicYearDto>> {
    return this._http.get<ApiResponse<AcademicYearDto>>(`${this._url}/${id}`);
  }

  getCurrent(schoolId?: string): Observable<ApiResponse<AcademicYearDto>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this._http.get<ApiResponse<AcademicYearDto>>(`${this._url}/current`, { params });
  }

  getOpen(schoolId?: string): Observable<ApiResponse<AcademicYearDto[]>> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }
    return this._http.get<ApiResponse<AcademicYearDto[]>>(`${this._url}/open`, { params });
  }

  create(payload: CreateAcademicYearRequest): Observable<ApiResponse<AcademicYearDto>> {
    return this._http.post<ApiResponse<AcademicYearDto>>(this._url, payload);
  }

  update(id: string, payload: UpdateAcademicYearRequest): Observable<ApiResponse<AcademicYearDto>> {
    return this._http.put<ApiResponse<AcademicYearDto>>(`${this._url}/${id}`, payload);
  }

  setAsCurrent(id: string): Observable<ApiResponse<string>> {
    return this._http.put<ApiResponse<string>>(`${this._url}/${id}/set-current`, {});
  }

  close(id: string): Observable<ApiResponse<string>> {
    return this._http.put<ApiResponse<string>>(`${this._url}/${id}/close`, {});
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`);
  }
}