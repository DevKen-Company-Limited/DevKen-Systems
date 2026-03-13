// ═══════════════════════════════════════════════════════════════════
// payments.ts  — Types, enums and helpers
// Mirrors the C# DTOs in Payments DTOs file
// ═══════════════════════════════════════════════════════════════════

// ── Enums (string-based, matching C# enum names) ─────────────────

export type PaymentMethod =
  | 'Cash'
  | 'Mpesa'
  | 'BankTransfer'
  | 'Cheque'
  | 'Card'
  | 'Online';

export type PaymentStatus =
  | 'Pending'
  | 'Completed'
  | 'Failed'
  | 'Refunded'
  | 'Cancelled'
  | 'Reversed';

// ── Enum → integer maps (sent to API) ────────────────────────────

export const PaymentMethodValue: Record<PaymentMethod, number> = {
  Cash:         0,
  Mpesa:        1,
  BankTransfer: 2,
  Cheque:       3,
  Card:         4,  // CreditCard = 4
  Online:       5,  // OnlinePortal = 5
};

export const PaymentStatusValue: Record<PaymentStatus, number> = {
  Pending:   0,
  Completed: 1,
  Failed:    2,
  Refunded:  3,
  Cancelled: 4,
  Reversed:  5,
};
export interface PaymentPagedResultDto {
    schoolCount: string;
    items:              PaymentResponseDto[];
    totalCount:         number;
    page:               number;
    pageSize:           number;
    totalPages:         number;
    totalCollected:     number;
    totalReversed:      number;
    netAvailable:       number;
    pendingCount:       number;
    mpesaCount:         number;
    totalReversalCount: number;
}
// ── Response DTO ──────────────────────────────────────────────────

export interface PaymentResponseDto {
  id:                    string;
  tenantId:              string;

  paymentReference:      string;
  receiptNumber?:        string;

  studentId:             string;
  studentName?:          string;
  admissionNumber?:      string;

  invoiceId:             string;
  invoiceNumber?:        string;

  receivedBy?:           string;
  receivedByName?:       string;

  paymentDate:           string;
  receivedDate?:         string;
  amount:                number;
  paymentMethod:         PaymentMethod;
  statusPayment:         PaymentStatus;
  transactionReference?: string;
  description?:          string;
  notes?:                string;

  mpesaCode?:            string;
  phoneNumber?:          string;

  bankName?:             string;
  accountNumber?:        string;
  chequeNumber?:         string;
  chequeClearanceDate?:  string;

  reversedFromPaymentId?: string;
  isReversal:            boolean;
  reversalReason?:       string;

  isCompleted:           boolean;
  isMpesa:               boolean;

  createdOn:             string;
  updatedOn:             string;
  createdBy?:            string;
  updatedBy?:            string;
}

// ── Create DTO ────────────────────────────────────────────────────

export interface CreatePaymentDto {
  tenantId?:             string;

  studentId:             string;
  invoiceId:             string;
  receivedBy?:           string;

  paymentDate:           string;
  receivedDate?:         string;

  amount:                number;
  paymentMethod:         number;  // integer sent to API
  statusPayment?:        number;  // integer sent to API

  transactionReference?: string;
  description?:          string;
  notes?:                string;

  mpesaCode?:            string;
  phoneNumber?:          string;

  bankName?:             string;
  accountNumber?:        string;
  chequeNumber?:         string;
  chequeClearanceDate?:  string;
}

// ── Update DTO (all fields optional — partial update) ─────────────

export interface UpdatePaymentDto {
  paymentDate?:          string;
  receivedDate?:         string;

  amount?:               number;
  paymentMethod?:        number;  // integer sent to API
  statusPayment?:        number;  // integer sent to API
  receivedBy?:           string;

  transactionReference?: string;
  description?:          string;
  notes?:                string;

  mpesaCode?:            string;
  phoneNumber?:          string;

  bankName?:             string;
  accountNumber?:        string;
  chequeNumber?:         string;
  chequeClearanceDate?:  string;
}

// ── Reversal DTO ──────────────────────────────────────────────────

export interface ReversePaymentDto {
  reversalReason: string;
  receivedBy?:    string;
}

// ── Bulk DTO ──────────────────────────────────────────────────────

export interface BulkPaymentItemDto {
  studentId:             string;
  invoiceId:             string;
  amount:                number;
  mpesaCode?:            string;
  phoneNumber?:          string;
  transactionReference?: string;
  notes?:                string;
}

export interface BulkPaymentDto {
  tenantId?:      string;
  paymentDate:    string;
  paymentMethod:  number;   // integer sent to API
  statusPayment?: number;   // integer sent to API
  receivedBy?:    string;
  description?:   string;
  bankName?:      string;
  accountNumber?: string;
  payments:       BulkPaymentItemDto[];
}

// ── Bulk Result ───────────────────────────────────────────────────

export interface BulkPaymentResultDto {
  totalRequested:    number;
  succeeded:         number;
  failed:            number;
  totalAmountPosted: number;
  createdPayments:   PaymentResponseDto[];
  errors:            BulkPaymentErrorDto[];
}

export interface BulkPaymentErrorDto {
  studentId: string;
  invoiceId: string;
  reason:    string;
}

// ── Summary DTO ───────────────────────────────────────────────────

export interface PaymentSummaryDto {
  totalPayments:   number;
  totalAmount:     number;
  completedCount:  number;
  completedAmount: number;
  pendingCount:    number;
  pendingAmount:   number;
  reversalCount:   number;
  reversalAmount:  number;
  byMethod:        { method: PaymentMethod; count: number; amount: number }[];
}

// ── Label / colour helpers ────────────────────────────────────────

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  Cash:         'Cash',
  Mpesa:        'M-Pesa',
  BankTransfer: 'Bank Transfer',
  Cheque:       'Cheque',
  Card:         'Card',
  Online:       'Online',
};

export const PAYMENT_METHOD_ICONS: Record<PaymentMethod, string> = {
  Cash:         'payments',
  Mpesa:        'phone_android',
  BankTransfer: 'account_balance',
  Cheque:       'description',
  Card:         'credit_card',
  Online:       'language',
};

export const PAYMENT_METHOD_COLORS: Record<PaymentMethod, string> = {
  Cash:         'emerald',
  Mpesa:        'green',
  BankTransfer: 'blue',
  Cheque:       'amber',
  Card:         'violet',
  Online:       'indigo',
};

export const PAYMENT_STATUS_COLORS: Record<PaymentStatus, string> = {
  Pending:   'amber',
  Completed: 'green',
  Failed:    'red',
  Refunded:  'blue',
  Cancelled: 'gray',
  Reversed:  'rose',
};

export function getPaymentMethodLabel(method: string): string {
  return PAYMENT_METHOD_LABELS[method as PaymentMethod] ?? method ?? 'Unknown';
}

export function getPaymentStatusLabel(status: string): string {
  return status ?? 'Unknown';
}