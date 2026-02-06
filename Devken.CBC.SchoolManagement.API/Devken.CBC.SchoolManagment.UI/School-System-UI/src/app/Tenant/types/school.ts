export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

export interface SchoolDto {
  id: string;
  slugName: string;
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  logoUrl?: string | null;
  isActive: boolean;
}

export interface CreateSchoolRequest {
  slugName: string;
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  logoUrl?: string | null;
  isActive?: boolean;
}

export interface UpdateSchoolRequest extends CreateSchoolRequest {
  // id passed separately in route; kept shape identical
}