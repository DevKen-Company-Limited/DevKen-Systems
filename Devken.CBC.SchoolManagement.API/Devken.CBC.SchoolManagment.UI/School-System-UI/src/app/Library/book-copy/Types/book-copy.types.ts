export interface BookCopyDto {
  id: string;
  schoolId: string;
  bookId: string;
  bookTitle: string;
  bookISBN: string;
  libraryBranchId: string;
  libraryBranchName: string;
  accessionNumber: string;
  barcode: string;
  qrCode?: string;
  condition: string;
  isAvailable: boolean;
  isLost: boolean;
  isDamaged: boolean;
  acquiredOn?: string;
  status: string;
}

export interface CreateBookCopyRequest {
  schoolId?: string;
  bookId: string;
  libraryBranchId: string;
  accessionNumber?: string;  // optional — auto-generated if blank
  barcode?: string;          // optional — auto-generated if blank
  qrCode?: string;           // optional — derived from accession number
  condition: string;
  isAvailable: boolean;
  isLost: boolean;
  isDamaged: boolean;
  acquiredOn?: string;
}

export interface UpdateBookCopyRequest {
  libraryBranchId: string;
  accessionNumber: string;
  barcode: string;
  qrCode?: string;
  condition: string;
  isAvailable: boolean;
  isLost: boolean;
  isDamaged: boolean;
  acquiredOn?: string;
}

export interface MarkBookCopyStatusRequest {
  remarks?: string;
}

export const BOOK_CONDITIONS = [
  { value: 'Good',    label: 'Good'    },
  { value: 'Fair',    label: 'Fair'    },
  { value: 'Poor',    label: 'Poor'    },
  { value: 'Damaged', label: 'Damaged' },
];