// app/core/DevKenService/Library/library-settings.service.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  LibrarySettingsDto,
  UpsertLibrarySettingsRequest,
} from 'app/Library/library-settings/Types/library-settings.types';

@Injectable({ providedIn: 'root' })
export class LibrarySettingsService {
  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/librarysettings`;

  get(schoolId?: string): Observable<ApiResponse<LibrarySettingsDto>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibrarySettingsDto>>(this._base, { params });
  }

  upsert(payload: UpsertLibrarySettingsRequest): Observable<ApiResponse<LibrarySettingsDto>> {
    return this._http.put<ApiResponse<LibrarySettingsDto>>(this._base, payload);
  }
}