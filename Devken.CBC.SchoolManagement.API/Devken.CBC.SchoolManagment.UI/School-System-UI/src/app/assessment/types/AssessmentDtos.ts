// ═══════════════════════════════════════════════════════════════════
// AssessmentDtos.ts
// ═══════════════════════════════════════════════════════════════════

// ── Enums ─────────────────────────────────────────────────────────

export enum AssessmentType {
  Formative  = 1,
  Summative  = 2,
  Competency = 3,
}

export enum AssessmentMethod {
  Observation    = 'Observation',
  Portfolio      = 'Portfolio',
  Project        = 'Project',
  Practical      = 'Practical',
  Written        = 'Written',
  Oral           = 'Oral',
  PeerAssessment = 'PeerAssessment',
}

export enum CBCLevel {
  Below      = 'Below',
  Approaching= 'Approaching',
  MeetingExpectations = 'MeetingExpectations',
  Exceeding  = 'Exceeding',
}

// ── Label / colour / icon helpers ─────────────────────────────────

export function getAssessmentTypeLabel(type: AssessmentType): string {
  switch (type) {
    case AssessmentType.Formative:  return 'Formative';
    case AssessmentType.Summative:  return 'Summative';
    case AssessmentType.Competency: return 'Competency';
    default: return 'Unknown';
  }
}

export function getAssessmentTypeColor(type: AssessmentType): string {
  switch (type) {
    case AssessmentType.Formative:  return 'blue';
    case AssessmentType.Summative:  return 'violet';
    case AssessmentType.Competency: return 'pink';
    default: return 'gray';
  }
}

export function getAssessmentTypeIcon(type: AssessmentType): string {
  switch (type) {
    case AssessmentType.Formative:  return 'assignment';
    case AssessmentType.Summative:  return 'school';
    case AssessmentType.Competency: return 'verified_user';
    default: return 'help';
  }
}

// ── Option lists ──────────────────────────────────────────────────

export interface SelectOption { label: string; value: string | number; }

export const AssessmentTypeOptions: SelectOption[] = [
  { label: 'Formative',  value: AssessmentType.Formative  },
  { label: 'Summative',  value: AssessmentType.Summative  },
  { label: 'Competency', value: AssessmentType.Competency },
];

/** Alias kept for components that import ASSESSMENT_TYPES */
export const ASSESSMENT_TYPES = AssessmentTypeOptions;

export const FormativeTypeOptions: SelectOption[] = [
  { label: 'Quiz',              value: 'Quiz'              },
  { label: 'Assignment',        value: 'Assignment'        },
  { label: 'Class Work',        value: 'ClassWork'         },
  { label: 'Homework',          value: 'Homework'          },
  { label: 'Project',           value: 'Project'           },
  { label: 'Practical',         value: 'Practical'         },
  { label: 'Oral Assessment',   value: 'OralAssessment'    },
  { label: 'Portfolio',         value: 'Portfolio'         },
  { label: 'Observation',       value: 'Observation'       },
];

export const ExamTypeOptions: SelectOption[] = [
  { label: 'End of Term Exam',  value: 'EndOfTermExam'     },
  { label: 'Mid Term Exam',     value: 'MidTermExam'       },
  { label: 'Mock Exam',         value: 'MockExam'          },
  { label: 'National Exam',     value: 'NationalExam'      },
  { label: 'Standardised Test', value: 'StandardisedTest'  },
  { label: 'Opening Exam',      value: 'OpeningExam'       },
];

export const RatingScaleOptions: SelectOption[] = [
  { label: 'Exceeds Expectations (EE)', value: 'EE'  },
  { label: 'Meets Expectations (ME)',   value: 'ME'  },
  { label: 'Approaching Expectations (AE)', value: 'AE' },
  { label: 'Below Expectations (BE)',   value: 'BE'  },
];

// ── DTOs ──────────────────────────────────────────────────────────

/** Full assessment response — mirrors C# AssessmentResponse */
export interface AssessmentDto {
  id:               string;
  assessmentType:   AssessmentType;
  title:            string;
  description?:     string;
  teacherId:        string;
  teacherName:      string;
  subjectId:        string;
  subjectName:      string;
  classId:          string;
   schoolId?:        string; 
  className:        string;
  termId:           string;
  termName:         string;
  academicYearId:   string;
  academicYearName: string;
  assessmentDate:   string;
  maximumScore:     number;
  isPublished:      boolean;
  publishedDate?:   string;
  createdOn:        string;
  scoreCount:       number;

  // Formative
  formativeType?:        string;
  competencyArea?:       string;
  strandId?:             string;
  strandName?:           string;
  subStrandId?:          string;
  subStrandName?:        string;
  learningOutcomeId?:    string;
  learningOutcomeName?:  string;
  criteria?:             string;
  feedbackTemplate?:     string;
  requiresRubric?:       boolean;
  assessmentWeight?:     number;
  formativeInstructions?: string;

  // Summative
  examType?:             string;
  duration?:             string;
  numberOfQuestions?:    number;
  passMark?:             number;
  hasPracticalComponent?: boolean;
  practicalWeight?:      number;
  theoryWeight?:         number;
  summativeInstructions?: string;

