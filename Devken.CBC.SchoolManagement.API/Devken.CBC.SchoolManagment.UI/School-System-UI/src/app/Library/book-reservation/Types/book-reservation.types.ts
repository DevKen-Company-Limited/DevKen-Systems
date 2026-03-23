export interface BookReservationDto {
  id: string;
  schoolId: string;
  schoolName?: string;
  bookId: string;
  bookTitle: string;
  memberId: string;
  memberName: string;
  reservedOn: string;       // ISO date string
  isFulfilled: boolean;
}

export interface CreateBookReservationRequest {
  schoolId?: string;        // SuperAdmin only
  bookId: string;
  memberId: string;
}

export interface UpdateBookReservationRequest {
  bookId: string;
  memberId: string;
  isFulfilled: boolean;
}