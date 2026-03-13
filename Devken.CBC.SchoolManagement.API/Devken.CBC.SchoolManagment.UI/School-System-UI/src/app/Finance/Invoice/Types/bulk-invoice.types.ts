// ── Bulk Invoice Types ─────────────────────────────────────────────────────

export interface BulkInvoiceStudentRow {
  studentId: string;
  studentName: string;
  admissionNumber?: string;
  selected: boolean;
  status: 'pending' | 'success' | 'error' | 'skipped';
  errorMessage?: string;
  invoiceId?: string;
}

export interface BulkInvoiceFeeItem {
  id: string;
  description: string;
  itemType?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  isTaxable: boolean;
  taxRate?: number;
  glCode?: string;
  notes?: string;
  feeItemId?: string;
}

export interface BulkInvoiceFormData {
  // Step 0 – Scope
  tenantId?: string;
  classId: string;
  academicYearId: string;
  termId?: string;
  invoiceDate: Date | string;
  dueDate: Date | string;
  description?: string;

  // Step 1 – Students
  students: BulkInvoiceStudentRow[];

  // Step 2 – Fee Items
  feeItems: BulkInvoiceFeeItem[];

  // Step 3 – Notes
  notes?: string;
}

export interface CreateBulkInvoiceDto {
  tenantId?: string;
  academicYearId: string;
  termId?: string;
  invoiceDate: string;
  dueDate: string;
  description?: string;
  notes?: string;
  studentIds: string[];
  items: {
    feeItemId?: string;
    description: string;
    itemType?: string;
    quantity: number;
    unitPrice: number;
    discount: number;
    isTaxable: boolean;
    taxRate?: number;
    glCode?: string;
    notes?: string;
  }[];
}

export interface BulkInvoiceResult {
  studentId: string;
  studentName: string;
  success: boolean;
  invoiceId?: string;
  invoiceNumber?: string;
  errorMessage?: string;
}

export interface ClassDto {
  id: string;
  name: string;
  grade?: string;
  stream?: string;
}