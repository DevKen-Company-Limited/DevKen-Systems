export interface BookAuthorResponseDto {
  id: string;
  name: string;
  biography?: string;
  tenantId: string;
  schoolName?: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateBookAuthorDto {
  name: string;
  biography?: string;
  tenantId?: string; // Required for SuperAdmin
}

export interface UpdateBookAuthorDto {
  name: string;
  biography?: string;
}