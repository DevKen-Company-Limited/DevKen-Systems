import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DashboardService } from 'app/core/DevKenService/dashboard/dashboard.service';
import {
    DashboardResponse,
    DashboardPermissions,
    ClassPerformanceItem,
    CompetencyItem,
    QuickActionItem,
    AssessmentRow,
    EventItem,
} from 'app/modules/models/dashboard-models';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, DatePipe],
    templateUrl: './example.component.html',
    styleUrls: ['./example.component.scss'],
    encapsulation: ViewEncapsulation.None,
})
export class ExampleComponent implements OnInit {

    loading = false;
    refreshing = false;
    accessDenied = false;
    lastRefreshed: Date | null = null;

    dashboard: DashboardResponse | null = null;
    permissions: DashboardPermissions | null = null;

    activeLevel = 'All Levels';
    levels = [
        'All Levels', 'PP1', 'PP2', 'Grade 1', 'Grade 2', 'Grade 3',
        'Grade 4', 'Grade 5', 'Grade 6', 'JHS 1', 'JHS 2', 'JHS 3',
    ];

    stats: { icon: string; value: string; label: string; trend: string; color: string; iconBg: string; trendBg: string; trendColor: string }[] = [];
    classes: { badge: string; name: string; meta: string; color: string; pct: number }[] = [];
    competencies: { label: string; color: string; pct: number }[] = [];
    quickActions: { icon: string; label: string; enabled: boolean; action: string }[] = [];
    assessments: { student: string; class: string; area: string; type: string; typeBg: string; typeColor: string; level: string; levelBg: string; levelColor: string; date: string; teacher: string }[] = [];
    events: { day: string; month: string; title: string; sub: string; tag: string; tagBg: string; tagColor: string }[] = [];
    finance: { label: string; value: string; cls: string }[] = [];
    feeCollectedPct = 0;
    termLabel = '';
    schoolName = '';
    academicYear = '';
    currentTerm = '';

    // ── Colour palette cycled for class badges when the backend omits a colour ──
    private readonly CLASS_COLORS = [
        '#2563EB', '#16a34a', '#d97706', '#dc2626',
        '#7c3aed', '#0891b2', '#be185d', '#b45309',
    ];

    constructor(
        private readonly route: ActivatedRoute,
        private readonly dashboardService: DashboardService,
    ) { }

    ngOnInit(): void {
        const resolved = this.route.snapshot.data['initialData'];
        // Route resolver may also return the raw envelope — unwrap consistently.
        const raw: any = resolved?.[5] ?? null;
        const prefetched: DashboardResponse | null = raw?.data ?? raw ?? null;

        if (prefetched?.permissions) {
            this.applyResponse(prefetched);
        } else {
            this.loadDashboard();
        }
    }

    setActiveLevel(level: string): void {
        this.activeLevel = level;
        this.loadDashboard();
    }

    refresh(): void {
        this.loadDashboard();
    }

    private loadDashboard(): void {
        this.loading = !this.refreshing;
        this.refreshing = true;
        this.accessDenied = false;

        this.dashboardService
            .getDashboard({ level: this.activeLevel })
            .subscribe({
                next: (raw: any) => {
                    // The backend wraps every response in { success, message, data, statusCode }.
                    // Unwrap here so applyResponse always receives the clean DashboardResponse.
                    // If the service already unwraps it (returns DashboardResponse directly)
                    // the fallback `raw` ensures nothing breaks either way.
                    const res: DashboardResponse = raw?.data ?? raw;

                    if (!raw?.success && raw?.data === undefined) {
                        // Treat an unsuccessful envelope as an error
                        this.accessDenied = true;
                        this.loading = false;
                        this.refreshing = false;
                        return;
                    }

                    this.applyResponse(res);
                },
                error: (_err: any) => {
                    this.accessDenied = true;
                    this.loading = false;
                    this.refreshing = false;
                },
            });
    }

