// core/services/student.service.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { StudentDto } from '../../Types/StudentDto';

@Injectable({ providedIn: 'root' })
export class StudentService {

  private readonly apiBase = inject(API_BASE_URL);
  private readonly base = `${this.apiBase}/api/academic/students`;

  constructor(private http: HttpClient) {}

  // ── CRUD ─────────────────────────────────────────────────────────────────
  getAll(schoolId?: string): Observable<StudentDto[]> {
    let params = new HttpParams();
    if (schoolId) {
      params = params.set('schoolId', schoolId);
    }

    return this.http.get<any>(this.base, { params }).pipe(
      map(r => r?.data ?? r ?? [])
    );
  }

  getById(id: string): Observable<StudentDto> {
    return this.http.get<any>(`${this.base}/${id}`).pipe(
      map(r => r?.data ?? r)
    );
  }

  create(payload: Partial<StudentDto>): Observable<StudentDto> {
    return this.http.post<any>(this.base, payload).pipe(
      map(r => r?.data ?? r)
    );
  }

  update(id: string, payload: Partial<StudentDto>): Observable<StudentDto> {
    return this.http.put<any>(`${this.base}/${id}`, payload).pipe(
      map(r => r?.data ?? r)
    );
  }

  /** Partial update — same endpoint as full update but sends only changed fields */
  updatePartial(id: string, payload: Partial<StudentDto>): Observable<StudentDto> {
    return this.update(id, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  // ── Lookup helpers ────────────────────────────────────────────────────────
  getSchools(): Observable<any[]> {
    return this.http.get<any>(`${this.apiBase}/api/administration/schools`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getClasses(): Observable<any[]> {
    return this.http.get<any>(`${this.apiBase}/api/academic/classes`).pipe(
      map(r => r?.data ?? [])
    );
  }

  getAcademicYears(): Observable<any[]> {
    return this.http.get<any>(`${this.apiBase}/api/academic/academic-years`).pipe(
      map(r => r?.data ?? [])
    );
  }
}
