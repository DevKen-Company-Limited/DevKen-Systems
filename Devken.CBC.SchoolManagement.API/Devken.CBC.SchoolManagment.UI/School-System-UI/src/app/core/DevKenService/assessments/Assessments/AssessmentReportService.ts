// ═══════════════════════════════════════════════════════════════════
// assessment-report.service.ts
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { from, Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';

export interface ReportDownloadResult {
  success: boolean;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AssessmentReportService {
  private readonly _http        = inject(HttpClient);
  private readonly _apiBase     = inject(API_BASE_URL);
  private readonly _authService = inject(AuthService);

  private readonly _baseUrl = `${this._apiBase}/api/reports/AssessmentsReports`;

  // ── Assessments List PDF ──────────────────────────────────────────────────

  downloadAssessmentsList(filters?: {
    classId?: string | null;
    termId?: string | null;
    assessmentType?: string | null;
    schoolId?: string | null;
  }): Observable<ReportDownloadResult> {
    let params = new HttpParams();
    if (filters?.classId)        params = params.set('classId',        filters.classId);
    if (filters?.termId)         params = params.set('termId',         filters.termId);
    if (filters?.assessmentType) params = params.set('assessmentType', filters.assessmentType);
    if (filters?.schoolId)       params = params.set('schoolId',       filters.schoolId);

    return this._http.get(`${this._baseUrl}/assessments-list`, {
      params,
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob        = response.body!;
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const fileMatch   = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
        const fileName    = fileMatch?.[2]?.trim() ?? `Assessments_List_${this._timestamp()}.pdf`;
        this._triggerDownload(blob, fileName);
        return { success: true } as ReportDownloadResult;
      }),
      catchError(err => this._handleBlobError(err))
    );
  }

  // ── Assessment Grades PDF ─────────────────────────────────────────────────

  downloadAssessmentGrades(assessmentId: string): Observable<ReportDownloadResult> {
    return this._http.get(`${this._baseUrl}/assessment-grades/${assessmentId}`, {
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob        = response.body!;
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const fileMatch   = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
        const fileName    = fileMatch?.[2]?.trim() ?? `Assessment_Grades_${this._timestamp()}.pdf`;
        this._triggerDownload(blob, fileName);
        return { success: true } as ReportDownloadResult;
      }),
      catchError(err => this._handleBlobError(err))
    );
  }

  // ── Private helpers ───────────────────────────────────────────────────────

  private _handleBlobError(err: any): Observable<ReportDownloadResult> {
    if (err.error instanceof Blob) {
      const blob = err.error as Blob;
      return from(blob.text()).pipe(
        map(text => {
          const parsed  = this._tryParseJson(text);
          const message = parsed?.message ?? 'Failed to generate report';
          return { success: false, message } as ReportDownloadResult;
        })
      );
    }
    return of({ success: false, message: err?.message ?? 'Failed to generate report' });
  }

  private _triggerDownload(blob: Blob, fileName: string): void {
    const url  = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href          = url;
    link.download      = fileName;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 500);
  }

  private _timestamp(): string {
    return new Date().toISOString().replace(/[-:T]/g, '').slice(0, 14);
  }

  private _tryParseJson(text: string | null): any {
    if (!text) return null;
    try   { return JSON.parse(text); }
    catch { return null; }
  }
}