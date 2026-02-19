// types/subjectdto.ts

export interface SubjectDto {
  id:          string;
  code:        string;
  name:        string;
  description: string | null;
  level:       string;           // API returns level as string "0", "1" etc.
  subjectType: string;           // API returns string name: "Core", "Optional", "CoCurricular"
  isActive:    boolean;
  status:      string;
  tenantId:    string;
  createdOn:   string;
  updatedOn:   string;
}

export interface CreateSubjectRequest {
  name:        string;
  description?: string;
  subjectType: string;           // send string name back to API
  level:       number;
  isActive:    boolean;
  tenantId?:   string;
}

export interface UpdateSubjectRequest {
  name:        string;
  description?: string;
  subjectType: string;
  level:       number;
  isActive:    boolean;
  tenantId?:   string;
}