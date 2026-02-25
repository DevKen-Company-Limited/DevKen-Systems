import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { CreateStrandDto, StrandResponseDto, UpdateStrandDto } from 'app/curriculum/types/strand.dto ';

@Injectable({ providedIn: 'root' })
export class StrandService {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  private get url(): string {
    return `${this.baseUrl}/api/curriculum/strands`;
  }

  getAll(schoolId?: string | null, learningAreaId?: string): Observable<StrandResponseDto[]> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    if (learningAreaId) params = params.set('learningAreaId', learningAreaId);
    return this.http.get<any>(this.url, { params }).pipe(map(r => r?.data ?? r ?? []));
  }

  getById(id: string): Observable<StrandResponseDto> {
    return this.http.get<any>(`${this.url}/${id}`).pipe(map(r => r?.data ?? r));
  }

  create(dto: CreateStrandDto): Observable<StrandResponseDto> {
    return this.http.post<any>(this.url, dto).pipe(map(r => r?.data ?? r));
  }

  update(id: string, dto: UpdateStrandDto): Observable<StrandResponseDto> {
    return this.http.put<any>(`${this.url}/${id}`, dto).pipe(map(r => r?.data ?? r));
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.url}/${id}`);
  }
}