// types/subjectdto.ts

export interface SubjectDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  subjectType: number | string;
  cbcLevel: number | string;
  isCompulsory: boolean;
  isActive: boolean;
  schoolId?: string;
  schoolName?: string;
  createdAt?: string | Date;
  updatedAt?: string | Date;
}

export interface CreateSubjectRequest {
  name: string;
  code: string;
  description?: string;
  subjectType: number;
  cbcLevel: number;
  isCompulsory: boolean;
  tenantId?: string;
}

export interface UpdateSubjectRequest extends Partial<CreateSubjectRequest> {}