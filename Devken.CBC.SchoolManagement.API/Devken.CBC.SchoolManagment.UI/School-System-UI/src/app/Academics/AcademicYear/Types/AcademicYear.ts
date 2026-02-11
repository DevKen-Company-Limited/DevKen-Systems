export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

export interface AcademicYearDto {
  id: string;
  schoolId: string;
  name: string;
  code: string;
  startDate: string; // ISO date string
  endDate: string;   // ISO date string
  isCurrent: boolean;
  isClosed: boolean;
  isActive: boolean;
  notes: string;
  createdOn: string;
  createdBy?: string | null;
  updatedOn?: string | null;
  updatedBy?: string | null;
}

export interface CreateAcademicYearRequest {
  schoolId: string;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  isCurrent?: boolean;
  notes?: string | null;
}

export interface UpdateAcademicYearRequest {
  name?: string | null;
  code?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  isCurrent?: boolean | null;
  notes?: string | null;
}