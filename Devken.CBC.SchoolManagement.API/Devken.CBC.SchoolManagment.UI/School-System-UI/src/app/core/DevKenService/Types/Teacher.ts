export interface TeacherDto {
  id: string;
  schoolId: string;
  firstName: string;
  middleName: string;
  lastName: string;
  fullName: string;
  displayName: string;
  teacherNumber: string;
  dateOfBirth: string | null;
  age: number | null;
  gender: string;
  tscNumber: string;
  nationality: string;
  idNumber: string;
  phoneNumber: string;
  email: string;
  address: string;
  employmentType: string;
  designation: string;
  qualification: string;
  specialization: string;
  dateOfEmployment: string | null;
  isClassTeacher: boolean;
  currentClassId: string | null;
  currentClassName: string;
  photoUrl: string;
  isActive: boolean;
  notes: string;
}

export interface CreateTeacherRequest {
  schoolId: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  teacherNumber: string;
  dateOfBirth?: string | null;
  gender: number;
  tscNumber?: string;
  nationality?: string;
  idNumber?: string;
  phoneNumber?: string;
  email?: string;
  address?: string;
  employmentType: number;
  designation: number;
  qualification?: string;
  specialization?: string;
  dateOfEmployment?: string | null;
  isClassTeacher: boolean;
  currentClassId?: string | null;
  photoUrl?: string;
  isActive: boolean;
  notes?: string;
}

export interface UpdateTeacherRequest extends Omit<CreateTeacherRequest, 'schoolId'> {}