// term.types.ts
export interface TermDto {
  id: string;
  schoolId: string;
  schoolName: string;
  name: string;
  termNumber: number;
  academicYearId: string;
  academicYearName: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
  isActive: boolean;
  notes: string;
  durationDays: number;
  status: string;
}

export interface CreateTermRequest {
  schoolId?: string; // Required for SuperAdmin only
  name: string;
  termNumber: number;
  academicYearId: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
  notes?: string;
}

export interface UpdateTermRequest {
  name: string;
  termNumber: number;
  academicYearId: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
  notes?: string;
}

export interface SetCurrentTermRequest {
  termId: string;
}

export interface CloseTermRequest {
  termId: string;
  remarks?: string;
}