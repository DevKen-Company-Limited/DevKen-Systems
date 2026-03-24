// ── DTOs ─────────────────────────────────────────────────────────────────────
export interface LibraryFineDto {
  id:           string;
  borrowItemId: string;
  bookTitle?:   string;
  memberName?:  string;
  memberId?:    string;
  schoolId?:    string;
  schoolName?:  string;
  amount:       number;
  isPaid:       boolean;
  isWaived?:    boolean;
  issuedOn:     string;
  paidOn?:      string;
  reason?:      string;
}

// ── Request Payloads ─────────────────────────────────────────────────────────
export interface CreateLibraryFineRequest {
  borrowItemId: string;
  amount:       number;
  reason:       string;
  issuedOn?:    string;
  schoolId?:    string;
}

export interface PayFineRequest {
  fineId:       string;
  paymentDate?: string;
}

export interface PayMultipleFinesRequest {
  fineIds:      string[];
  paymentDate?: string;
}

export interface WaiveFineRequest {
  fineId:  string;
  reason:  string;
}