import { CBCLevel } from './curriculum-enums';

export interface LearningAreaResponseDto {
  id: string;
  name: string;
  code?: string;
  level: string;
  tenantId: string;
  status: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateLearningAreaDto {
  name: string;
  code?: string;
  level: CBCLevel;
  tenantId?: string;
}

export interface UpdateLearningAreaDto {
  name: string;
  code?: string;
  level: CBCLevel;
}