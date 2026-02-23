import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';import { CBCLevel } from 'app/curriculum/types/curriculum-enums';
import { CreateLearningAreaDto, LearningAreaResponseDto, UpdateLearningAreaDto } from 'app/curriculum/types/learning-area.dto ';


@Injectable({ providedIn: 'root' })
export class LearningAreaService {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  private get url(): string {
    return `${this.baseUrl}/api/curriculum/learningareas`;
  }

  getAll(schoolId?: string | null, level?: CBCLevel): Observable<LearningAreaResponseDto[]> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    if (level != null) params = params.set('level', level.toString());
    return this.http.get<any>(this.url, { params }).pipe(
      map(res => res?.data ?? res ?? [])
    );
  }

  getById(id: string): Observable<LearningAreaResponseDto> {
    return this.http.get<any>(`${this.url}/${id}`).pipe(map(r => r?.data ?? r));
  }

  getByCode(code: string): Observable<LearningAreaResponseDto> {
    return this.http.get<any>(`${this.url}/by-code/${code}`).pipe(map(r => r?.data ?? r));
  }

  create(dto: CreateLearningAreaDto): Observable<LearningAreaResponseDto> {
    return this.http.post<any>(this.url, dto).pipe(map(r => r?.data ?? r));
  }

  update(id: string, dto: UpdateLearningAreaDto): Observable<LearningAreaResponseDto> {
    return this.http.put<any>(`${this.url}/${id}`, dto).pipe(map(r => r?.data ?? r));
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.url}/${id}`);
  }
}