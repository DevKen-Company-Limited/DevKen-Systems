/**
 * CBC Level Enum - matching backend
 */
export const CBCLevel = {
  PrePrimary1: 0,
  PrePrimary2: 1,
  Grade1: 2,
  Grade2: 3,
  Grade3: 4,
  Grade4: 5,
  Grade5: 6,
  Grade6: 7,
  JuniorSecondary1: 8,
  JuniorSecondary2: 9,
  JuniorSecondary3: 10,
  SeniorSecondary1: 11,
  SeniorSecondary2: 12,
  SeniorSecondary3: 13
} as const;

export type CBCLevel = typeof CBCLevel[keyof typeof CBCLevel];

// ==================== API Response Types ====================

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

// ==================== Class DTOs ====================

export interface ClassDto {
  id: string;
  schoolId: string;
  name: string;
  code: string;
  level: number; // CBCLevel
  levelName: string;
  description?: string;
  capacity: number;
  currentEnrollment: number;
  availableSeats: number;
  isFull: boolean;
  isActive: boolean;
  academicYearId: string;
  academicYearName?: string;
  academicYearCode?: string;
  teacherId?: string;
  teacherName?: string;
  createdOn: Date;
  createdBy?: string;
  updatedOn?: Date;
  updatedBy?: string;
}

export interface ClassDetailDto extends ClassDto {
  studentCount: number;
  subjectCount: number;
}

export interface CreateClassRequest {
  schoolId?: string;
  name: string;
  code: string;
  level: number; // CBCLevel
  description?: string;
  capacity: number;
  academicYearId: string;
  teacherId?: string;
  isActive: boolean;
}

export interface UpdateClassRequest {
  name?: string;
  code?: string;
  level?: number;
  description?: string;
  capacity?: number;
  academicYearId?: string;
  teacherId?: string;
  isActive?: boolean;
}

// ==================== Helper Types ====================

export interface AcademicYearOption {
  id: string;
  name: string;
  code: string;
}

export interface TeacherOption {
  id: string;
  name: string;
  teacherNumber: string;
}

export interface ClassStats {
  totalClasses: number;
  activeClasses: number;
  inactiveClasses: number;
  fullClasses: number;
  availableCapacity: number;
}

// ==================== Utility Functions ====================

export function getCBCLevelDisplay(level: number): string {
  const levelMap: { [key: number]: string } = {
    [CBCLevel.PrePrimary1]: 'Pre-Primary 1',
    [CBCLevel.PrePrimary2]: 'Pre-Primary 2',
    [CBCLevel.Grade1]: 'Grade 1',
    [CBCLevel.Grade2]: 'Grade 2',
    [CBCLevel.Grade3]: 'Grade 3',
    [CBCLevel.Grade4]: 'Grade 4',
    [CBCLevel.Grade5]: 'Grade 5',
    [CBCLevel.Grade6]: 'Grade 6',
    [CBCLevel.JuniorSecondary1]: 'Junior Secondary 1',
    [CBCLevel.JuniorSecondary2]: 'Junior Secondary 2',
    [CBCLevel.JuniorSecondary3]: 'Junior Secondary 3',
    [CBCLevel.SeniorSecondary1]: 'Senior Secondary 1',
    [CBCLevel.SeniorSecondary2]: 'Senior Secondary 2',
    [CBCLevel.SeniorSecondary3]: 'Senior Secondary 3'
  };
  return levelMap[level] || `Level ${level}`;
}

export function getAllCBCLevels(): { value: number; label: string }[] {
  return Object.entries(CBCLevel).map(([key, value]) => ({
    value,
    label: getCBCLevelDisplay(value)
  }));
}