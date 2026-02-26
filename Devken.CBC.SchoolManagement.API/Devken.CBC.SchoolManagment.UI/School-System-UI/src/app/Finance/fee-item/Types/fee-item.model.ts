export enum FeeType {
  Tuition = 0,
  Boarding = 1,
  Meals = 2,
  Transport = 3,
  Activities = 4,
  Uniform = 5,
  Books = 6,
  Examination = 7,
  Other = 8,
}

export enum RecurrenceType {
  None = 0,
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
  Termly = 4,
  Annually = 5,
}

export enum CBCLevel {
  PrePrimary = 0,
  LowerPrimary = 1,
  UpperPrimary = 2,
  JuniorSecondary = 3,
}

export enum ApplicableTo {
  All = 0,
  DayScholars = 1,
  Boarders = 2,
}

export interface FeeItemResponseDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  defaultAmount: number;
  feeType: string;        // numeric string e.g. "0"
  isMandatory: boolean;
  isRecurring: boolean;
  recurrence?: string;
  isTaxable: boolean;
  taxRate?: number;
  glCode?: string;
  isActive: boolean;
  applicableLevel?: string;
  applicableTo?: string;
  displayName: string;
  tenantId: string;
  schoolId: string;
  schoolName?: string;
  status: string;
  createdOn: string;
  updatedOn: string;
}

export interface CreateFeeItemDto {
  name: string;
  description?: string;
  defaultAmount: number;
  feeType: FeeType;
  isMandatory: boolean;
  isRecurring: boolean;
  recurrence?: RecurrenceType;
  isTaxable: boolean;
  taxRate?: number;
  glCode?: string;
  isActive: boolean;
  applicableLevel?: CBCLevel;
  applicableTo?: ApplicableTo;
  tenantId?: string; 
}

export interface UpdateFeeItemDto {
  name: string;
  description?: string;
  defaultAmount: number;
  feeType: FeeType;
  isMandatory: boolean;
  isRecurring: boolean;
  recurrence?: RecurrenceType;
  isTaxable: boolean;
  taxRate?: number;
  glCode?: string;
  isActive: boolean;
  applicableLevel?: CBCLevel;
  applicableTo?: ApplicableTo;
  schoolId?: string;   
}

export const FEE_TYPE_OPTIONS = [
  { value: FeeType.Tuition,    label: 'Tuition' },
  { value: FeeType.Boarding,   label: 'Boarding' },
  { value: FeeType.Meals,      label: 'Meals' },
  { value: FeeType.Transport,  label: 'Transport' },
  { value: FeeType.Activities, label: 'Activities' },
  { value: FeeType.Uniform,    label: 'Uniform' },
  { value: FeeType.Books,      label: 'Books' },
  { value: FeeType.Examination,label: 'Examination' },
  { value: FeeType.Other,      label: 'Other' },
];

export const RECURRENCE_OPTIONS = [
  { value: RecurrenceType.None,     label: 'None' },
  { value: RecurrenceType.Daily,    label: 'Daily' },
  { value: RecurrenceType.Weekly,   label: 'Weekly' },
  { value: RecurrenceType.Monthly,  label: 'Monthly' },
  { value: RecurrenceType.Termly,   label: 'Termly' },
  { value: RecurrenceType.Annually, label: 'Annually' },
];

export const CBC_LEVEL_OPTIONS = [
  { value: CBCLevel.PrePrimary,      label: 'Pre-Primary' },
  { value: CBCLevel.LowerPrimary,    label: 'Lower Primary' },
  { value: CBCLevel.UpperPrimary,    label: 'Upper Primary' },
  { value: CBCLevel.JuniorSecondary, label: 'Junior Secondary' },
];

export const APPLICABLE_TO_OPTIONS = [
  { value: ApplicableTo.All,         label: 'All Students' },
  { value: ApplicableTo.DayScholars, label: 'Day Scholars' },
  { value: ApplicableTo.Boarders,    label: 'Boarders' },
];

export function resolveFeeTypeLabel(value: string | number): string {
  const num = typeof value === 'string' ? parseInt(value) : value;
  return FEE_TYPE_OPTIONS.find(o => o.value === num)?.label ?? '—';
}

export function resolveLevelLabel(value: string | undefined): string {
  if (value == null) return 'All Levels';
  const num = parseInt(value);
  return CBC_LEVEL_OPTIONS.find(o => o.value === num)?.label ?? '—';
}

export function resolveApplicableToLabel(value: string | undefined): string {
  if (value == null) return '—';
  const num = parseInt(value);
  return APPLICABLE_TO_OPTIONS.find(o => o.value === num)?.label ?? '—';
}

export function resolveRecurrenceLabel(value: string | undefined): string {
  if (value == null) return '—';
  const num = parseInt(value);
  return RECURRENCE_OPTIONS.find(o => o.value === num)?.label ?? '—';
}