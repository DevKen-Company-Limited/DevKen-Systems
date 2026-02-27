// ═══════════════════════════════════════════════════════════════════
// types/AssessmentDtos.ts
// Mirrors Devken.CBC.SchoolManagement.Application.DTOs.Assessments
// ═══════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────
// ENUMS
// ─────────────────────────────────────────────────────────────────────────

export enum AssessmentType {
  Formative  = 1,
  Summative  = 2,
  Competency = 3,
}

export enum AssessmentMethod {
  Observation    = 0,
  Written        = 1,
  Oral           = 2,
  Practical      = 3,
  Portfolio      = 4,
  PeerAssessment = 5,
}

// Human-readable labels
export const ASSESSMENT_TYPE_LABELS: Record<AssessmentType, string> = {
  [AssessmentType.Formative]:  'Formative',
  [AssessmentType.Summative]:  'Summative',
  [AssessmentType.Competency]: 'Competency',
};

export function getAssessmentTypeLabel(type: AssessmentType): string {
  return ASSESSMENT_TYPE_LABELS[type] ?? 'Unknown';
}

export const ASSESSMENT_TYPES: readonly string[] = ['Formative', 'Summative', 'Competency'];

export const ASSESSMENT_TYPE_COLORS: Record<string, string> = {
  Formative:  'indigo',
  Summative:  'violet',
  Competency: 'teal',
};

// ─────────────────────────────────────────────────────────────────────────
// LIST ITEM  (lightweight — table/grid views)
// ─────────────────────────────────────────────────────────────────────────

export interface AssessmentListItem {
  classId(classId: any): unknown;
  id:             string;
  title:          string;
  assessmentType: AssessmentType;
  teacherName:    string;
  subjectName:    string;
  className:      string;
  termName:       string;
  academicYearName?: string;
  assessmentDate: string;
  maximumScore:   number;
  isPublished:    boolean;
  scoreCount:     number;

  // Formative-only
  strandName?:    string;
  subStrandName?: string;
}

// ─────────────────────────────────────────────────────────────────────────
// FULL RESPONSE  (detail view — all type-specific fields)
// ─────────────────────────────────────────────────────────────────────────

export interface AssessmentResponse {
  schoolName: any;
  // Shared
  id:               string;
  assessmentType:   AssessmentType;
  title:            string;
  description?:     string;
  teacherId:        string;
  teacherName:      string;
  subjectId:        string;
  subjectName:      string;
  classId:          string;
  className:        string;
  termId:           string;
  termName:         string;
  academicYearId:   string;
  academicYearName: string;
  schoolId?:        string;
  assessmentDate:   string;
  maximumScore:     number;
  isPublished:      boolean;
  publishedDate?:   string;
  createdOn:        string;
  scoreCount:       number;

  // ── Formative-specific ────────────────────────────────────────────────
  formativeType?:         string;
  competencyArea?:        string;
  strandId?:              string;
  strandName?:            string;
  subStrandId?:           string;
  subStrandName?:         string;
  learningOutcomeId?:     string;
  learningOutcomeName?:   string;
  criteria?:              string;
  feedbackTemplate?:      string;
  requiresRubric?:        boolean;
  assessmentWeight?:      number;
  formativeInstructions?: string;

  // ── Summative-specific ────────────────────────────────────────────────
  examType?:              string;
  duration?:              string;          // TimeSpan serialised as string "HH:mm:ss"
  numberOfQuestions?:     number;
  passMark?:              number;
  hasPracticalComponent?: boolean;
  practicalWeight?:       number;
  theoryWeight?:          number;
  summativeInstructions?: string;

  // ── Competency-specific ───────────────────────────────────────────────
  competencyName?:          string;
  competencyStrand?:        string;
  competencySubStrand?:     string;
  targetLevel?:             unknown;
  performanceIndicators?:   string;
  assessmentMethod?:        AssessmentMethod | unknown;
  ratingScale?:             string;
  isObservationBased?:      boolean;
  toolsRequired?:           string;
  competencyInstructions?:  string;
  specificLearningOutcome?: string;
}

// ─────────────────────────────────────────────────────────────────────────
// SCORE RESPONSE
// ─────────────────────────────────────────────────────────────────────────

export interface AssessmentScoreResponse {
  id:                string;
  assessmentType:    AssessmentType;
  assessmentId:      string;
  assessmentTitle:   string;
  studentId:         string;
  studentName:       string;
  studentAdmissionNo: string;
  assessmentDate:    string;

  // Formative
  score?:              number;
  maximumScore?:       number;
  percentage?:         number;
  grade?:              string;
  performanceLevel?:   string;
  feedback?:           string;
  strengths?:          string;
  competencyAchieved?: boolean;
  isSubmitted?:        boolean;
  gradedByName?:       string;

