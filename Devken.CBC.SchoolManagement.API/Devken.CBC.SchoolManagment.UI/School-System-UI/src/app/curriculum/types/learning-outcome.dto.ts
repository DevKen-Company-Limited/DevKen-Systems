import { CBCLevel } from './curriculum-enums';

export interface LearningOutcomeResponseDto {
  id: string;
  outcome: string;
  code?: string;
  description?: string;
  level: string;
  isCore: boolean;
  learningAreaId: string;
  learningAreaName?: string;
  strandId: string;
  strandName?: string;
  subStrandId: string;
  subStrandName?: string;
  tenantId: string;
  status: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateLearningOutcomeDto {
  outcome: string;
  code?: string;
  description?: string;
  level: CBCLevel;
  isCore: boolean;
  learningAreaId: string;
  strandId: string;
  subStrandId: string;
  tenantId?: string;
}

export interface UpdateLearningOutcomeDto {
  outcome: string;
  code?: string;
  description?: string;
  level: CBCLevel;
  isCore: boolean;
  learningAreaId: string;
  strandId: string;
  subStrandId: string;
}