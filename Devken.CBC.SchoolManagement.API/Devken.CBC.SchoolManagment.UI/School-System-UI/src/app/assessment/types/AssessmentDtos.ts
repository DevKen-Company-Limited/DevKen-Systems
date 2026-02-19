// ═══════════════════════════════════════════════════════════════════
// assessment.types.ts
// ═══════════════════════════════════════════════════════════════════

export interface AssessmentDto {
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

  grades?: AssessmentGradeDto[];
}

export interface AssessmentGradeDto {
  id: string;
  studentId: string;
  score: number;
  remarks?: string;
  createdOn: string;
}

export interface CreateAssessmentRequest {
  title: string;
  description?: string;
  assessmentType: string;
  maximumScore: number;
  assessmentDate: string;
  teacherId?: string;
  subjectId?: string;
  classId?: string;
  termId?: string;
  academicYearId?: string;
  schoolId?: string;
}

export interface UpdateAssessmentRequest extends CreateAssessmentRequest {}

export interface UpdateAssessmentPublishRequest {
  isPublished: boolean;
}

export const ASSESSMENT_TYPES = ['Formative', 'Summative', 'Competency'] as const;
export type AssessmentType = typeof ASSESSMENT_TYPES[number];

export const ASSESSMENT_TYPE_COLORS: Record<string, string> = {
  Formative:  'indigo',
  Summative:  'violet',
  Competency: 'teal',
};