// types/SubjectEnums.ts
<<<<<<< HEAD
=======

// Level as returned by API (level field as numeric string "0","1"...)
// Map display labels to the numeric values the API uses
>>>>>>> upstream/main
export interface SelectOption {
  value: any;
  label: string;
}

export const SubjectTypeOptions: SelectOption[] = [
  { value: 1, label: 'Core'          },
  { value: 2, label: 'Optional'      },
  { value: 3, label: 'Elective'      },
  { value: 4, label: 'Co-Curricular' },
];

export const CBCLevelOptions: SelectOption[] = [
  { value: 1,  label: 'Pre-Primary 1' },
  { value: 2,  label: 'Pre-Primary 2' },
  { value: 3,  label: 'Grade 1'       },
  { value: 4,  label: 'Grade 2'       },
  { value: 5,  label: 'Grade 3'       },
  { value: 6,  label: 'Grade 4'       },
  { value: 7,  label: 'Grade 5'       },
  { value: 8,  label: 'Grade 6'       },
  { value: 9,  label: 'Grade 7'       },
  { value: 10, label: 'Grade 8'       },
  { value: 11, label: 'Grade 9'       },
  { value: 12, label: 'Grade 10'      },
  { value: 13, label: 'Grade 11'      },
  { value: 14, label: 'Grade 12'      },
];

<<<<<<< HEAD
// Maps C# enum name → numeric value
const subjectTypeNameMap: Record<string, number> = {
  core: 1, optional: 2, elective: 3,
  cocurricular: 4, extracurricular: 4,
};

const cbcLevelNameMap: Record<string, number> = {
  pp1: 1, preprimary1: 1,
  pp2: 2, preprimary2: 2,
  grade1: 3,  grade2: 4,  grade3: 5,  grade4: 6,  grade5: 7,
  grade6: 8,  grade7: 9,  grade8: 10, grade9: 11, grade10: 12,
  grade11: 13, grade12: 14,
};

/**
 * Resolves any representation the backend might send to a numeric value:
 * - numeric string "3" → 3
 * - enum name "Grade1"  → 3
 * - already a number 3  → 3
 */
export function resolveCBCLevel(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n) && n > 0) return n;
  return cbcLevelNameMap[String(val).toLowerCase()] ?? null;
}

export function resolveSubjectType(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n) && n > 0) return n;
  return subjectTypeNameMap[String(val).toLowerCase()] ?? null;
}

export function getSubjectTypeLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  const resolved = resolveSubjectType(val);
  return SubjectTypeOptions.find(o => o.value === resolved)?.label ?? val.toString();
}

export function getCBCLevelLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  const resolved = resolveCBCLevel(val);
  return CBCLevelOptions.find(o => o.value === resolved)?.label ?? val.toString();
=======
/** Get SubjectType label from numeric level value */
export function getSubjectTypeLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  const n = Number(val);
  return SubjectTypeOptions.find(o => o.value === n)?.label ?? val.toString();
}
/** Get CBCLevel label from numeric level value */
export function getCBCLevelLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  const n = Number(val);
  return CBCLevelOptions.find(o => o.value === n)?.label ?? val.toString();
}

/** Normalize raw subject from API */
export function normalizeSubject(data: any): any {
  if (!data) return {};
  return {
    ...data,
    // subjectType comes as string name — keep as-is, matches option values
    subjectType: data.subjectType ?? '',
    // level comes as string "0", "1" etc. — convert to number for mat-select
    level: data.level !== undefined && data.level !== null ? Number(data.level) : '',
  };
>>>>>>> upstream/main
}