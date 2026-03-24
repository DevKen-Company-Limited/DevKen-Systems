import { LibraryFineDto } from "app/Library/library-fines/Types/library-fine.types";


// ── Enums / Constants ────────────────────────────────────────────────────────
export type BorrowStatus = 'Borrowed' | 'Returned' | 'Overdue';

export const BORROW_STATUSES: { label: string; value: BorrowStatus }[] = [
  { label: 'Borrowed', value: 'Borrowed' },
  { label: 'Returned', value: 'Returned' },
  { label: 'Overdue',  value: 'Overdue'  },
];

// ── DTOs ─────────────────────────────────────────────────────────────────────
export interface BookBorrowItemDto {
  id:              string;
  borrowId:        string;
  bookCopyId:      string;
  bookTitle:       string;
  isbn:            string;
  accessionNumber: string;
  barcode:         string;
  returnedOn?:     string;
  isReturned:      boolean;
  isOverdue:       boolean;
  daysOverdue:     number;
  fines:           LibraryFineDto[];
}

export interface BookBorrowDto {
  id:               string;
  memberId:         string;
  memberName:       string;
  memberNumber:     string;
  schoolId?:        string;
  schoolName?:      string;
  borrowDate:       string;
  dueDate:          string;
  borrowStatus:     BorrowStatus;
  isOverdue:        boolean;
  totalItems:       number;
  returnedItems:    number;
  unreturnedItems:  number;
  totalFines:       number;
  items:            BookBorrowItemDto[];
}

// ── Request Payloads ─────────────────────────────────────────────────────────
export interface CreateBookBorrowRequest {
  memberId:     string;
  borrowDate:   string;
  dueDate:      string;
  bookCopyIds:  string[];
  schoolId?:    string;
}

export interface UpdateBookBorrowRequest {
  dueDate?: string;
}

export interface ReturnBookRequest {
  borrowItemId: string;
  returnDate?:  string;
}

export interface ReturnMultipleBooksRequest {
  borrowItemIds: string[];
  returnDate?:   string;
}