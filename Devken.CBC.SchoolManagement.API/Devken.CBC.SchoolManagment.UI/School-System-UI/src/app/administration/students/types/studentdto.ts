export interface StudentDto {
  academicYearName: string | number;
  AcademicYearName: string | number;
  // ── Core Identity ────────────────────────────────────────────────────────
  id: string;
  schoolId: string;
  schoolName?: string;
  
  // ── Personal Information ─────────────────────────────────────────────────
  firstName: string;
  lastName: string;
  middleName?: string;
  fullName?: string; // Computed on backend
  
  gender: string | number; // Enum value
  dateOfBirth: string; // ISO date
  religion?: string;
  nationality?: string;
  photoUrl?: string;
  
  // ── Identification Numbers ───────────────────────────────────────────────
  admissionNumber: string;
  nemisNumber?: number;
  birthCertificateNumber?: string;
  
  // ── Location & Background ────────────────────────────────────────────────
  placeOfBirth?: string;
  county?: string;
  subCounty?: string;
  homeAddress?: string;
  
  // ── Academic Details ─────────────────────────────────────────────────────
  dateOfAdmission: string; // ISO date
  studentStatus: string | number; // Enum value
  cbcLevel: string | number; // Enum value
  currentLevel?: string;
  currentClassId?: string;
  currentClassName?: string;
  currentAcademicYearId?: string;
  previousSchool?: string;
  
  // ── Medical & Health ─────────────────────────────────────────────────────
  bloodGroup?: string;
  medicalConditions?: string;
  allergies?: string;
  specialNeeds?: string;
  requiresSpecialSupport?: boolean;
  
  // ── Guardian Information ─────────────────────────────────────────────────
  // Primary Guardian
  primaryGuardianName: string;
  primaryGuardianRelationship: string;
  primaryGuardianPhone: string;
  primaryGuardianEmail?: string;
  primaryGuardianOccupation?: string;
  primaryGuardianAddress?: string;
  
  // Secondary Guardian (Optional)
  secondaryGuardianName?: string;
  secondaryGuardianRelationship?: string;
  secondaryGuardianPhone?: string;
  secondaryGuardianEmail?: string;
  secondaryGuardianOccupation?: string;
  
  // Emergency Contact
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  
  // ── Status & Metadata ────────────────────────────────────────────────────
  isActive: boolean;
  notes?: string;
  
  // Audit fields
  createdAt?: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface CreateStudentRequest extends Omit<StudentDto, 'id' | 'fullName' | 'createdAt' | 'updatedAt' | 'createdBy' | 'updatedBy'> {}

export interface UpdateStudentRequest extends Partial<CreateStudentRequest> {}