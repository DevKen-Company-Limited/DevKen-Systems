// ── Enums ──────────────────────────────────────────────────────────────────
export enum ParentRelationship {
  Father = 0,
  Mother = 1,
  Guardian = 2,
  Sibling = 3,
  Grandparent = 4,
  Uncle = 5,
  Aunt = 6,
  Other = 7,
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