    private applyResponse(res: DashboardResponse): void {
        const p = res.permissions;
        const hasAnyPermission =
            p.canViewStats ||
            p.canViewClassPerformance ||
            p.canViewCompetency ||
            p.canViewRecentAssessments ||
            p.canViewEvents ||
            p.canViewFeeCollection ||
            p.canViewQuickActions;

        if (!hasAnyPermission) {
            this.accessDenied = true;
            this.loading = false;
            this.refreshing = false;
            return;
        }

        this.dashboard = res;
        this.permissions = res.permissions;
        this.schoolName = res.schoolName || 'School';
        // FIX: backend returns "" when no active academic year / term exists.
        //      Show a friendly fallback instead of a blank string.
        this.academicYear = res.academicYear || 'Academic Year';
        this.currentTerm = res.currentTerm || 'Current Term';
        this.termLabel = res.currentTerm || '';

        this.mapStats(res);
        this.mapClasses(res);
        this.mapCompetencies(res);
        this.mapQuickActions(res);
        this.mapAssessments(res);
        this.mapEvents(res);
        this.mapFinance(res);

        this.loading = false;
        this.refreshing = false;
        this.lastRefreshed = new Date();
    }

    private mapStats(res: DashboardResponse): void {
        this.stats = [];

        if (res.stats?.enrolledStudents) {
            this.stats.push({
                icon: res.stats.enrolledStudents.icon,
                value: res.stats.enrolledStudents.value,
                label: res.stats.enrolledStudents.label,
                trend: res.stats.enrolledStudents.trend,
                color: '#2563EB',
                iconBg: 'rgba(37,99,235,0.1)',
                trendBg: 'rgba(22,163,74,0.1)',
                trendColor: '#16a34a',
            });
        }

        if (res.stats?.teachingStaff) {
            this.stats.push({
                icon: res.stats.teachingStaff.icon,
                value: res.stats.teachingStaff.value,
                label: res.stats.teachingStaff.label,
                trend: res.stats.teachingStaff.trend,
                color: '#16a34a',
                iconBg: 'rgba(22,163,74,0.1)',
                trendBg: 'rgba(22,163,74,0.1)',
                trendColor: '#16a34a',
            });
        }

        if (res.stats?.assessmentsPending) {
            this.stats.push({
                icon: res.stats.assessmentsPending.icon,
                value: res.stats.assessmentsPending.value,
                label: res.stats.assessmentsPending.label,
                trend: res.stats.assessmentsPending.trend,
                color: '#d97706',
                iconBg: 'rgba(217,119,6,0.1)',
                trendBg: 'rgba(220,38,38,0.1)',
                trendColor: '#dc2626',
            });
        }

        if (res.stats?.feeCollectionRate) {
            this.stats.push({
                icon: res.stats.feeCollectionRate.icon,
                value: res.stats.feeCollectionRate.value,
                label: res.stats.feeCollectionRate.label,
                trend: res.stats.feeCollectionRate.trend,
                color: '#dc2626',
                iconBg: 'rgba(220,38,38,0.1)',
                trendBg: 'rgba(220,38,38,0.1)',
                trendColor: '#dc2626',
            });
        }
    }

    private mapClasses(res: DashboardResponse): void {
        // FIX: The backend currently only sends classId, className, pct.
        //      badge, meta, and color are commented out server-side.
        //      Generate sensible client-side fallbacks so the UI renders correctly.
        this.classes = (res.classPerformance?.classes ?? []).map(
            (c: ClassPerformanceItem, index: number) => ({
                // badge: use server value if present, else first 4 chars of className
                badge: (c as any).badge
                    || c.className?.substring(0, 4).toUpperCase()
                    || '—',
                name: c.className || '—',
                // meta: use server value if present, else empty
                meta: (c as any).meta || '',
                // color: use server value if present, else cycle through palette
                color: (c as any).color
                    || this.CLASS_COLORS[index % this.CLASS_COLORS.length],
                pct: c.pct ?? 0,
            })
        );
    }

    private mapCompetencies(res: DashboardResponse): void {
        this.competencies = (res.competency?.items ?? []).map((c: CompetencyItem) => ({
            label: c.label,
            color: c.color,
            pct: c.pct,
        }));
    }

    private mapQuickActions(res: DashboardResponse): void {
        this.quickActions = (res.quickActions?.items ?? []).map((a: QuickActionItem) => ({
            icon: a.icon,
            label: a.label,
            enabled: a.enabled,
            action: a.action,
        }));
    }

