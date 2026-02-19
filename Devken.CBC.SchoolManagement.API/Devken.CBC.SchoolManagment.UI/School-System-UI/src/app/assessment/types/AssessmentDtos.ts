// ─────────────────────────────────────────────────────────────────────────────
// Assessment Types
// ─────────────────────────────────────────────────────────────────────────────

export interface AssessmentDto {
  id: string;
  title: string;
  description?: string;

  teacherId: string;
  teacherName?: string;

  subjectId: string;
  subjectName?: string;

  classId: string;
  className?: string;

  termId: string;
  termName?: string;

  academicYearId: string;
  academicYearName?: string;

  assessmentDate: string;
  maximumScore: number;

  /** Formative | Summative | Competency */
  assessmentType: string;

  isPublished: boolean;
  publishedDate?: string;

  schoolId: string;
  schoolName?: string;

  createdOn: string;
}

export interface CreateAssessmentRequest {
  title: string;
  description?: string | null;
  teacherId: string;
  subjectId: string;
  classId: string;
  termId: string;
  academicYearId: string;
  assessmentDate: string;
  maximumScore: number;
  assessmentType: string;
  schoolId?: string | null; // SuperAdmin only
}

export interface UpdateAssessmentRequest {
  title: string;
  description?: string | null;
  teacherId: string;
  subjectId: string;
  classId: string;
  termId: string;
  academicYearId: string;
  assessmentDate: string;
  maximumScore: number;
  assessmentType: string;
}

export interface UpdateAssessmentPublishRequest {
  isPublished: boolean;
}

// ─────────────────────────────────────────────────────────────────────────────
// Lookup types returned by related endpoints
// ─────────────────────────────────────────────────────────────────────────────

export interface TeacherLookup  { id: string; fullName: string; }
export interface SubjectLookup  { id: string; name: string; }
export interface ClassLookup    { id: string; name: string; }
export interface TermLookup     { id: string; name: string; }
export interface AcademicYearLookup { id: string; name: string; }