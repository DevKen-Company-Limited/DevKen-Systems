export interface StrandResponseDto {
  id: string;
  name: string;
  learningAreaId: string;
  learningAreaName?: string;
  tenantId: string;
  status: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateStrandDto {
  name: string;
  learningAreaId: string;
  tenantId?: string;
}

export interface UpdateStrandDto {
  name: string;
  learningAreaId: string;
}