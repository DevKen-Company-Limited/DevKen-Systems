// types/GradeEnums.ts

export interface SelectOption {
  value: any;
  label: string;
}

// ── GradeLetter (mirrors C# GradeLetter enum) ─────────────────────────────
export const GradeLetterOptions: SelectOption[] = [
  { value: 0,  label: 'A+' },
  { value: 1,  label: 'A'  },
  { value: 2,  label: 'A-' },
  { value: 3,  label: 'B+' },
  { value: 4,  label: 'B'  },
  { value: 5,  label: 'B-' },
  { value: 6,  label: 'C+' },
  { value: 7,  label: 'C'  },
  { value: 8,  label: 'C-' },
  { value: 9,  label: 'D'  },
  { value: 10, label: 'E'  },
  { value: 11, label: 'F'  },
];

// ── AssessmentType (mirrors C# AssessmentType enum used for GradeType) ────
export const GradeTypeOptions: SelectOption[] = [
  { value: 0, label: 'Formative'  },
  { value: 1, label: 'Summative'  },
  { value: 2, label: 'Competency' },
];

/** Resolve GradeLetter to display string */
export function getGradeLetterLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  // API may return the string directly e.g. "A", "B+"
  if (typeof val === 'string' && isNaN(Number(val))) return val;
  const n = Number(val);
  return GradeLetterOptions.find(o => o.value === n)?.label ?? val.toString();
}

/** Resolve GradeType (AssessmentType) to display string */
export function getGradeTypeLabel(val: number | string | undefined | null): string {
  if (val === null || val === undefined || val === '') return '—';
  if (typeof val === 'string' && isNaN(Number(val))) return val;
  const n = Number(val);
  return GradeTypeOptions.find(o => o.value === n)?.label ?? val.toString();
}

/** Resolve raw grade letter string/int → numeric enum for API payload */
export function resolveGradeLetter(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n)) return n;
  const found = GradeLetterOptions.find(o => o.label.toLowerCase() === String(val).toLowerCase());
  return found?.value ?? null;
}

/** Resolve raw grade type string/int → numeric enum for API payload */
export function resolveGradeType(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n)) return n;
  const map: Record<string, number> = { formative: 0, summative: 1, competency: 2 };
  return map[String(val).toLowerCase()] ?? null;
}

/** Get color class for grade letter (for badges) */
export function getGradeLetterColor(letter: string | null | undefined): string {
  if (!letter) return 'gray';
  const l = letter.toUpperCase();
  if (l.startsWith('A')) return 'green';
  if (l.startsWith('B')) return 'blue';
  if (l.startsWith('C')) return 'amber';
  if (l.startsWith('D')) return 'orange';
  return 'red';
}

/** Get color class for percentage */
export function getPercentageColor(pct: number | null | undefined): string {
  if (pct === null || pct === undefined) return 'gray';
  if (pct >= 80) return 'green';
  if (pct >= 70) return 'blue';
  if (pct >= 60) return 'amber';
  if (pct >= 50) return 'orange';
  return 'red';
}

/** Get performance label from percentage */
export function getPerformanceLabel(pct: number | null | undefined): string {
  if (pct === null || pct === undefined) return 'Not Assessed';
  if (pct >= 80) return 'Excellent';
  if (pct >= 70) return 'Very Good';
  if (pct >= 60) return 'Good';
  if (pct >= 50) return 'Average';
  if (pct >= 40) return 'Below Average';
  return 'Needs Improvement';
}