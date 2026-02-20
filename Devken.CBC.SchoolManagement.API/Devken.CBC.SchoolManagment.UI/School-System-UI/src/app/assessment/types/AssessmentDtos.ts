// ─────────────────────────────────────────────────────────────────────────────
// ENUMS matching C# backend
// ─────────────────────────────────────────────────────────────────────────────

export enum AssessmentType {
  Formative  = 1,
  Summative  = 2,
  Competency = 3,
}

export enum CBCLevel {
  PP1     = 1,  PP2     = 2,
  Grade1  = 3,  Grade2  = 4,  Grade3  = 5,
  Grade4  = 6,  Grade5  = 7,  Grade6  = 8,
  Grade7  = 9,  Grade8  = 10, Grade9  = 11,
  Grade10 = 12, Grade11 = 13, Grade12 = 14,
}

export enum AssessmentMethod {
  Observation    = 1,
  OralQuestioning = 2,
  WrittenTask    = 3,
  PracticalTask  = 4,
  Portfolio      = 5,
  Project        = 6,
  Other          = 7,
}

// ─────────────────────────────────────────────────────────────────────────────
// OPTION ARRAYS for dropdowns
// ─────────────────────────────────────────────────────────────────────────────

export const AssessmentTypeOptions = [
  { value: AssessmentType.Formative,  label: 'Formative',  icon: 'assignment',      color: 'indigo',
    description: 'Ongoing, low-stakes assessments to monitor day-to-day learning progress.' },
  { value: AssessmentType.Summative,  label: 'Summative',  icon: 'school',          color: 'violet',
    description: 'End-of-unit or term exams that evaluate cumulative learning.' },
  { value: AssessmentType.Competency, label: 'Competency', icon: 'verified_user',   color: 'teal',
    description: 'Observation-based assessments evaluating CBC competency strands.' },
];

export const AssessmentMethodOptions = [
  { value: AssessmentMethod.Observation,     label: 'Observation',      icon: 'visibility'   },
  { value: AssessmentMethod.OralQuestioning, label: 'Oral Questioning', icon: 'record_voice_over' },
  { value: AssessmentMethod.WrittenTask,     label: 'Written Task',     icon: 'edit_note'    },
  { value: AssessmentMethod.PracticalTask,   label: 'Practical Task',   icon: 'science'      },
  { value: AssessmentMethod.Portfolio,       label: 'Portfolio',        icon: 'collections_bookmark' },
  { value: AssessmentMethod.Project,         label: 'Project',          icon: 'folder_special' },
  { value: AssessmentMethod.Other,           label: 'Other',            icon: 'more_horiz'   },
];

export const FormativeTypeOptions = [
  { value: 'Quiz',        label: 'Quiz',        icon: 'quiz'         },
  { value: 'Homework',    label: 'Homework',    icon: 'home_work'    },
  { value: 'Observation', label: 'Observation', icon: 'visibility'   },
  { value: 'Classwork',   label: 'Classwork',   icon: 'class'        },
  { value: 'Portfolio',   label: 'Portfolio',   icon: 'collections_bookmark' },
  { value: 'Project',     label: 'Project',     icon: 'folder_special' },
  { value: 'Other',       label: 'Other',       icon: 'more_horiz'   },
];

export const ExamTypeOptions = [
  { value: 'MidTerm',  label: 'Mid-Term Exam',  icon: 'assignment_late' },
  { value: 'EndTerm',  label: 'End-Term Exam',  icon: 'assignment_turned_in' },
  { value: 'Final',    label: 'Final Exam',     icon: 'workspace_premium' },
  { value: 'Opening',  label: 'Opening Exam',   icon: 'assignment'       },
  { value: 'Mock',     label: 'Mock Exam',      icon: 'assignment_return' },
];

export const RatingScaleOptions = [
  { value: 'Exceeds|Meets|Approaching|Below',    label: 'Exceeds / Meets / Approaching / Below' },
  { value: 'Excellent|Good|Fair|Poor',           label: 'Excellent / Good / Fair / Poor' },
  { value: 'Advanced|Proficient|Developing|Beginning', label: 'Advanced / Proficient / Developing / Beginning' },
];

// ─────────────────────────────────────────────────────────────────────────────
// LABEL HELPERS
// ─────────────────────────────────────────────────────────────────────────────

export function getAssessmentTypeLabel(val: any): string {
  const n = Number(val);
  const opt = AssessmentTypeOptions.find(o => o.value === n);
  return opt?.label ?? String(val ?? '—');
}

export function getAssessmentTypeColor(val: any): string {
  const n = Number(val);
  const opt = AssessmentTypeOptions.find(o => o.value === n);
  return opt?.color ?? 'gray';
}

export function getAssessmentTypeIcon(val: any): string {
  const n = Number(val);
  const opt = AssessmentTypeOptions.find(o => o.value === n);
  return opt?.icon ?? 'assignment';
}

