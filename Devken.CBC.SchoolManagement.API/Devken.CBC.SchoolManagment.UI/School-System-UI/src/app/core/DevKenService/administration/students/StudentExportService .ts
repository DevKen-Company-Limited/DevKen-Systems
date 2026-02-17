import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { from, Observable, of, throwError } from 'rxjs';
import { catchError, map, mergeMap } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';

export interface ReportDownloadResult {
  success:  boolean;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class StudentReportService {
  private readonly _http       = inject(HttpClient);
  private readonly _apiBase    = inject(API_BASE_URL);
  private readonly _authService = inject(AuthService);

  private readonly _baseUrl = `${this._apiBase}/api/reports/StudentsReports`;

  // ─── Students List PDF ────────────────────────────────────────────────────

  /**
   * Streams the PDF blob from the server and triggers a browser download.
   * Maps to: GET /api/reports/students/students-list?schoolId={schoolId}
   */
downloadStudentsList(schoolId?: string | null): Observable<ReportDownloadResult> {
  let params = new HttpParams();
  if (schoolId) {
    params = params.set('schoolId', schoolId);
  }

  return this._http.get(`${this._baseUrl}/students-list`, {
    params,
    responseType: 'blob',
    observe: 'response',
  }).pipe(
    map(response => {
      const blob = response.body!;
      
      // Extract filename from Content-Disposition header, fallback to timestamp
      const disposition = response.headers.get('Content-Disposition') ?? '';
      const fileMatch = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
      const fileName = fileMatch?.[2]?.trim() ?? `Students_List_${this._timestamp()}.pdf`;

      this._triggerDownload(blob, fileName);

      return { success: true } as ReportDownloadResult;
    }),
    catchError(err => {
  // Check if err.error is a Blob
  if (err.error instanceof Blob) {
    const blob = err.error as Blob; // <-- cast here
    return from(blob.text()).pipe(
      map(text => {
        const parsed = this._tryParseJson(text);
        const message = parsed?.message ?? 'Failed to generate report';
        return { success: false, message } as ReportDownloadResult;
      })
    );
  }

  // Fallback for non-blob errors
  return of({
    success: false,
    message: err?.message ?? 'Failed to generate report'
  } as ReportDownloadResult);
})
  );

}


  // ─── Private helpers ──────────────────────────────────────────────────────

  private _triggerDownload(blob: Blob, fileName: string): void {
    const url  = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href     = url;
    link.download = fileName;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    // Small delay so the browser registers the download before we revoke
    setTimeout(() => URL.revokeObjectURL(url), 500);
  }

  private _timestamp(): string {
    return new Date()
      .toISOString()
      .replace(/[-:T]/g, '')
      .slice(0, 14);
  }

  private _tryParseJson(text: string | null): any {
    if (!text) return null;
    try   { return JSON.parse(text); }
    catch { return null; }
  }
}