// types/subjectdto.ts

export interface SubjectDto {
  id:           string;
  code:         string;
  name:         string;
  description:  string | null;
<<<<<<< HEAD
  level:       string;        // backend sends "3" (numeric string)
  subjectType: string;        // backend sends "Core" (enum name) subjectType:  number | string;   // API may return number or string name e.g. "Core"
  // isCompulsory: boolean;
  isActive:     boolean;
  tenantId:     string;
  schoolId:     string;
  schoolName:   string | null;
  createdOn:    string | null;
  updatedOn:    string | null;
=======
  cbcLevel:     number | string;   // API may return number or string name e.g. "Grade1"
  subjectType:  number | string;   // API may return number or string name e.g. "Core"
  isCompulsory: boolean;
  isActive:     boolean;
  schoolId:     string;
  schoolName:   string | null;
  createdAt:    string | null;
  updatedAt:    string | null;
>>>>>>> upstream/main
}

export interface CreateSubjectRequest {
  name:         string;
  description?: string | null;
  subjectType:  number;  // 1=Core, 2=Optional, 3=Elective, 4=CoCurricular
<<<<<<< HEAD
  cbclevel:     number;  // 1=PP1 ... 14=Grade12
  //isCompulsory: boolean;
=======
  cbcLevel:     number;  // 1=PP1 ... 14=Grade12
  isCompulsory: boolean;
>>>>>>> upstream/main
  isActive:     boolean;
  tenantId?:    string;
}

export interface UpdateSubjectRequest {
  name:         string;
  description?: string | null;
  subjectType:  number;
<<<<<<< HEAD
  cbclevel:     number;
  //isCompulsory: boolean;
=======
  cbcLevel:     number;
  isCompulsory: boolean;
>>>>>>> upstream/main
  isActive:     boolean;
  tenantId?:    string;
}