export function getAssessmentMethodLabel(val: any): string {
  const n = Number(val);
  const opt = AssessmentMethodOptions.find(o => o.value === n);
  return opt?.label ?? String(val ?? '—');
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs (matching API AssessmentDtos.cs)
// ─────────────────────────────────────────────────────────────────────────────

export interface AssessmentListItem {
  id:              string;
  title:           string;
  assessmentType:  AssessmentType;
  assessmentTypeLabel: string;
  teacherName:     string;
  subjectName:     string;
  className:       string;
  termName:        string;
  assessmentDate:  string;
  maximumScore:    number;
  isPublished:     boolean;
  scoreCount:      number;
}

export interface AssessmentResponse {
  id:              string;
  assessmentType:  AssessmentType;
  title:           string;
  description?:    string;
  teacherId:       string;
  teacherName:     string;
  subjectId:       string;
  subjectName:     string;
  classId:         string;
  className:       string;
  termId:          string;
  termName:        string;
  academicYearId:  string;
  academicYearName:string;
  assessmentDate:  string;
  maximumScore:    number;
  isPublished:     boolean;
  publishedDate?:  string;
  createdOn:       string;
  scoreCount:      number;

  // Formative
  formativeType?:       string;
  competencyArea?:      string;
  learningOutcomeId?:   string;
  learningOutcomeName?: string;
  formativeStrand?:     string;
  formativeSubStrand?:  string;
  criteria?:            string;
  feedbackTemplate?:    string;
  requiresRubric?:      boolean;
  assessmentWeight?:    number;
  formativeInstructions?: string;

  // Summative
  examType?:               string;
  duration?:               string;
  numberOfQuestions?:      number;
  passMark?:               number;
  hasPracticalComponent?:  boolean;
  practicalWeight?:        number;
  theoryWeight?:           number;
  summativeInstructions?:  string;

  // Competency
  competencyName?:         string;
  competencyStrand?:       string;
  competencySubStrand?:    string;
  targetLevel?:            CBCLevel;
  performanceIndicators?:  string;
  assessmentMethod?:       AssessmentMethod;
  ratingScale?:            string;
  isObservationBased?:     boolean;
  toolsRequired?:          string;
  competencyInstructions?: string;
  specificLearningOutcome?: string;
}

export interface CreateAssessmentRequest {
  assessmentType:  AssessmentType;
  title:           string;
  description?:    string;
  teacherId:       string;
  subjectId:       string;
  classId:         string;
  termId:          string;
  academicYearId:  string;
  assessmentDate:  string;
  maximumScore:    number;
  isPublished:     boolean;
  tenantId?:       string;

  // Formative
  formativeType?:       string;
  competencyArea?:      string;
  learningOutcomeId?:   string;
  formativeStrand?:     string;
  formativeSubStrand?:  string;
  criteria?:            string;
  feedbackTemplate?:    string;
  requiresRubric?:      boolean;
  assessmentWeight?:    number;
  formativeInstructions?: string;

  // Summative
  examType?:               string;
  duration?:               string;
  numberOfQuestions?:      number;
  passMark?:               number;
  hasPracticalComponent?:  boolean;
  practicalWeight?:        number;
  theoryWeight?:           number;
  summativeInstructions?:  string;

  // Competency
  competencyName?:         string;
  competencyStrand?:       string;
  competencySubStrand?:    string;
  targetLevel?:            CBCLevel;
  performanceIndicators?:  string;
  assessmentMethod?:       AssessmentMethod;
  ratingScale?:            string;
  isObservationBased?:     boolean;
  toolsRequired?:          string;
  competencyInstructions?: string;
  specificLearningOutcome?: string;
}

export interface UpdateAssessmentRequest extends CreateAssessmentRequest {
  id: string;
}

export interface UpsertScoreRequest {
  scoreId?:        string;
  assessmentType:  AssessmentType;
  assessmentId:    string;
  studentId:       string;

  // Formative
  score?:            number;
  maximumScore?:     number;
  grade?:            string;
  performanceLevel?: string;
  feedback?:         string;
  strengths?:        string;
  areasForImprovement?: string;
  isSubmitted?:      boolean;
  competencyArea?:   string;
  competencyAchieved?: boolean;
  gradedById?:       string;

  // Summative
  theoryScore?:           number;
  practicalScore?:        number;
  maximumTheoryScore?:    number;
  maximumPracticalScore?: number;
  remarks?:               string;
  positionInClass?:       number;
  isPassed?:              boolean;
  comments?:              string;

  // Competency
  rating?:               string;
  evidence?:             string;
  isFinalized?:          boolean;
  strand?:               string;
  subStrand?:            string;
  specificLearningOutcome?: string;
  assessorId?:           string;
}

export interface AssessmentScoreResponse {
  id:              string;
  assessmentType:  AssessmentType;
  assessmentId:    string;
  assessmentTitle: string;
  studentId:       string;
  studentName:     string;
  studentAdmissionNo: string;
  assessmentDate:  string;

  score?:          number;
  maximumScore?:   number;
  percentage?:     number;
  grade?:          string;
  performanceLevel?: string;
  feedback?:       string;
  strengths?:      string;
  competencyAchieved?: boolean;
  isSubmitted?:    boolean;
  gradedByName?:   string;

  theoryScore?:    number;
  practicalScore?: number;
  totalScore?:     number;
  maximumTotalScore?: number;
  remarks?:        string;
  positionInClass?: number;
  isPassed?:       boolean;
  performanceStatus?: string;

  rating?:         string;
  competencyLevel?: string;
  evidence?:       string;
  isFinalized?:    boolean;
  assessorName?:   string;
}

// Lookup DTOs — shapes from your existing API endpoints
export interface TeacherLookup { id: string; firstName: string; lastName: string; }
export interface SubjectLookup { id: string; name: string; code: string; }
export interface ClassLookup   { id: string; name: string; }
export interface TermLookup    { id: string; name: string; }
export interface AcademicYearLookup { id: string; name: string; }
export interface LearningOutcomeLookup { id: string; outcome: string; code?: string; }