// ------------------------ DTOs ------------------------
export type NumberSeriesEntityValue = 'AcademicYear'|'Student' | 'Teacher' | 'Invoice' | 'Payment' | 'Assessment' | 'Class'| 'BookAccessionNumber'| 'BookBarcode'  | 'LibraryMember';

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
  { value: 'AcademicYear', label: 'Academic Year Code'},
  { value: 'Student', label: 'Student Number' },
  { value: 'Teacher', label: 'Teacher Number' },
  { value: 'Invoice', label: 'Invoice Number' },
  { value: 'Payment', label: 'Payment Number' },
  { value: 'Assessment', label: 'Assessment Number' },
  { value: 'Class', label: 'Class Code' },
  { value: 'BookAccessionNumber', label: 'Book Accession Number' }, 
  { value: 'BookBarcode',         label: 'Book Barcode'          },
  { value: 'LibraryMember',       label: 'Library Member Number'  }, // ← add



] as const;

export const ENTITY_TYPES_MAP: Record<NumberSeriesEntityValue, string> = {
  AcademicYear: 'Academic Year Code',
  Student: 'Student Number',
  Teacher: 'Teacher Number',
  Invoice: 'Invoice Number',
  Payment: 'Payment Number',
  Assessment: 'Assessment Number',
  Class: 'Class Code',
  BookAccessionNumber: 'Book Accession Number',  
  BookBarcode:         'Book Barcode',  
  LibraryMember:       'Library Member Number', 
};
