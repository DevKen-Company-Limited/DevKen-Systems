export interface BookPublisherResponseDto {
  id: string;
  name: string;
  address?: string;
  tenantId: string;
  schoolName?: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateBookPublisherDto {
  name: string;
  address?: string;
  tenantId?: string;
}

export interface UpdateBookPublisherDto {
  name: string;
  address?: string;
}