  // Summative
  theoryScore?:        number;
  practicalScore?:     number;
  totalScore?:         number;
  maximumTotalScore?:  number;
  remarks?:            string;
  positionInClass?:    number;
  isPassed?:           boolean;
  performanceStatus?:  string;
  comments?:           string;

  // Competency
  rating?:             string;
  competencyLevel?:    string;
  evidence?:           string;
  isFinalized?:        boolean;
  assessorName?:       string;
  strand?:             string;
  subStrand?:          string;
}

// ─────────────────────────────────────────────────────────────────────────
// CREATE REQUEST  (mirrors CreateAssessmentRequest.cs)
// ─────────────────────────────────────────────────────────────────────────

export interface CreateAssessmentRequest {
  assessmentType: any;   // "Formative" | "Summative" | "Competency"

  // Tenant resolution
  tenantId?:  string;
  schoolId?:  string;

  // ── Shared (required) ────────────────────────────────────────────────
  title:          string;
  description?:   string;
  teacherId:      string;
  subjectId:      string;
  classId:        string;
  termId:         string;
  academicYearId: string;
  assessmentDate: string;  // ISO date string
  maximumScore:   number;

  // ── Formative-specific ────────────────────────────────────────────────
  formativeType?:         string;
  competencyArea?:        string;
  strandId?:              string;
  subStrandId?:           string;
  learningOutcomeId?:     string;
  criteria?:              string;
  feedbackTemplate?:      string;
  requiresRubric?:        boolean;
  assessmentWeight?:      number;
  formativeInstructions?: string;

  // ── Summative-specific ────────────────────────────────────────────────
  examType?:              string;
  duration?:              string;   // "HH:mm:ss"
  numberOfQuestions?:     number;
  passMark?:              number;
  hasPracticalComponent?: boolean;
  practicalWeight?:       number;
  theoryWeight?:          number;
  summativeInstructions?: string;

  // ── Competency-specific ───────────────────────────────────────────────
  competencyName?:          string;
  competencyStrand?:        string;
  competencySubStrand?:     string;
  targetLevel?:             unknown;
  performanceIndicators?:   string;
  assessmentMethod?:        AssessmentMethod;
  ratingScale?:             string;
  isObservationBased?:      boolean;
  toolsRequired?:           string;
  competencyInstructions?:  string;
  specificLearningOutcome?: string;
}

// ─────────────────────────────────────────────────────────────────────────
// UPDATE REQUEST  (mirrors UpdateAssessmentRequest.cs)
// ─────────────────────────────────────────────────────────────────────────

export interface UpdateAssessmentRequest extends CreateAssessmentRequest {
  id: string;
}

// ─────────────────────────────────────────────────────────────────────────
// PUBLISH REQUEST
// ─────────────────────────────────────────────────────────────────────────

export interface PublishAssessmentRequest {
  assessmentType: string;
}

// ─────────────────────────────────────────────────────────────────────────
// UPSERT SCORE REQUEST  (mirrors UpsertScoreRequest.cs)
// ─────────────────────────────────────────────────────────────────────────

export interface UpsertScoreRequest {
  assessmentId:   string;
  assessmentType: AssessmentType;
  studentId:      string;

  // Formative
  score?:                 number;
  maximumScore?:          number;
  grade?:                 string;
  performanceLevel?:      string;
  feedback?:              string;
  strengths?:             string;
  areasForImprovement?:   string;
  isSubmitted?:           boolean;
  submissionDate?:        string;
  competencyArea?:        string;
  competencyAchieved?:    boolean;
  gradedById?:            string;

  // Summative
  theoryScore?:           number;
  practicalScore?:        number;
  maximumTheoryScore?:    number;
  maximumPracticalScore?: number;
  remarks?:               string;
  positionInClass?:       number;
  positionInStream?:      number;
  isPassed?:              boolean;
  comments?:              string;

  // Competency
  rating?:                string;
  scoreValue?:            number;
  evidence?:              string;
  assessmentMethod?:      string;
  toolsUsed?:             string;
  isFinalized?:           boolean;
  strand?:                string;
  subStrand?:             string;
  specificLearningOutcome?: string;
  assessorId?:            string;
}

// ─────────────────────────────────────────────────────────────────────────
// QUERY PARAMS  (GET /api/assessments)
// ─────────────────────────────────────────────────────────────────────────

export interface AssessmentQueryParams {
  type?:        AssessmentType;
  classId?:     string;
  termId?:      string;
  subjectId?:   string;
  teacherId?:   string;
  isPublished?: boolean;
}

// ─────────────────────────────────────────────────────────────────────────
// SCHEMA RESPONSE  (GET /api/assessments/schema/{type})
// ─────────────────────────────────────────────────────────────────────────

export interface SchemaField {
  field:    string;
  label:    string;
  required: boolean;
}

export interface AssessmentSchemaResponse {
  type:               string;
  sharedFields:       SchemaField[];
  typeSpecificFields: SchemaField[];
}