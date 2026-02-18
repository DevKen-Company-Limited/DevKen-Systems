// services/SubjectReportService.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

@Injectable({ providedIn: 'root' })
export class SubjectReportService {

  private readonly apiBase = inject(API_BASE_URL);
  private readonly base    = `${this.apiBase}/api/reports/subjectsreports`;

  constructor(private http: HttpClient) {}

  downloadSubjectsList(schoolId?: string | null): Observable<void> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);

    return this.http.get(`${this.base}/subjects-list`, {
      params,
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob      = response.body as Blob;
        const cd        = response.headers.get('Content-Disposition') ?? '';
        const match     = cd.match(/filename[^;=\n]*=(['"]?)([^'";\n]+)\1/);
        const filename  = match ? match[2] : `Subjects_List_${new Date().toISOString().slice(0,10)}.pdf`;
        const url       = URL.createObjectURL(blob);
        const a         = document.createElement('a');
        a.href          = url;
        a.download      = filename;
        a.click();
        URL.revokeObjectURL(url);
      }),
      catchError(err => throwError(() =>
        new Error(err?.error?.message ?? err?.message ?? 'Failed to download report')
      ))
    );
  }
}