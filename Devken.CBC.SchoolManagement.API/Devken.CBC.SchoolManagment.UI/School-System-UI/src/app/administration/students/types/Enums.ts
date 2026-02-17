/**
 * Enum Utilities for Student Management
 * CRITICAL: These values MUST match the backend exactly!
 * Backend enums start at 1, not 0!
 */

// ─── Enum Definitions (MUST match backend Domain.Enums) ──────────────────────

export enum Gender {
  Male = 1,
  Female = 2,
  Other = 3,
}

export enum StudentStatus {
  Active = 1,
  Inactive = 2,
  Transferred = 3,
  Graduated = 4,
  Suspended = 5,
  Expelled = 6,
  Withdrawn = 7,
  Deceased = 8,
  Deleted = 9,
}

export enum CBCLevel {
  PP1 = 1,
  PP2 = 2,
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

// ─── Display Options ──────────────────────────────────────────────────────────

export const GenderOptions = [
  { value: Gender.Male, label: 'Male', name: 'Male' },
  { value: Gender.Female, label: 'Female', name: 'Female' },
  { value: Gender.Other, label: 'Other / Prefer not to say', name: 'Other' },
];

export const StudentStatusOptions = [
  { value: StudentStatus.Active, label: 'Active', name: 'Active' },
  { value: StudentStatus.Inactive, label: 'Inactive', name: 'Inactive' },
  { value: StudentStatus.Transferred, label: 'Transferred', name: 'Transferred' },
  { value: StudentStatus.Graduated, label: 'Graduated', name: 'Graduated' },
  { value: StudentStatus.Suspended, label: 'Suspended', name: 'Suspended' },
  { value: StudentStatus.Expelled, label: 'Expelled', name: 'Expelled' },
  { value: StudentStatus.Withdrawn, label: 'Withdrawn', name: 'Withdrawn' },
];

export const CBCLevelOptions = [
  { value: CBCLevel.PP1, label: 'Pre-Primary 1 (PP1)', name: 'PP1', shortName: 'PP1' },
  { value: CBCLevel.PP2, label: 'Pre-Primary 2 (PP2)', name: 'PP2', shortName: 'PP2' },
  { value: CBCLevel.Grade1, label: 'Grade 1', name: 'Grade1', shortName: 'Grade1' },
  { value: CBCLevel.Grade2, label: 'Grade 2', name: 'Grade2', shortName: 'Grade2' },
  { value: CBCLevel.Grade3, label: 'Grade 3', name: 'Grade3', shortName: 'Grade3' },
  { value: CBCLevel.Grade4, label: 'Grade 4', name: 'Grade4', shortName: 'Grade4' },
  { value: CBCLevel.Grade5, label: 'Grade 5', name: 'Grade5', shortName: 'Grade5' },
  { value: CBCLevel.Grade6, label: 'Grade 6', name: 'Grade6', shortName: 'Grade6' },
  { value: CBCLevel.Grade7, label: 'Grade 7', name: 'Grade7', shortName: 'Grade7' },
  { value: CBCLevel.Grade8, label: 'Grade 8', name: 'Grade8', shortName: 'Grade8' },
  { value: CBCLevel.Grade9, label: 'Grade 9', name: 'Grade9', shortName: 'Grade9' },
  { value: CBCLevel.Grade10, label: 'Grade 10', name: 'Grade10', shortName: 'Grade10' },
  { value: CBCLevel.Grade11, label: 'Grade 11', name: 'Grade11', shortName: 'Grade11' },
  { value: CBCLevel.Grade12, label: 'Grade 12', name: 'Grade12', shortName: 'Grade12' },
];

// ─── Conversion Utilities ─────────────────────────────────────────────────────

/**
 * Convert any value to a number (handles strings, numbers, null)
 */
export function toNumber(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  if (typeof value === 'number') return value;
  
  const num = Number(value);
  return !isNaN(num) ? num : null;
}

/**
 * Convert Gender value from backend format to enum number
 * Handles: "1", 1, "Male", "2", 2, "Female", etc.
 */
export function normalizeGender(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  // If it's already a valid number (1, 2, 3)
  const numValue = toNumber(value);
  if (numValue !== null && numValue >= 1 && numValue <= 3) {
    return numValue;
  }
  
  // If it's a string name, convert it
  const strValue = String(value).toLowerCase().trim();
  if (strValue === 'male' || strValue === '1') return Gender.Male;
  if (strValue === 'female' || strValue === '2') return Gender.Female;
  if (strValue === 'other' || strValue === '3') return Gender.Other;
  
  return null;
}
export enum Religion {
  Christianity = 1,
  Islam = 2,
  Hinduism = 3,
  Buddhism = 4,
  Sikhism = 5,
  Judaism = 6,
  TraditionalAfrican = 7,
  Other = 8,
  PreferNotToSay = 9,
}


/**
 * Convert StudentStatus value from backend format to enum number
 * Handles: "1", 1, "Active", etc.
 */
export function normalizeStudentStatus(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  // If it's already a valid number (1-9)
  const numValue = toNumber(value);
  if (numValue !== null && numValue >= 1 && numValue <= 9) {
    return numValue;
  }
  
  // If it's a string name, convert it
  const strValue = String(value).toLowerCase().trim();
  const statusMap: { [key: string]: number } = {
    'active': StudentStatus.Active,
    'inactive': StudentStatus.Inactive,
    'transferred': StudentStatus.Transferred,
    'graduated': StudentStatus.Graduated,
    'suspended': StudentStatus.Suspended,
    'expelled': StudentStatus.Expelled,
    'withdrawn': StudentStatus.Withdrawn,
    'deceased': StudentStatus.Deceased,
    'deleted': StudentStatus.Deleted,
  };
  
  return statusMap[strValue] ?? null;
}

export const ReligionOptions = [
  { value: Religion.Christianity, label: 'Christianity', name: 'Christianity' },
  { value: Religion.Islam, label: 'Islam', name: 'Islam' },
  { value: Religion.Hinduism, label: 'Hinduism', name: 'Hinduism' },
  { value: Religion.Buddhism, label: 'Buddhism', name: 'Buddhism' },
  { value: Religion.Sikhism, label: 'Sikhism', name: 'Sikhism' },
  { value: Religion.Judaism, label: 'Judaism', name: 'Judaism' },
  { value: Religion.TraditionalAfrican, label: 'Traditional African Religion', name: 'TraditionalAfrican' },
  { value: Religion.Other, label: 'Other', name: 'Other' },
  { value: Religion.PreferNotToSay, label: 'Prefer not to say', name: 'PreferNotToSay' },
];

export enum Nationality {
  Kenyan = 1,
  Ugandan = 2,
  Tanzanian = 3,
  Rwandan = 4,
  Burundian = 5,
  SouthSudanese = 6,
  Somali = 7,
  Ethiopian = 8,
  Congolese = 9,
  Nigerian = 10,
  Ghanaian = 11,
  SouthAfrican = 12,
  Eritrean = 13,
  Sudanese = 14,
  OtherAfrican = 15,
  British = 16,
  American = 17,
  Canadian = 18,
  Indian = 19,
  Pakistani = 20,
  Chinese = 21,
  Other = 22,
}
export function normalizeReligion(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  // If it's already a valid number (1-9)
  const numValue = typeof value === 'number' ? value : Number(value);
  if (!isNaN(numValue) && numValue >= 1 && numValue <= 9) {
    return numValue;
  }
  
  // If it's a string name, convert it
  const strValue = String(value).toLowerCase().replace(/\s+/g, '');
  const religionMap: { [key: string]: number } = {
    'christianity': Religion.Christianity,
    'islam': Religion.Islam,
    'hinduism': Religion.Hinduism,
    'buddhism': Religion.Buddhism,
    'sikhism': Religion.Sikhism,
    'judaism': Religion.Judaism,
    'traditionalafrican': Religion.TraditionalAfrican,
    'traditionalafricanreligion': Religion.TraditionalAfrican,
    'other': Religion.Other,
    'prefernottosay': Religion.PreferNotToSay,
  };
  
  return religionMap[strValue] ?? null;
}

export function normalizeNationality(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  // If it's already a valid number (1-22)
  const numValue = typeof value === 'number' ? value : Number(value);
  if (!isNaN(numValue) && numValue >= 1 && numValue <= 22) {
    return numValue;
  }
  
  // If it's a string name, convert it
  const strValue = String(value).toLowerCase().replace(/[\s\-()]/g, '');
  const nationalityMap: { [key: string]: number } = {
    'kenyan': Nationality.Kenyan,
    'ugandan': Nationality.Ugandan,
    'tanzanian': Nationality.Tanzanian,
    'rwandan': Nationality.Rwandan,
    'burundian': Nationality.Burundian,
    'southsudanese': Nationality.SouthSudanese,
    'somali': Nationality.Somali,
    'ethiopian': Nationality.Ethiopian,
    'congolese': Nationality.Congolese,
    'congolesedrc': Nationality.Congolese,
    'nigerian': Nationality.Nigerian,
    'ghanaian': Nationality.Ghanaian,
    'southafrican': Nationality.SouthAfrican,
    'eritrean': Nationality.Eritrean,
    'sudanese': Nationality.Sudanese,
    'otherafrican': Nationality.OtherAfrican,
    'british': Nationality.British,
    'american': Nationality.American,
    'canadian': Nationality.Canadian,
    'indian': Nationality.Indian,
    'pakistani': Nationality.Pakistani,
    'chinese': Nationality.Chinese,
    'other': Nationality.Other,
  };
  
  return nationalityMap[strValue] ?? null;
}




export const NationalityOptions = [
  // East African Community (Priority)
  { value: Nationality.Kenyan, label: 'Kenyan', name: 'Kenyan', region: 'EAC' },
  { value: Nationality.Ugandan, label: 'Ugandan', name: 'Ugandan', region: 'EAC' },
  { value: Nationality.Tanzanian, label: 'Tanzanian', name: 'Tanzanian', region: 'EAC' },
  { value: Nationality.Rwandan, label: 'Rwandan', name: 'Rwandan', region: 'EAC' },
  { value: Nationality.Burundian, label: 'Burundian', name: 'Burundian', region: 'EAC' },
  { value: Nationality.SouthSudanese, label: 'South Sudanese', name: 'SouthSudanese', region: 'EAC' },
  
  // Other African
  { value: Nationality.Somali, label: 'Somali', name: 'Somali', region: 'Africa' },
  { value: Nationality.Ethiopian, label: 'Ethiopian', name: 'Ethiopian', region: 'Africa' },
  { value: Nationality.Congolese, label: 'Congolese (DRC)', name: 'Congolese', region: 'Africa' },
  { value: Nationality.Nigerian, label: 'Nigerian', name: 'Nigerian', region: 'Africa' },
  { value: Nationality.Ghanaian, label: 'Ghanaian', name: 'Ghanaian', region: 'Africa' },
  { value: Nationality.SouthAfrican, label: 'South African', name: 'SouthAfrican', region: 'Africa' },
  { value: Nationality.Eritrean, label: 'Eritrean', name: 'Eritrean', region: 'Africa' },
  { value: Nationality.Sudanese, label: 'Sudanese', name: 'Sudanese', region: 'Africa' },
  { value: Nationality.OtherAfrican, label: 'Other African', name: 'OtherAfrican', region: 'Africa' },
  
  // International
  { value: Nationality.British, label: 'British', name: 'British', region: 'International' },
  { value: Nationality.American, label: 'American', name: 'American', region: 'International' },
  { value: Nationality.Canadian, label: 'Canadian', name: 'Canadian', region: 'International' },
  { value: Nationality.Indian, label: 'Indian', name: 'Indian', region: 'International' },
  { value: Nationality.Pakistani, label: 'Pakistani', name: 'Pakistani', region: 'International' },
  { value: Nationality.Chinese, label: 'Chinese', name: 'Chinese', region: 'International' },
  { value: Nationality.Other, label: 'Other', name: 'Other', region: 'International' },
];


/**
 * Convert CBCLevel value from backend format to enum number
 * Handles: "1", 1, "PP1", "2", 2, "PP2", "Grade1", etc.
 */
export function normalizeCBCLevel(value: any): number | null {
  if (value === null || value === undefined || value === '') return null;
  
  // If it's already a valid number (1-14)
  const numValue = toNumber(value);
  if (numValue !== null && numValue >= 1 && numValue <= 14) {
    return numValue;
  }
  
  // If it's a string name, convert it
  const strValue = String(value).toUpperCase().replace(/[\s-]/g, '');
  
  // Comprehensive mapping of all possible string representations
  const levelMap: { [key: string]: CBCLevel } = {
    // Direct enum names
    'PP1': CBCLevel.PP1,
    'PP2': CBCLevel.PP2,
    'GRADE1': CBCLevel.Grade1,
    'GRADE2': CBCLevel.Grade2,
    'GRADE3': CBCLevel.Grade3,
    'GRADE4': CBCLevel.Grade4,
    'GRADE5': CBCLevel.Grade5,
    'GRADE6': CBCLevel.Grade6,
    'GRADE7': CBCLevel.Grade7,
    'GRADE8': CBCLevel.Grade8,
    'GRADE9': CBCLevel.Grade9,
    'GRADE10': CBCLevel.Grade10,
    'GRADE11': CBCLevel.Grade11,
    'GRADE12': CBCLevel.Grade12,
    
    // Alternative formats
    'PREPRIMARY1': CBCLevel.PP1,
    'PREPRIMARY2': CBCLevel.PP2,
    'GR1': CBCLevel.Grade1,
    'GR2': CBCLevel.Grade2,
    'GR3': CBCLevel.Grade3,
    'GR4': CBCLevel.Grade4,
    'GR5': CBCLevel.Grade5,
    'GR6': CBCLevel.Grade6,
    'GR7': CBCLevel.Grade7,
    'GR8': CBCLevel.Grade8,
    'GR9': CBCLevel.Grade9,
    'GR10': CBCLevel.Grade10,
    'GR11': CBCLevel.Grade11,
    'GR12': CBCLevel.Grade12,
    
    // String number representations
    '1': CBCLevel.PP1,
    '2': CBCLevel.PP2,
    '3': CBCLevel.Grade1,
    '4': CBCLevel.Grade2,
    '5': CBCLevel.Grade3,
    '6': CBCLevel.Grade4,
    '7': CBCLevel.Grade5,
    '8': CBCLevel.Grade6,
    '9': CBCLevel.Grade7,
    '10': CBCLevel.Grade8,
    '11': CBCLevel.Grade9,
    '12': CBCLevel.Grade10,
    '13': CBCLevel.Grade11,
    '14': CBCLevel.Grade12,
  };
  
  return levelMap[strValue] ?? null;
}

/**
 * Normalize all enum values in a student data object
 * This is the main function to use when loading data from backend
 */
export function normalizeStudentEnums(data: any): any {
  if (!data) return {};
  
  return {
    ...data,
    gender: normalizeGender(data.gender),
    studentStatus: normalizeStudentStatus(data.studentStatus),
    cbcLevel: normalizeCBCLevel(data.cbcLevel),
    currentLevel: normalizeCBCLevel(data.currentLevel),
  };
}

/**
 * Get display label for Gender
 */
export function getGenderLabel(value: any): string {
  const normalized = normalizeGender(value);
  const option = GenderOptions.find(o => o.value === normalized);
  return option?.label ?? '—';
}

/**
 * Get display label for StudentStatus
 */
export function getStudentStatusLabel(value: any): string {
  const normalized = normalizeStudentStatus(value);
  const option = StudentStatusOptions.find(o => o.value === normalized);
  return option?.label ?? '—';
}

/**
 * Get display label for CBCLevel
 */
export function getCBCLevelLabel(value: any): string {
  const normalized = normalizeCBCLevel(value);
  const option = CBCLevelOptions.find(o => o.value === normalized);
  return option?.label ?? '—';
}