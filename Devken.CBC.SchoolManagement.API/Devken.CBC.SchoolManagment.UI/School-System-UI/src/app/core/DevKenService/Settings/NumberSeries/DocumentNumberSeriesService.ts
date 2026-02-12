import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { DocumentNumberSeriesDto, CreateDocumentNumberSeriesRequest, UpdateDocumentNumberSeriesRequest } from 'app/Settings/types/DocumentNumberSeries';


export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class DocumentNumberSeriesService {
  private baseUrl = `${inject(API_BASE_URL)}/api/document-number-series`;
  private http = inject(HttpClient);

  getAll(tenantId?: string): Observable<ApiResponse<DocumentNumberSeriesDto[]>> {
    let params = new HttpParams();
    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }
    return this.http.get<ApiResponse<DocumentNumberSeriesDto[]>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<DocumentNumberSeriesDto>> {
    return this.http.get<ApiResponse<DocumentNumberSeriesDto>>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateDocumentNumberSeriesRequest): Observable<ApiResponse<DocumentNumberSeriesDto>> {
    return this.http.post<ApiResponse<DocumentNumberSeriesDto>>(this.baseUrl, request);
  }

  update(id: string, request: UpdateDocumentNumberSeriesRequest): Observable<ApiResponse<DocumentNumberSeriesDto>> {
    return this.http.put<ApiResponse<DocumentNumberSeriesDto>>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}