export interface DocumentNumberSeriesDto {
  id: string;
  tenantId: string;
  entityName: string;
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
  lastNumber: number;
  lastGeneratedYear: number;
}

export interface CreateDocumentNumberSeriesRequest {
  tenantId: string;
  entityName: string;
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
}

export interface UpdateDocumentNumberSeriesRequest {
  prefix: string;
  padding: number;
  resetEveryYear: boolean;
}

// Available entity types for number series
export const ENTITY_TYPES = [
  { value: 'Student', label: 'Student Number' },
  { value: 'Teacher', label: 'Teacher Number' },
  { value: 'Invoice', label: 'Invoice Number' },
  { value: 'Payment', label: 'Payment Number' },
  { value: 'Assessment', label: 'Assessment Number' },
  { value: 'Class', label: 'Class Code' },
] as const;