import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { 
  DashboardQueryParams, 
  DashboardResponse, 
  DashboardPermissions, 
  StatsSection, 
  ClassPerformanceSection, 
  CompetencySection, 
  RecentAssessmentsSection, 
  EventsSection, 
  FeeCollectionSection, 
  QuickActionsSection 
} from 'app/modules/models/dashboard-models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  private get url(): string {
    return `${this.baseUrl}/api/dashboard`;
  }

  getDashboard(params: DashboardQueryParams): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>(this.url, { params: this.buildParams(params) });
  }

  getPermissions(): Observable<DashboardPermissions> {
    return this.http.get<DashboardPermissions>(`${this.url}/permissions`);
  }

  getStats(params: DashboardQueryParams): Observable<StatsSection> {
    return this.http.get<StatsSection>(`${this.url}/stats`, { params: this.buildParams(params) });
  }

  getClassPerformance(params: DashboardQueryParams): Observable<ClassPerformanceSection> {
    return this.http.get<ClassPerformanceSection>(`${this.url}/class-performance`, { params: this.buildParams(params) });
  }

  getCompetency(params: DashboardQueryParams): Observable<CompetencySection> {
    return this.http.get<CompetencySection>(`${this.url}/competency`, { params: this.buildParams(params) });
  }

  getRecentAssessments(params: DashboardQueryParams): Observable<RecentAssessmentsSection> {
    return this.http.get<RecentAssessmentsSection>(`${this.url}/recent-assessments`, { params: this.buildParams(params) });
  }

  getEvents(params: DashboardQueryParams): Observable<EventsSection> {
    return this.http.get<EventsSection>(`${this.url}/events`, { params: this.buildParams(params) });
  }

  getFeeCollection(params: DashboardQueryParams): Observable<FeeCollectionSection> {
    return this.http.get<FeeCollectionSection>(`${this.url}/fee-collection`, { params: this.buildParams(params) });
  }

  getQuickActions(): Observable<QuickActionsSection> {
    return this.http.get<QuickActionsSection>(`${this.url}/quick-actions`);
  }

  private buildParams(query: DashboardQueryParams): HttpParams {
    let params = new HttpParams();
    if (query.level) params = params.set('level', query.level);
    if (query.termId) params = params.set('termId', query.termId);
    if (query.academicYearId) params = params.set('academicYearId', query.academicYearId);
    if (query.schoolId) params = params.set('schoolId', query.schoolId);
    return params;
  }
}