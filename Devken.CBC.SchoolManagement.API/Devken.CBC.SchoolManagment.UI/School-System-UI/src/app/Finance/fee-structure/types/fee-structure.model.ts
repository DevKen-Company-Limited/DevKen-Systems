// ─────────────────────────────────────────────────────────────────────────────
// Enums — mirror the C# backend enums
// ─────────────────────────────────────────────────────────────────────────────

/** Mirrors CBCLevel enum from the backend */
export enum CBCLevel {
  PP1     = 0,
  PP2     = 1,
  Grade1  = 2,
  Grade2  = 3,
  Grade3  = 4,
  Grade4  = 5,
  Grade5  = 6,
  Grade6  = 7,
  Grade7  = 8,
  Grade8  = 9,
  Grade9  = 10,
  Grade10 = 11,
  Grade11 = 12,
  Grade12 = 13,
}

/** Mirrors ApplicableTo enum from the backend */
export enum ApplicableTo {
  All      = 0,
  Day      = 1,
  Boarding = 2,
  Special  = 3,
}

// ─────────────────────────────────────────────────────────────────────────────
// Option Arrays — for dropdowns
// ─────────────────────────────────────────────────────────────────────────────

export interface SelectOption {
  label: string;
  value: number;
}

export const CBC_LEVEL_OPTIONS: SelectOption[] = [
  { label: 'PP1',      value: CBCLevel.PP1 },
  { label: 'PP2',      value: CBCLevel.PP2 },
  { label: 'Grade 1',  value: CBCLevel.Grade1 },
  { label: 'Grade 2',  value: CBCLevel.Grade2 },
  { label: 'Grade 3',  value: CBCLevel.Grade3 },
  { label: 'Grade 4',  value: CBCLevel.Grade4 },
  { label: 'Grade 5',  value: CBCLevel.Grade5 },
  { label: 'Grade 6',  value: CBCLevel.Grade6 },
  { label: 'Grade 7',  value: CBCLevel.Grade7 },
  { label: 'Grade 8',  value: CBCLevel.Grade8 },
  { label: 'Grade 9',  value: CBCLevel.Grade9 },
  { label: 'Grade 10', value: CBCLevel.Grade10 },
  { label: 'Grade 11', value: CBCLevel.Grade11 },
  { label: 'Grade 12', value: CBCLevel.Grade12 },
];

export const APPLICABLE_TO_OPTIONS: SelectOption[] = [
  { label: 'All Students', value: ApplicableTo.All },
  { label: 'Day',          value: ApplicableTo.Day },
  { label: 'Boarding',     value: ApplicableTo.Boarding },
  { label: 'Special',      value: ApplicableTo.Special },
];

// ─────────────────────────────────────────────────────────────────────────────
// Label helpers
// ─────────────────────────────────────────────────────────────────────────────

export function resolveLevelLabel(level: CBCLevel | null | undefined): string {
  if (level === null || level === undefined) return 'All Levels';
  return CBC_LEVEL_OPTIONS.find(o => o.value === level)?.label ?? `Level ${level}`;
}

export function resolveApplicableToLabel(val: ApplicableTo): string {
  return APPLICABLE_TO_OPTIONS.find(o => o.value === val)?.label ?? 'All';
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs — mirror the C# backend DTOs
// ─────────────────────────────────────────────────────────────────────────────

/** Backend FeeStructureDto (response) */
export interface FeeStructureDto {
  id:               string;
  tenantId:         string;
  feeItemId:        string;
  feeItemName:      string;
  academicYearId:   string;
  academicYearName: string;
  termId:           string | null;
  termName:         string | null;
  level:            CBCLevel | null;
  applicableTo:     ApplicableTo;
  amount:           number;
  maxDiscountPercent: number | null;
  effectiveFrom:    string | null;   // ISO date string
  effectiveTo:      string | null;
  isActive:         boolean;
  createdOn:        string;
  updatedOn:        string;
}

/** POST payload */
export interface CreateFeeStructureDto {
  tenantId?:          string;
  feeItemId:          string;
  academicYearId:     string;
  termId?:            string | null;
  level?:             CBCLevel | null;
  applicableTo:       ApplicableTo;
  amount:             number;
  maxDiscountPercent?: number | null;
  effectiveFrom?:     string | null;
  effectiveTo?:       string | null;
  isActive:           boolean;
}

/** PUT payload */
export interface UpdateFeeStructureDto {
  level?:             CBCLevel | null;
  applicableTo:       ApplicableTo;
  amount:             number;
  maxDiscountPercent?: number | null;
  effectiveFrom?:     string | null;
  effectiveTo?:       string | null;
  isActive:           boolean;
}

// ─────────────────────────────────────────────────────────────────────────────
// Lightweight lookup DTOs used when populating dropdowns
// ─────────────────────────────────────────────────────────────────────────────

export interface FeeItemLookup {
  id:   string;
  name: string;
  code: string;
}

export interface AcademicYearLookup {
  id:   string;
  name: string;
}

export interface TermLookup {
  id:            string;
  name:          string;
  academicYearId: string;
}