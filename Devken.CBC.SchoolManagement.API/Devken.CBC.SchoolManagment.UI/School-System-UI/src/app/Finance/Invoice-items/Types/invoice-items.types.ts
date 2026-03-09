// ── Response ──────────────────────────────────────────────────────────────────

export interface InvoiceItemResponseDto {
    schoolId: string;
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

// ── Create ────────────────────────────────────────────────────────────────────

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
    tenantId?: string; 
}

// ── Update ────────────────────────────────────────────────────────────────────

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

// ── Dialog data ───────────────────────────────────────────────────────────────

export interface InvoiceItemDialogData {
    mode: 'create' | 'edit';
    invoiceId: string;
    item?: InvoiceItemResponseDto;
}

// ── Misc ──────────────────────────────────────────────────────────────────────

export const ITEM_TYPE_OPTIONS: string[] = [
    'Tuition', 'Transport', 'Meals', 'Accommodation',
    'Activity', 'Uniform', 'Books', 'Exam', 'Medical', 'Other',
];
