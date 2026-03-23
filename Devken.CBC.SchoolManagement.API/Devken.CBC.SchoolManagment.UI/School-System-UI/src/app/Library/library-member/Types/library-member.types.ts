export type LibraryMemberType = 'Student' | 'Teacher' | 'Staff' | 'Other';

export interface LibraryMemberDto {
  id: string;
  schoolId: string;
  schoolName?: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  memberNumber: string;
  memberType: LibraryMemberType;
  joinedOn: string;       // ISO date string
  isActive: boolean;
  totalBorrows: number;
}

export interface CreateLibraryMemberRequest {
  schoolId?: string;      // SuperAdmin only
  userId: string;
  memberNumber?: string;
  memberType: LibraryMemberType;
  joinedOn?: string;      // ISO date string, optional — defaults to today on server
  isActive?: boolean; 
}

export interface UpdateLibraryMemberRequest {
  memberNumber: string;
  memberType: LibraryMemberType;
  isActive: boolean;
}