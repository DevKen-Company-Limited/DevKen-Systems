export interface LibraryBranchDto {
  id: string;
  schoolId: string;
  schoolName: string;
  name: string;
  location?: string;
  totalCopies: number;
  availableCopies: number;
}

export interface CreateLibraryBranchRequest {
  schoolId?: string;
  name: string;
  location?: string;
}

export interface UpdateLibraryBranchRequest {
  name: string;
  location?: string;
}