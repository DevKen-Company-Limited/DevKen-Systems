// types/SubjectEnums.ts

export enum SubjectType {
  Core = 1,
  Optional = 2,
  Elective = 3,
  Extracurricular = 4,
}

export enum CBCLevel {
  PrePrimary1 = 1,
  PrePrimary2 = 2,
  Grade1 = 3,
  Grade2 = 4,
  Grade3 = 5,
  Grade4 = 6,
  Grade5 = 7,
  Grade6 = 8,
  Grade7 = 9,
  Grade8 = 10,
  Grade9 = 11,
  Grade10 = 12,
  Grade11 = 13,
  Grade12 = 14,
}

export interface SelectOption {
  value: number;
  label: string;
}

export const SubjectTypeOptions: SelectOption[] = [
  { value: SubjectType.Core,            label: 'Core'            },
  { value: SubjectType.Optional,        label: 'Optional'        },
  { value: SubjectType.Elective,        label: 'Elective'        },
  { value: SubjectType.Extracurricular, label: 'Extracurricular' },
];

export const CBCLevelOptions: SelectOption[] = [
  { value: CBCLevel.PrePrimary1, label: 'Pre-Primary 1' },
  { value: CBCLevel.PrePrimary2, label: 'Pre-Primary 2' },
  { value: CBCLevel.Grade1,      label: 'Grade 1'       },
  { value: CBCLevel.Grade2,      label: 'Grade 2'       },
  { value: CBCLevel.Grade3,      label: 'Grade 3'       },
  { value: CBCLevel.Grade4,      label: 'Grade 4'       },
  { value: CBCLevel.Grade5,      label: 'Grade 5'       },
  { value: CBCLevel.Grade6,      label: 'Grade 6'       },
  { value: CBCLevel.Grade7,      label: 'Grade 7'       },
  { value: CBCLevel.Grade8,      label: 'Grade 8'       },
  { value: CBCLevel.Grade9,      label: 'Grade 9'       },
  { value: CBCLevel.Grade10,     label: 'Grade 10'      },
  { value: CBCLevel.Grade11,     label: 'Grade 11'      },
  { value: CBCLevel.Grade12,     label: 'Grade 12'      },
];

/** Convert any raw API value (string | number) → number or null */
export function toNumber(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = typeof val === 'string' ? parseInt(val, 10) : val;
  return isNaN(n) ? null : n;
}

/** Lookup SubjectType label from numeric value */
export function getSubjectTypeLabel(val: number | string | undefined): string {
  if (val === undefined || val === null) return '—';
  const n = typeof val === 'string' ? parseInt(val, 10) : val;
  return SubjectTypeOptions.find(o => o.value === n)?.label ?? val.toString();
}

/** Lookup CBCLevel label from numeric value */
export function getCBCLevelLabel(val: number | string | undefined): string {
  if (val === undefined || val === null) return '—';
  const n = typeof val === 'string' ? parseInt(val, 10) : val;
  return CBCLevelOptions.find(o => o.value === n)?.label ?? val.toString();
}

/** Normalize raw subject data coming from the API */
export function normalizeSubject(data: any): any {
  if (!data) return {};
  return {
    ...data,
    subjectType: toNumber(data.subjectType),
    cbcLevel:    toNumber(data.cbcLevel),
  };
}