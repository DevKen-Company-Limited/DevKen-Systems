// ── Enums ──────────────────────────────────────────────────────────────────
export enum ParentRelationship {
  Father      = 1,   // ← was 0
  Mother      = 2,   // ← was 1
  Guardian    = 3,   // ← was 2
  Sponsor     = 4,   // ← was Sibling = 3 (wrong member entirely)
  Grandparent = 5,   // ← was 4
  Other       = 6,   // ← was 7
  // removed: Sibling, Uncle, Aunt — these don't exist in C#
}

// ── Request DTOs ───────────────────────────────────────────────────────────
export interface CreateParentDto {
  firstName: string;
  middleName?: string;
  lastName: string;
  phoneNumber?: string;
  alternativePhoneNumber?: string;
  email?: string;
  address?: string;
  nationalIdNumber?: string;
  passportNumber?: string;
  occupation?: string;
  employer?: string;
  employerContact?: string;
  relationship: ParentRelationship;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  hasPortalAccess: boolean;
  portalUserId?: string;
  tenantId?: string;
}

export interface UpdateParentDto {
  firstName: string;
  middleName?: string;
  lastName: string;
  phoneNumber?: string;
  alternativePhoneNumber?: string;
  email?: string;
  address?: string;
  nationalIdNumber?: string;
  passportNumber?: string;
  occupation?: string;
  employer?: string;
  employerContact?: string;
  relationship: ParentRelationship;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  hasPortalAccess: boolean;
  portalUserId?: string;
}

// ── Response DTOs ──────────────────────────────────────────────────────────
export interface ParentDto {
  id: string;
  tenantId: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  phoneNumber?: string;
  alternativePhoneNumber?: string;
  email?: string;
  address?: string;
  nationalIdNumber?: string;
  passportNumber?: string;
  occupation?: string;
  employer?: string;
  employerContact?: string;
  relationship: ParentRelationship;
  relationshipDisplay: string;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  hasPortalAccess: boolean;
  portalUserId?: string;
  status: string;
  createdOn: string;
  updatedOn: string;
  studentCount: number;
}

export interface ParentSummaryDto {
  id: string;
  fullName: string;
  phoneNumber?: string;
  email?: string;
  relationship: ParentRelationship;
  relationshipDisplay: string;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  hasPortalAccess: boolean;
  studentCount: number;
  status: string;
}

// ── Query DTO ──────────────────────────────────────────────────────────────
export interface ParentQueryDto {
  searchTerm?: string;
  relationship?: ParentRelationship;
  isPrimaryContact?: boolean;
  isEmergencyContact?: boolean;
  hasPortalAccess?: boolean;
  isActive?: boolean;
}

// ── Dialog Data ────────────────────────────────────────────────────────────
export interface ParentDialogData {
  mode: 'create' | 'edit';
  parent?: ParentDto;
}