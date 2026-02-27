// ── Enums ──────────────────────────────────────────────────────────────────
export enum InvoiceStatus {
  Draft     = 0,
  Issued    = 1,
  PartiallyPaid = 2,
  Paid      = 3,
  Overdue   = 4,
  Cancelled = 5,
}

// ── Request DTOs ───────────────────────────────────────────────────────────
export interface CreateInvoiceItemDto {
  description: string;
  itemType?: string;
  feeItemId?: string;
  termId?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate?: number;
  glCode?: string;
  notes?: string;
}

export interface CreateInvoiceDto {
  studentId: string;
  academicYearId: string;
  termId?: string;
  parentId?: string;
  invoiceDate: string;
  dueDate: string;
  description?: string;
  notes?: string;
  tenantId?: string;
  items: CreateInvoiceItemDto[];
}

export interface UpdateInvoiceDto {
  parentId?: string;
  dueDate: string;
  description?: string;
  notes?: string;
}

export interface ApplyDiscountDto {
  discountAmount: number;
}

// ── Response DTOs ──────────────────────────────────────────────────────────
export interface InvoiceItemResponseDto {
  id: string;
  feeItemId?: string;
  description: string;
  itemType?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate?: number;
  total: number;
  taxAmount: number;
  netAmount: number;
  glCode?: string;
  notes?: string;
}

export interface InvoiceResponseDto {
  id: string;
  tenantId: string;
  invoiceNumber: string;
  studentId: string;
  studentName: string;
  academicYearId: string;
  academicYearName: string;
  termId?: string;
  termName?: string;
  parentId?: string;
  parentName?: string;
  invoiceDate: string;
  dueDate: string;
  description?: string;
  totalAmount: number;
  discountAmount: number;
  amountPaid: number;
  balance: number;
  statusInvoice: InvoiceStatus;
  statusDisplay: string;
  isOverdue: boolean;
  notes?: string;
  status: string;
  createdOn: string;
  updatedOn: string;
  items: InvoiceItemResponseDto[];
}

export interface InvoiceSummaryResponseDto {
  id: string;
  invoiceNumber: string;
  studentId: string;
  studentName: string;
  invoiceDate: string;
  dueDate: string;
  totalAmount: number;
  amountPaid: number;
  balance: number;
  statusInvoice: InvoiceStatus;
  statusDisplay: string;
  isOverdue: boolean;
  termName?: string;
  status: string;
}

// ── Query DTO ──────────────────────────────────────────────────────────────
export interface InvoiceQueryDto {
  studentId?: string;
  parentId?: string;
  academicYearId?: string;
  termId?: string;
  invoiceStatus?: InvoiceStatus;
  isOverdue?: boolean;
  dateFrom?: string;
  dateTo?: string;
  isActive?: boolean;
}

// ── Dialog Data ────────────────────────────────────────────────────────────
export interface InvoiceDialogData {
  mode: 'create' | 'edit' | 'view';
  invoice?: InvoiceResponseDto;
}