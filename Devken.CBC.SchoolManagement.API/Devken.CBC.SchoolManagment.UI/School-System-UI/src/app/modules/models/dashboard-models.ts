export interface DashboardQueryParams {
    level?: string;
    termId?: string;
    academicYearId?: string;
    schoolId?: string;
}

export interface DashboardPermissions {
    canViewStats: boolean;
    canViewClassPerformance: boolean;
    canViewCompetency: boolean;
    canViewRecentAssessments: boolean;
    canViewEvents: boolean;
    canViewFeeCollection: boolean;
    canViewQuickActions: boolean;
}

export interface StatCard {
    icon: string;
    value: string;
    label: string;
    trend: string;
}

export interface StatsSection {
    enrolledStudents: StatCard | null;
    teachingStaff: StatCard | null;
    assessmentsPending: StatCard | null;
    feeCollectionRate: StatCard | null;
}

export interface ClassPerformanceItem {
    classId: string;
    badge: string;
    className: string;
    meta: string;
    pct: number;
    color: string;
}

export interface ClassPerformanceSection {
    termLabel: string;
    classes: ClassPerformanceItem[];
}

export interface CompetencyItem {
    label: string;
    code: string;
    pct: number;
    color: string;
}

export interface CompetencySection {
    items: CompetencyItem[];
}

export interface AssessmentRow {
    assessmentId: string;
    studentName: string;
    className: string;
    learningArea: string;
    assessmentType: string;
    competencyLevel: string;
    assessmentDate: string;
    teacherName: string;
}

export interface RecentAssessmentsSection {
    items: AssessmentRow[];
}

export interface EventItem {
    eventId: string;
    title: string;
    subTitle: string;
    date: string;
    tag: string;
}

export interface EventsSection {
    items: EventItem[];
}

export interface FeeCollectionSection {
    collectedPct: number;
    expectedTotal: string;
    collectedTotal: string;
    outstandingTotal: string;
    defaulterCount: number;
}

export interface QuickActionItem {
    icon: string;
    label: string;
    enabled: boolean;
    action: string;
}

export interface QuickActionsSection {
    items: QuickActionItem[];
}

export interface DashboardResponse {
    schoolName: string;
    academicYear: string;
    currentTerm: string;
    activeLevel: string;
    permissions: DashboardPermissions;
    stats: StatsSection | null;
    classPerformance: ClassPerformanceSection | null;
    competency: CompetencySection | null;
    recentAssessments: RecentAssessmentsSection | null;
    events: EventsSection | null;
    feeCollection: FeeCollectionSection | null;
    quickActions: QuickActionsSection | null;
}