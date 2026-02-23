// ═══════════════════════════════════════════════════════════════════
// summative-assessment-report.service.ts
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
export class SummativeAssessmentReportService {
  private readonly _http        = inject(HttpClient);
  private readonly _apiBase     = inject(API_BASE_URL);
  private readonly _authService = inject(AuthService);

  private readonly _baseUrl = `${this._apiBase}/api/reports/SummativeAssessments`;

  // ── Assessment List PDF ───────────────────────────────────────────────────

  downloadAssessmentsList(filters?: { classId?: string; termId?: string; examType?: string }): Observable<ReportDownloadResult> {
    let params = new HttpParams();
    if (filters?.classId)  params = params.set('classId',  filters.classId);
    if (filters?.termId)   params = params.set('termId',   filters.termId);
    if (filters?.examType) params = params.set('examType', filters.examType);

    return this._http.get(`${this._baseUrl}/assessments-list`, {
      params,
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob        = response.body!;
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const fileMatch   = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
        const fileName    = fileMatch?.[2]?.trim() ?? `Summative_Assessments_${this._timestamp()}.pdf`;
        this._triggerDownload(blob, fileName);
        return { success: true } as ReportDownloadResult;
      }),
      catchError(err => this._handleBlobError(err))
    );
  }

  // ── Score Sheet PDF ───────────────────────────────────────────────────────

  downloadScoreSheet(assessmentId: string): Observable<ReportDownloadResult> {
    return this._http.get(`${this._baseUrl}/score-sheet/${assessmentId}`, {
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob        = response.body!;
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const fileMatch   = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
        const fileName    = fileMatch?.[2]?.trim() ?? `Score_Sheet_${this._timestamp()}.pdf`;
        this._triggerDownload(blob, fileName);
        return { success: true } as ReportDownloadResult;
      }),
      catchError(err => this._handleBlobError(err))
    );
  }

  // ── Student Performance Report ────────────────────────────────────────────

  downloadStudentPerformance(studentId: string, termId?: string): Observable<ReportDownloadResult> {
    let params = new HttpParams();
    if (termId) params = params.set('termId', termId);

    return this._http.get(`${this._baseUrl}/student-performance/${studentId}`, {
      params,
      responseType: 'blob',
      observe: 'response',
    }).pipe(
      map(response => {
        const blob        = response.body!;
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const fileMatch   = disposition.match(/filename[^;=\n]*=(['"]?)([^'";\n]*)\1/);
        const fileName    = fileMatch?.[2]?.trim() ?? `Student_Performance_${this._timestamp()}.pdf`;
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
    return of({ success: false, message: err?.message ?? 'Failed to generate report' } as ReportDownloadResult);
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