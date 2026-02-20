import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';

export interface UserActivityDto {
  schoolName: any;
  id: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  tenantId?: string;
  activityType: string;
  activityDetails: string;
  createdOn: string;
}

export interface ActivitySummaryDto {
  totalActivities: number;
  todayActivities: number;
  loginCount: number;
  uniqueUsers: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class UserActivityService {
  private readonly _http    = inject(HttpClient);
  private readonly _baseUrl = inject(API_BASE_URL);

  private get _url() { return `${this._baseUrl}/api/user-activity`; }

  getAll(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<UserActivityDto>>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this._http.get<ApiResponse<PagedResult<UserActivityDto>>>(this._url, { params });
  }

  getSummary(): Observable<ApiResponse<ActivitySummaryDto>> {
    return this._http.get<ApiResponse<ActivitySummaryDto>>(`${this._url}/summary`);
  }

  getByUser(
    userId: string, page = 1, pageSize = 20
  ): Observable<ApiResponse<PagedResult<UserActivityDto>>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this._http.get<ApiResponse<PagedResult<UserActivityDto>>>(
      `${this._url}/user/${userId}`, { params });
  }

  getByTenant(
    tenantId: string, page = 1, pageSize = 20
  ): Observable<ApiResponse<PagedResult<UserActivityDto>>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this._http.get<ApiResponse<PagedResult<UserActivityDto>>>(
      `${this._url}/tenant/${tenantId}`, { params });
  }
}