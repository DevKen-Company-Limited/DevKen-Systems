export interface BookCopyDto {
  id: string;
  bookId: string;
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
}

export interface BookDto {
  id: string;
  schoolId: string;
  schoolName: string;
  title: string;
  isbn: string;
  categoryId: string;
  categoryName: string;
  authorId: string;
  authorName: string;
  publisherId: string;
  publisherName: string;
  publicationYear: number;
  language?: string;
  description?: string;
  totalCopies: number;
  availableCopies: number;
  copies: BookCopyDto[];
}

export interface CreateBookRequest {
  schoolId?: string;
  title: string;
  isbn: string;
  categoryId: string;
  authorId: string;
  publisherId: string;
  publicationYear: number;
  language?: string;
  description?: string;
}

export interface UpdateBookRequest {
  title: string;
  isbn: string;
  categoryId: string;
  authorId: string;
  publisherId: string;
  publicationYear: number;
  language?: string;
  description?: string;
}