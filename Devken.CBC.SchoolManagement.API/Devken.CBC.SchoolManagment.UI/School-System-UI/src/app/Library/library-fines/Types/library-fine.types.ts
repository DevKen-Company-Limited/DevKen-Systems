// library-fine/Types/library-fine.types.ts

// ── DTOs ─────────────────────────────────────────────────────────────────────

export interface LibraryFineDto {
  id:              string;
  borrowItemId:    string;
  amount:          number;
  isPaid:          boolean;
  isWaived:        boolean;
  issuedOn:        string;
  paidOn?:         string;
  reason?:         string;
  
  // Multi-tenant fields
  schoolId?:       string;
  schoolName?:     string;
  tenantId?:       string;
  
  // Denormalized fields from related entities
  memberName?:     string;
  memberNumber?:   string;
  bookTitle?:      string;
  isbn?:           string;
}

// ── Request Payloads ─────────────────────────────────────────────────────────

export interface CreateLibraryFineRequest {
  borrowItemId: string;
  amount:       number;
  reason:       string;
  issuedOn?:    string;
  schoolId?:    string; // SuperAdmin only
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
  fineId: string;
  reason: string;
}