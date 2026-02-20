// ═══════════════════════════════════════════════════════════════════
// summative-assessment.types.ts
// ═══════════════════════════════════════════════════════════════════

export interface SummativeAssessmentDto {
  id: string;
  title: string;
  description?: string;
  assessmentType: string;
  maximumScore: number;
  assessmentDate: string;
  isPublished: boolean;
  publishedDate?: string;
  createdOn: string;

  teacherId?: string;
  teacherName?: string;
  subjectId?: string;
  subjectName?: string;
  classId?: string;
  className?: string;
  termId?: string;
  termName?: string;
  academicYearId?: string;
  academicYearName?: string;
  schoolId: string;
  schoolName?: string;

  // Summative-specific
  examType?: string;           // EndTerm | MidTerm | Final
  duration?: string;           // TimeSpan as string e.g. "02:30:00"
  numberOfQuestions: number;
  passMark: number;
  hasPracticalComponent: boolean;
  practicalWeight: number;
  theoryWeight: number;
  instructions?: string;

  scores?: SummativeAssessmentScoreDto[];
}

export interface SummativeAssessmentScoreDto {
  id: string;
  summativeAssessmentId?: string;
  assessmentTitle?: string;

  studentId: string;
  studentName?: string;

  theoryScore: number;
  practicalScore?: number;
  maximumTheoryScore: number;
  maximumPracticalScore?: number;

  totalScore: number;
  maximumTotalScore: number;
  percentage: number;
  performanceStatus?: string;

  grade?: string;
  remarks?: string;
  positionInClass?: number;
  positionInStream?: number;
  isPassed: boolean;
  comments?: string;

  gradedDate?: string;
  gradedById?: string;
  gradedByName?: string;

  createdOn: string;
}

export interface CreateSummativeAssessmentRequest {
  title: string;
  description?: string;
  maximumScore: number;
  assessmentDate: string;
  teacherId?: string;
  subjectId?: string;
  classId?: string;
  termId?: string;
  academicYearId?: string;
  schoolId?: string;

  examType?: string;
  duration?: string;
  numberOfQuestions: number;
  passMark: number;
  hasPracticalComponent: boolean;
  practicalWeight: number;
  theoryWeight: number;
  instructions?: string;
}

export interface UpdateSummativeAssessmentRequest extends CreateSummativeAssessmentRequest {}

export interface CreateSummativeAssessmentScoreRequest {
  summativeAssessmentId: string;
  studentId: string;
  theoryScore: number;
  practicalScore?: number;
  maximumTheoryScore: number;
  maximumPracticalScore?: number;
  grade?: string;
  remarks?: string;
  positionInClass?: number;
  positionInStream?: number;
  comments?: string;
}

export interface UpdateSummativeAssessmentScoreRequest {
  theoryScore: number;
  practicalScore?: number;
  maximumTheoryScore: number;
  maximumPracticalScore?: number;
  grade?: string;
  remarks?: string;
  positionInClass?: number;
  positionInStream?: number;
  comments?: string;
}

export const EXAM_TYPES = ['EndTerm', 'MidTerm', 'Final', 'CAT', 'Mock'] as const;
export type ExamType = typeof EXAM_TYPES[number];

export const PERFORMANCE_STATUS_COLORS: Record<string, string> = {
  'Excellent':     'green',
  'Very Good':     'teal',
  'Good':          'blue',
  'Average':       'amber',
  'Below Average': 'orange',
  'Poor':          'red',
};

export interface SummativeAssessmentFormStep {
  label: string;
  icon: string;
  sectionKey: string;
}