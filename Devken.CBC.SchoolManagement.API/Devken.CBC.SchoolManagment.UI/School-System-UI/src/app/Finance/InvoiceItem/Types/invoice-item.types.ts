// ── Models ────────────────────────────────────────────────────────────────────

export interface InvoiceItemResponseDto {
  id: string;
  invoiceId: string;
  feeItemId?: string;
  termId?: string;
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
  effectiveUnitPrice: number;
  glCode?: string;
  notes?: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateInvoiceItemDto {
  invoiceId: string;
  feeItemId?: string | null;
  termId?: string | null;
  description: string;
  itemType?: string | null;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate?: number | null;
  glCode?: string | null;
  notes?: string | null;
  discountOverride?: number | null;
}

export interface UpdateInvoiceItemDto {
  description: string;
  itemType?: string | null;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate?: number | null;
  glCode?: string | null;
  notes?: string | null;
  discountOverride?: number | null;
}

// ── Form model (internal) ─────────────────────────────────────────────────────

export interface InvoiceItemForm {
  description: string;
  itemType: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate: number | null;
  glCode: string;
  notes: string;
  discountOverride: number | null;
}

// ── Panel mode ────────────────────────────────────────────────────────────────

export type PanelMode = 'create' | 'edit' | null;