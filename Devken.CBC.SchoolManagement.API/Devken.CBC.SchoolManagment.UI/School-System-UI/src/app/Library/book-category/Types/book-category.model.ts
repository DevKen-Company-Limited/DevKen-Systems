export interface BookCategoryResponseDto {
  id: string;
  name: string;
  description?: string;
  tenantId: string;
  schoolName?: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateBookCategoryDto {
  name: string;
  description?: string;
  tenantId?: string;
}

export interface UpdateBookCategoryDto {
  name: string;
  description?: string;
}