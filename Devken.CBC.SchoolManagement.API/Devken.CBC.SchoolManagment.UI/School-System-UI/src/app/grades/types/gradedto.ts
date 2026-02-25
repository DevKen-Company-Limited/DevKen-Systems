// types/gradedto.ts

export interface GradeDto {
  id:             string;
  studentId:      string;
  studentName:    string | null;
  subjectId:      string;
  subjectName:    string | null;
  termId:         string | null;
  termName:       string | null;
  assessmentId:   string | null;
  score:          number | null;
  maximumScore:   number | null;
  percentage:     number | null;
  gradeLetter:    string | null;  // e.g. "A", "B", "C"
  gradeType:      string | null;  // e.g. "Formative", "Summative", "Competency"
  assessmentDate: string;
  remarks:        string | null;
  isFinalized:    boolean;
  tenantId:       string;
  schoolId:       string;
  schoolName:     string | null;
  status:         string;
  createdOn:      string | null;
  updatedOn:      string | null;
}

export interface CreateGradeRequest {
  studentId:      string;
  subjectId:      string;
  termId?:        string | null;
  assessmentId?:  string | null;
  score?:         number | null;
  maximumScore?:  number | null;
  gradeLetter?:   number | null;  // enum int: A=0, B=1 …
  gradeType?:     number | null;  // enum int: Formative=0, Summative=1, Competency=2
  assessmentDate: string;
  remarks?:       string | null;
  isFinalized?:   boolean;
  tenantId?:      string;
}

export interface UpdateGradeRequest {
  score?:         number | null;
  maximumScore?:  number | null;
  gradeLetter?:   number | null;
  gradeType?:     number | null;
  assessmentDate: string;
  remarks?:       string | null;
  isFinalized?:   boolean;
}