import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { API_BASE_URL } from 'app/app.config';
import { DashboardQueryParams, DashboardResponse, DashboardPermissions, StatsSection, ClassPerformanceSection, CompetencySection, RecentAssessmentsSection, EventsSection, FeeCollectionSection, QuickActionsSection } from 'app/modules/models/dashboard-models';
import { Observable } from 'rxjs';


@Injectable({
    providedIn: 'root',
})
export class DashboardService {
    constructor(
        private http: HttpClient,
        @Inject(API_BASE_URL) private baseUrl: string
    ) {}

    getDashboard(params: DashboardQueryParams): Observable<DashboardResponse> {
        return this.http.get<DashboardResponse>(
            `${this.baseUrl}api/dashboard`,
            { params: this.buildParams(params) }
        );
    }

    getPermissions(): Observable<DashboardPermissions> {
        return this.http.get<DashboardPermissions>(
            `${this.baseUrl}api/dashboard/permissions`
        );
    }

    getStats(params: DashboardQueryParams): Observable<StatsSection> {
        return this.http.get<StatsSection>(
            `${this.baseUrl}api/dashboard/stats`,
            { params: this.buildParams(params) }
        );
    }

    getClassPerformance(params: DashboardQueryParams): Observable<ClassPerformanceSection> {
        return this.http.get<ClassPerformanceSection>(
            `${this.baseUrl}api/dashboard/class-performance`,
            { params: this.buildParams(params) }
        );
    }

    getCompetency(params: DashboardQueryParams): Observable<CompetencySection> {
        return this.http.get<CompetencySection>(
            `${this.baseUrl}api/dashboard/competency`,
            { params: this.buildParams(params) }
        );
    }

    getRecentAssessments(params: DashboardQueryParams): Observable<RecentAssessmentsSection> {
        return this.http.get<RecentAssessmentsSection>(
            `${this.baseUrl}api/dashboard/recent-assessments`,
            { params: this.buildParams(params) }
        );
    }

    getEvents(params: DashboardQueryParams): Observable<EventsSection> {
        return this.http.get<EventsSection>(
            `${this.baseUrl}api/dashboard/events`,
            { params: this.buildParams(params) }
        );
    }

    getFeeCollection(params: DashboardQueryParams): Observable<FeeCollectionSection> {
        return this.http.get<FeeCollectionSection>(
            `${this.baseUrl}api/dashboard/fee-collection`,
            { params: this.buildParams(params) }
        );
    }

    getQuickActions(): Observable<QuickActionsSection> {
        return this.http.get<QuickActionsSection>(
            `${this.baseUrl}api/dashboard/quick-actions`
        );
    }

    private buildParams(query: DashboardQueryParams): HttpParams {
        let params = new HttpParams();
        if (query.level)          params = params.set('level', query.level);
        if (query.termId)         params = params.set('termId', query.termId);
        if (query.academicYearId) params = params.set('academicYearId', query.academicYearId);
        if (query.schoolId)       params = params.set('schoolId', query.schoolId);
        return params;
    }
}