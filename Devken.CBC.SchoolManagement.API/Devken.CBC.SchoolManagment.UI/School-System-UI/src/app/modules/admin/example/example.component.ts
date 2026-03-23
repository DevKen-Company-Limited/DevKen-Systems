import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from 'app/core/DevKenService/dashboard/dashboard.service';
import { DashboardResponse, DashboardPermissions, ClassPerformanceItem, CompetencyItem, QuickActionItem, AssessmentRow, EventItem } from 'app/modules/models/dashboard-models';


@Component({
    selector     : 'app-dashboard',
    standalone   : true,
    imports      : [CommonModule],
    templateUrl  : './example.component.html',
    styleUrls    : ['./example.component.scss'],
    encapsulation: ViewEncapsulation.None,
})
export class DashboardComponent implements OnInit {

    loading        = true;
    accessDenied   = false;
    dashboard      : DashboardResponse | null = null;
    permissions    : DashboardPermissions | null = null;

    activeLevel    = 'All Levels';
    levels         = [
        'All Levels', 'PP1', 'PP2', 'Grade 1', 'Grade 2', 'Grade 3',
        'Grade 4', 'Grade 5', 'Grade 6', 'JHS 1', 'JHS 2', 'JHS 3',
    ];

    stats          : { icon: string; value: string; label: string; trend: string; color: string; iconBg: string; trendBg: string; trendColor: string }[] = [];
    classes        : { badge: string; name: string; meta: string; color: string; pct: number }[] = [];
    competencies   : { label: string; color: string; pct: number }[] = [];
    quickActions   : { icon: string; label: string; enabled: boolean; action: string }[] = [];
    assessments    : { student: string; class: string; area: string; type: string; typeBg: string; typeColor: string; level: string; levelBg: string; levelColor: string; date: string; teacher: string }[] = [];
    events         : { day: string; month: string; title: string; sub: string; tag: string; tagBg: string; tagColor: string }[] = [];
    finance        : { label: string; value: string; cls: string }[] = [];
    feeCollectedPct = 0;
    termLabel      = '';
    schoolName     = '';
    academicYear   = '';
    currentTerm    = '';

    constructor(private dashboardService: DashboardService) {}

    ngOnInit(): void {
        this.load();
    }

    setActiveLevel(level: string): void {
        this.activeLevel = level;
        this.load();
    }

    private load(): void {
        this.loading      = true;
        this.accessDenied = false;

        this.dashboardService.getDashboard({ level: this.activeLevel }).subscribe({
            next : (res) => {
                if (!res.permissions.canViewStats
                    && !res.permissions.canViewClassPerformance
                    && !res.permissions.canViewCompetency
                    && !res.permissions.canViewRecentAssessments
                    && !res.permissions.canViewEvents
                    && !res.permissions.canViewFeeCollection
                    && !res.permissions.canViewQuickActions) {
                    this.accessDenied = true;
                    this.loading      = false;
                    return;
                }

                this.dashboard    = res;
                this.permissions  = res.permissions;
                this.schoolName   = res.schoolName;
                this.academicYear = res.academicYear;
                this.currentTerm  = res.currentTerm;
                this.termLabel    = res.currentTerm;

                this.mapStats(res);
                this.mapClasses(res);
                this.mapCompetencies(res);
                this.mapQuickActions(res);
                this.mapAssessments(res);
                this.mapEvents(res);
                this.mapFinance(res);

                this.loading = false;
            },
            error: (err) => {
                if (err.status === 401 || err.status === 403) {
                    this.accessDenied = true;
                } else {
                    this.accessDenied = true;
                }
                this.loading = false;
            },
        });
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
        this.classes = (res.classPerformance?.classes ?? []).map((c: ClassPerformanceItem) => ({
            badge : c.badge,
            name  : c.className,
            meta  : c.meta,
            color : c.color,
            pct   : c.pct,
        }));
    }

    private mapCompetencies(res: DashboardResponse): void {
        this.competencies = (res.competency?.items ?? []).map((c: CompetencyItem) => ({
            label : c.label,
            color : c.color,
            pct   : c.pct,
        }));
    }

    private mapQuickActions(res: DashboardResponse): void {
        this.quickActions = (res.quickActions?.items ?? []).map((a: QuickActionItem) => ({
            icon    : a.icon,
            label   : a.label,
            enabled : a.enabled,
            action  : a.action,
        }));
    }

    private mapAssessments(res: DashboardResponse): void {
        this.assessments = (res.recentAssessments?.items ?? []).map((a: AssessmentRow) => ({
            student    : a.studentName,
            class      : a.className,
            area       : a.learningArea,
            type       : a.assessmentType,
            typeBg     : a.assessmentType === 'Summative' ? 'rgba(168,85,247,.1)' : 'rgba(99,102,241,.1)',
            typeColor  : a.assessmentType === 'Summative' ? '#a855f7' : '#6366f1',
            level      : a.competencyLevel,
            levelBg    : this.levelBg(a.competencyLevel),
            levelColor : this.levelColor(a.competencyLevel),
            date       : new Date(a.assessmentDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }),
            teacher    : a.teacherName,
        }));
    }

    private mapEvents(res: DashboardResponse): void {
        const tagColors: Record<string, { bg: string; color: string }> = {
            Assessment : { bg: 'rgba(37,99,235,0.1)',  color: '#2563EB' },
            Report     : { bg: 'rgba(22,163,74,0.1)',  color: '#16a34a' },
            Meeting    : { bg: 'rgba(217,119,6,0.1)',  color: '#d97706' },
        };

        this.events = (res.events?.items ?? []).map((e: EventItem) => {
            const d   = new Date(e.date);
            const tag = tagColors[e.tag] ?? { bg: 'rgba(107,114,128,0.1)', color: '#6b7280' };
            return {
                day      : String(d.getDate()).padStart(2, '0'),
                month    : d.toLocaleString('en-GB', { month: 'short' }),
                title    : e.title,
                sub      : e.subTitle,
                tag      : e.tag,
                tagBg    : tag.bg,
                tagColor : tag.color,
            };
        });
    }

    private mapFinance(res: DashboardResponse): void {
        const fc = res.feeCollection;
        if (!fc) {
            this.finance        = [];
            this.feeCollectedPct = 0;
            return;
        }

        this.feeCollectedPct = fc.collectedPct;
        this.finance = [
            { label: 'Expected (Term 1)', value: fc.expectedTotal,              cls: ''    },
            { label: 'Collected',          value: fc.collectedTotal,             cls: 'pos' },
            { label: 'Outstanding',        value: fc.outstandingTotal,           cls: 'neg' },
            { label: 'Defaulters',         value: `${fc.defaulterCount} students`, cls: 'neg' },
        ];
    }

    private levelBg(level: string): string {
        const map: Record<string, string> = {
            EE: 'rgba(37,99,235,0.1)',
            ME: 'rgba(22,163,74,0.1)',
            AE: 'rgba(217,119,6,0.1)',
            BE: 'rgba(220,38,38,0.1)',
        };
        return map[level] ?? 'rgba(107,114,128,0.1)';
    }

    private levelColor(level: string): string {
        const map: Record<string, string> = {
            EE: '#2563EB',
            ME: '#16a34a',
            AE: '#d97706',
            BE: '#dc2626',
        };
        return map[level] ?? '#6b7280';
    }
}