    private mapAssessments(res: DashboardResponse): void {
        // FIX: backend does not currently project studentName or competencyLevel
        //      (both are commented out in the server-side Select).
        //      Use safe fallbacks so the table renders without errors.
        this.assessments = (res.recentAssessments?.items ?? []).map((a: AssessmentRow) => ({
            // studentName is not returned by backend — show a dash until it is added
            student: (a as any).studentName || '—',
            class: a.className || '—',
            area: a.learningArea || '—',
            type: a.assessmentType || '—',
            typeBg: a.assessmentType === 'Summative'
                ? 'rgba(168,85,247,.1)'
                : 'rgba(99,102,241,.1)',
            typeColor: a.assessmentType === 'Summative' ? '#a855f7' : '#6366f1',
            // competencyLevel is not returned by backend — hide the badge gracefully
            level: (a as any).competencyLevel || '',
            levelBg: this.levelBg((a as any).competencyLevel || ''),
            levelColor: this.levelColor((a as any).competencyLevel || ''),
            date: a.assessmentDate
                ? new Date(a.assessmentDate).toLocaleDateString('en-GB', {
                    day: '2-digit', month: 'short', year: 'numeric',
                })
                : '—',
            teacher: a.teacherName || '—',
        }));
    }

    private mapEvents(res: DashboardResponse): void {
        const tagColors: Record<string, { bg: string; color: string }> = {
            Assessment: { bg: 'rgba(37,99,235,0.1)', color: '#2563EB' },
            Report: { bg: 'rgba(22,163,74,0.1)', color: '#16a34a' },
            Meeting: { bg: 'rgba(217,119,6,0.1)', color: '#d97706' },
        };

        this.events = (res.events?.items ?? []).map((e: EventItem) => {
            const d = new Date(e.date);
            const tag = tagColors[e.tag] ?? { bg: 'rgba(107,114,128,0.1)', color: '#6b7280' };
            return {
                day: String(d.getDate()).padStart(2, '0'),
                month: d.toLocaleString('en-GB', { month: 'short' }),
                title: e.title,
                sub: e.subTitle,
                tag: e.tag,
                tagBg: tag.bg,
                tagColor: tag.color,
            };
        });
    }

    private mapFinance(res: DashboardResponse): void {
        const fc = res.feeCollection;
        if (!fc) {
            this.finance = [];
            this.feeCollectedPct = 0;
            return;
        }

        this.feeCollectedPct = fc.collectedPct;
        this.finance = [
            { label: 'Expected', value: fc.expectedTotal, cls: '' },
            { label: 'Collected', value: fc.collectedTotal, cls: 'pos' },
            { label: 'Outstanding', value: fc.outstandingTotal, cls: 'neg' },
            { label: 'Defaulters', value: `${fc.defaulterCount} student(s)`, cls: 'neg' },
        ];
    }

    // ── Competency level colour helpers ──────────────────────────────────────
    // FIX: the backend Rating strings are "Exceeds" | "Meets" | "Approaching" | "Below"
    //      but the AssessmentRow.competencyLevel field is currently not returned.
    //      When it is re-enabled server-side it will carry the computed label:
    //      "Excellent" | "Proficient" | "Developing" | "Beginning".
    //      Both sets of values are handled below so no mapping changes are needed
    //      once the backend uncomments the field.
    private levelBg(level: string): string {
        const map: Record<string, string> = {
            // Computed label values (CompetencyAssessmentScore.CompetencyLevel)
            Excellent: 'rgba(37,99,235,0.1)',
            Proficient: 'rgba(22,163,74,0.1)',
            Developing: 'rgba(217,119,6,0.1)',
            Beginning: 'rgba(220,38,38,0.1)',
            // Raw Rating values — also handle in case backend returns them directly
            Exceeds: 'rgba(37,99,235,0.1)',
            Meets: 'rgba(22,163,74,0.1)',
            Approaching: 'rgba(217,119,6,0.1)',
            Below: 'rgba(220,38,38,0.1)',
        };
        return map[level] ?? 'rgba(107,114,128,0.1)';
    }

    private levelColor(level: string): string {
        const map: Record<string, string> = {
            Excellent: '#2563EB',
            Proficient: '#16a34a',
            Developing: '#d97706',
            Beginning: '#dc2626',
            Exceeds: '#2563EB',
            Meets: '#16a34a',
            Approaching: '#d97706',
            Below: '#dc2626',
        };
        return map[level] ?? '#6b7280';
    }
}