  // Competency
  competencyName?:          string;
  competencyStrand?:        string;
  competencySubStrand?:     string;
  targetLevel?:             string;
  performanceIndicators?:   string;
  assessmentMethod?:        string;
  ratingScale?:             string;
  isObservationBased?:      boolean;
  toolsRequired?:           string;
  competencyInstructions?:  string;
  specificLearningOutcome?: string;
}

/** Lightweight item for list/grid — mirrors C# AssessmentListItem */
export interface AssessmentListItem {
  id:               string;
  title:            string;
  assessmentType:   AssessmentType;
  assessmentTypeLabel: string;
  teacherName:      string;
  subjectName:      string;
  className:        string;
  termName:         string;
  assessmentDate:   string;
  maximumScore:     number;
  isPublished:      boolean;
  scoreCount:       number;
  strandName?:      string;
  subStrandName?:   string;
}

// ── Request DTOs ──────────────────────────────────────────────────

export interface CreateAssessmentRequest {
  assessmentType:   AssessmentType;
  title:            string;
  description?:     string;
  teacherId:        string;
  subjectId:        string;
  classId:          string;
  termId:           string;
  academicYearId:   string;
  assessmentDate:   string;
  maximumScore:     number;
  isPublished?:     boolean;
  schoolId?:        string;   // frontend alias → backend resolves to tenantId

  // Formative
  formativeType?:        string;
  competencyArea?:       string;
  strandId?:             string;
  subStrandId?:          string;
  learningOutcomeId?:    string;
  criteria?:             string;
  feedbackTemplate?:     string;
  requiresRubric?:       boolean;
  assessmentWeight?:     number;
  formativeInstructions?: string;

  // Summative
  examType?:             string;
  duration?:             string;
  numberOfQuestions?:    number;
  passMark?:             number;
  hasPracticalComponent?: boolean;
  practicalWeight?:      number;
  theoryWeight?:         number;
  summativeInstructions?: string;

  // Competency
  competencyName?:          string;
  competencyStrand?:        string;
  competencySubStrand?:     string;
  targetLevel?:             string;
  performanceIndicators?:   string;
  assessmentMethod?:        string;
  ratingScale?:             string;
  isObservationBased?:      boolean;
  toolsRequired?:           string;
  competencyInstructions?:  string;
  specificLearningOutcome?: string;
}

export interface UpdateAssessmentRequest extends CreateAssessmentRequest {
  id: string;
}

export interface PublishAssessmentRequest {
  assessmentType: AssessmentType;
}
export const ASSESSMENT_TYPE_COLORS: Record<string, string> = {
  Formative:  'blue',
  Summative:  'violet',
  Competency: 'pink',
};

// ── Score DTOs ────────────────────────────────────────────────────

export interface UpsertScoreRequest {
  scoreId?:          string;
  assessmentType:    AssessmentType;
  assessmentId:      string;
  studentId:         string;
  score?:            number;
  maximumScore?:     number;
  grade?:            string;
  performanceLevel?: string;
  feedback?:         string;
  strengths?:        string;
  areasForImprovement?: string;
  isSubmitted?:      boolean;
  submissionDate?:   string;
  competencyArea?:   string;
  competencyAchieved?: boolean;
  gradedById?:       string;
  theoryScore?:      number;
  practicalScore?:   number;
  maximumTheoryScore?: number;
  maximumPracticalScore?: number;
  remarks?:          string;
  positionInClass?:  number;
  positionInStream?: number;
  isPassed?:         boolean;
  comments?:         string;
  rating?:           string;
  scoreValue?:       number;
  evidence?:         string;
  toolsUsed?:        string;
  isFinalized?:      boolean;
  strand?:           string;
  subStrand?:        string;
  specificLearningOutcome?: string;
  assessorId?:       string;
}

export interface AssessmentScoreResponse {
  id:                  string;
  assessmentType:      AssessmentType;
  assessmentId:        string;
  assessmentTitle:     string;
  studentId:           string;
  studentName:         string;
  studentAdmissionNo:  string;
  assessmentDate:      string;
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
  theoryScore?:        number;
  practicalScore?:     number;
  totalScore?:         number;
  maximumTotalScore?:  number;
  remarks?:            string;
  positionInClass?:    number;
  isPassed?:           boolean;
  performanceStatus?:  string;
  comments?:           string;
  rating?:             string;
  competencyLevel?:    string;
  evidence?:           string;
  isFinalized?:        boolean;
  assessorName?:       string;
  strand?:             string;
  subStrand?:          string;
}

// After AssessmentMethod enum
export const AssessmentMethodOptions: SelectOption[] = [
  { label: 'Observation',      value: AssessmentMethod.Observation    },
  { label: 'Portfolio',        value: AssessmentMethod.Portfolio       },
  { label: 'Project',          value: AssessmentMethod.Project         },
  { label: 'Practical',        value: AssessmentMethod.Practical       },
  { label: 'Written',          value: AssessmentMethod.Written         },
  { label: 'Oral',             value: AssessmentMethod.Oral            },
  { label: 'Peer Assessment',  value: AssessmentMethod.PeerAssessment  },
];