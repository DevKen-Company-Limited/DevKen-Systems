// book-recommendation/Types/book-recommendation.types.ts

// ── DTOs ─────────────────────────────────────────────────────────────────────

export interface BookRecommendationDto {
  id:          string;
  schoolId:    string;
  bookId:      string;
  bookTitle:   string;
  studentId:   string;
  studentName: string;
  score:       number;
  reason:      string;
  createdOn:   string;
  createdBy?:  string;
  updatedOn?:  string;
  updatedBy?:  string;
  
  // Additional denormalized fields
  isbn?:         string;
  authorName?:   string;
  categoryName?: string;
  studentNumber?: string;
  schoolName?:   string;
}

// ── Request Payloads ─────────────────────────────────────────────────────────

export interface CreateBookRecommendationRequest {
  schoolId:  string;
  bookId:    string;
  studentId: string;
  score:     number;
  reason:    string;
}

export interface UpdateBookRecommendationRequest {
  score?:  number;
  reason?: string;
}

export interface GenerateRecommendationsRequest {
  schoolId:          string;
  studentId:         string;
  maxRecommendations: number;
}