// ------------------------ DTOs ------------------------
export type NumberSeriesEntityValue = 'Student' | 'Teacher' | 'Invoice' | 'Payment' | 'Assessment' | 'Class';

export interface DocumentNumberSeriesDto {
  id: string;
  tenantId: string;
  entityName: NumberSeriesEntityValue;
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
  lastNumber: number;
  lastGeneratedYear: number;
}

export interface CreateDocumentNumberSeriesRequest {
  tenantId: string;
  entityName: NumberSeriesEntityValue;
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
}

export interface UpdateDocumentNumberSeriesRequest {
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
}

// ------------------------ ENTITY TYPES ------------------------
export const ENTITY_TYPES: { value: NumberSeriesEntityValue; label: string }[] = [
  { value: 'Student', label: 'Student Number' },
  { value: 'Teacher', label: 'Teacher Number' },
  { value: 'Invoice', label: 'Invoice Number' },
  { value: 'Payment', label: 'Payment Number' },
  { value: 'Assessment', label: 'Assessment Number' },
  { value: 'Class', label: 'Class Code' },
] as const;

export const ENTITY_TYPES_MAP: Record<NumberSeriesEntityValue, string> = {
  Student: 'Student Number',
  Teacher: 'Teacher Number',
  Invoice: 'Invoice Number',
  Payment: 'Payment Number',
  Assessment: 'Assessment Number',
  Class: 'Class Code',
};
