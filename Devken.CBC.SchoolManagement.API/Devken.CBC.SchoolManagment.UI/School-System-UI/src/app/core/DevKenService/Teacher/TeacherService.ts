import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { TeacherDto, CreateTeacherRequest, UpdateTeacherRequest } from '../Types/Teacher';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class TeacherService {
  private baseUrl = `${inject(API_BASE_URL)}/api/academic/teachers`;
  private http = inject(HttpClient);

  getAll(schoolId?: string): Observable<ApiResponse<TeacherDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this.http.get<ApiResponse<TeacherDto[]>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<TeacherDto>> {
    return this.http.get<ApiResponse<TeacherDto>>(`${this.baseUrl}/${id}`);
  }

create(request: CreateTeacherRequest): Observable<ApiResponse<TeacherDto>> {
  return this.http.post<ApiResponse<TeacherDto>>(this.baseUrl, request);
}

update(id: string, request: UpdateTeacherRequest): Observable<ApiResponse<TeacherDto>> {
  return this.http.put<ApiResponse<TeacherDto>>(`${this.baseUrl}/${id}`, request);
}


  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  uploadPhoto(id: string, file: File): Observable<ApiResponse<{ photoUrl: string }>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<{ photoUrl: string }>>(`${this.baseUrl}/${id}/photo`, formData);
  }

  deletePhoto(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}/photo`);
  }
}