import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  LibraryMemberDto,
  CreateLibraryMemberRequest,
  UpdateLibraryMemberRequest,
} from 'app/Library/library-member/Types/library-member.types';

@Injectable({ providedIn: 'root' })
export class LibraryMemberService {
  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/librarymembers`;

  getAll(schoolId?: string): Observable<ApiResponse<LibraryMemberDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibraryMemberDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<LibraryMemberDto>> {
    return this._http.get<ApiResponse<LibraryMemberDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateLibraryMemberRequest): Observable<ApiResponse<LibraryMemberDto>> {
    return this._http.post<ApiResponse<LibraryMemberDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateLibraryMemberRequest): Observable<ApiResponse<LibraryMemberDto>> {
    return this._http.put<ApiResponse<LibraryMemberDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }
}