// app/Library/library-fee/Types/library-fee.types.ts
export enum LibraryFeeType {
  MembershipFee  = 'MembershipFee',
  LateFine       = 'LateFine',
  DamageFee      = 'DamageFee',
  LostBookFee    = 'LostBookFee',
  ProcessingFee  = 'ProcessingFee',
  Other          = 'Other',
}

export enum LibraryFeeStatus {
  Unpaid        = 'Unpaid',
  Paid          = 'Paid',
  Waived        = 'Waived',
  PartiallyPaid = 'PartiallyPaid',
}

export interface LibraryFeeDto {
  id:               string;
  schoolId:         string;
  memberId:         string;
  memberNumber:     string;
  userFullName:   string;
  bookBorrowId?:    string;
  feeType:          LibraryFeeType;
  feeTypeDisplay:   string;
  amount:           number;
  amountPaid:       number;
  balance:          number;
  feeStatus:        LibraryFeeStatus;
  feeStatusDisplay: string;
  description:      string;
  feeDate:          string;
  paidOn?:          string;
  waivedReason?:    string;
}

export interface CreateLibraryFeeRequest {
  schoolId?:    string;
  memberId:     string;
  bookBorrowId?: string;
  feeType:      LibraryFeeType;
  amount:       number;
  description:  string;
  feeDate?:     string;
}

export interface UpdateLibraryFeeRequest {
  feeType:     LibraryFeeType;
  amount:      number;
  description: string;
  feeDate?:    string;
}

export interface RecordLibraryFeePaymentRequest {
  amountPaid: number;
  paidOn?:    string;
}

export interface WaiveLibraryFeeRequest {
  reason: string;
}

export interface LibraryFeeFilterRequest {
  schoolId?:  string;
  memberId?:  string;
  feeStatus?: LibraryFeeStatus;
  feeType?:   LibraryFeeType;
  fromDate?:  string;
  toDate?:    string;
}