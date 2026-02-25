import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { LearningOutcomeResponseDto, CreateLearningOutcomeDto, UpdateLearningOutcomeDto } from 'app/curriculum/types/learning-outcome.dto';
import { CBCLevel } from 'app/curriculum/types/curriculum-enums';

@Injectable({ providedIn: 'root' })
export class LearningOutcomeService {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  private get url(): string {
    return `${this.baseUrl}/api/curriculum/learningoutcomes`;
  }

  getAll(opts?: {
    schoolId?: string | null;
    subStrandId?: string;
    strandId?: string;
    learningAreaId?: string;
    level?: CBCLevel;
    isCore?: boolean;
  }): Observable<LearningOutcomeResponseDto[]> {
    let params = new HttpParams();
    if (opts?.schoolId) params = params.set('schoolId', opts.schoolId);
    if (opts?.subStrandId) params = params.set('subStrandId', opts.subStrandId);
    if (opts?.strandId) params = params.set('strandId', opts.strandId);
    if (opts?.learningAreaId) params = params.set('learningAreaId', opts.learningAreaId);
    if (opts?.level != null) params = params.set('level', opts.level.toString());
    if (opts?.isCore != null) params = params.set('isCore', opts.isCore.toString());
    return this.http.get<any>(this.url, { params }).pipe(map(r => r?.data ?? r ?? []));
  }

  getById(id: string): Observable<LearningOutcomeResponseDto> {
    return this.http.get<any>(`${this.url}/${id}`).pipe(map(r => r?.data ?? r));
  }

  getByCode(code: string): Observable<LearningOutcomeResponseDto> {
    return this.http.get<any>(`${this.url}/by-code/${code}`).pipe(map(r => r?.data ?? r));
  }

  create(dto: CreateLearningOutcomeDto): Observable<LearningOutcomeResponseDto> {
    return this.http.post<any>(this.url, dto).pipe(map(r => r?.data ?? r));
  }

  update(id: string, dto: UpdateLearningOutcomeDto): Observable<LearningOutcomeResponseDto> {
    return this.http.put<any>(`${this.url}/${id}`, dto).pipe(map(r => r?.data ?? r));
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.url}/${id}`);
  }
}