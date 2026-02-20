// types/subjectdto.ts

export interface SubjectDto {
  id:           string;
  code:         string;
  name:         string;
  description:  string | null;
  level:       string;        // backend sends "3" (numeric string)
  subjectType: string;        // backend sends "Core" (enum name) subjectType:  number | string;   // API may return number or string name e.g. "Core"
  // isCompulsory: boolean;
  isActive:     boolean;
  tenantId:     string;
  schoolId:     string;
  schoolName:   string | null;
  createdOn:    string | null;
  updatedOn:    string | null;
}

export interface CreateSubjectRequest {
  name:         string;
  description?: string | null;
  subjectType:  number;  // 1=Core, 2=Optional, 3=Elective, 4=CoCurricular
  cbclevel:     number;  // 1=PP1 ... 14=Grade12
  //isCompulsory: boolean;
  isActive:     boolean;
  tenantId?:    string;
}

export interface UpdateSubjectRequest {
  name:         string;
  description?: string | null;
  subjectType:  number;
  cbclevel:     number;
  //isCompulsory: boolean;
  isActive:     boolean;
  tenantId?:    string;
}