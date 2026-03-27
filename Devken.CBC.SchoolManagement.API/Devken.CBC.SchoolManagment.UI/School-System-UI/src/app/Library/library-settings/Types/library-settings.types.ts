// app/Library/library-settings/Types/library-settings.types.ts
export interface LibrarySettingsDto {
  id: string;
  schoolId: string;
  maxBooksPerStudent: number;
  maxBooksPerTeacher: number;
  borrowDaysStudent: number;
  borrowDaysTeacher: number;
  finePerDay: number;
  allowBookReservation: boolean;
}

export interface UpsertLibrarySettingsRequest {
  schoolId?: string;
  maxBooksPerStudent: number;
  maxBooksPerTeacher: number;
  borrowDaysStudent: number;
  borrowDaysTeacher: number;
  finePerDay: number;
  allowBookReservation: boolean;
}