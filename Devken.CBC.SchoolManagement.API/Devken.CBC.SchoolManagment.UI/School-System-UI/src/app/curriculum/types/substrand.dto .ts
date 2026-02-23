export interface SubStrandResponseDto {
  id: string;
  name: string;
  strandId: string;
  strandName?: string;
  learningAreaId: string;
  learningAreaName?: string;
  tenantId: string;
  status: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateSubStrandDto {
  name: string;
  strandId: string;
  tenantId?: string;
}

export interface UpdateSubStrandDto {
  name: string;
  strandId: string;
}