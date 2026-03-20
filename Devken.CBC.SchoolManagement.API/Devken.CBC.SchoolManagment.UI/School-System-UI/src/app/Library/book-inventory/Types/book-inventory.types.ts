export interface BookInventoryDto {
  id: string;
  schoolId: string;
  bookId: string;
  bookTitle: string;
  bookISBN: string;
  authorName: string;
  categoryName: string;
  totalCopies: number;
  availableCopies: number;
  borrowedCopies: number;
  lostCopies: number;
  damagedCopies: number;
  availabilityPercentage: number;
}

export interface CreateBookInventoryRequest {
  schoolId?: string;
  bookId: string;
  totalCopies: number;
  availableCopies: number;
  borrowedCopies: number;
  lostCopies: number;
  damagedCopies: number;
}

export interface UpdateBookInventoryRequest {
  totalCopies: number;
  availableCopies: number;
  borrowedCopies: number;
  lostCopies: number;
  damagedCopies: number;
}