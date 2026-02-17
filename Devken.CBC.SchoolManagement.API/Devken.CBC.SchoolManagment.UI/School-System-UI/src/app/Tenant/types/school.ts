// ==================== Enums ====================

/**
 * Subscription Status - matches backend C# enum
 */
export const SubscriptionStatus = {
  PendingPayment: 0,
  Active: 1,
  Suspended: 2,
  Cancelled: 3,
  Expired: 4,
  GracePeriod: 5
} as const;
export type SubscriptionStatus = typeof SubscriptionStatus[keyof typeof SubscriptionStatus];

/**
 * Billing Cycle - matches backend C# enum
 */
export const BillingCycle = {
  Monthly: 1,
  Quarterly: 3,
  Yearly: 4
} as const;
export type BillingCycle = typeof BillingCycle[keyof typeof BillingCycle];

/**
 * Subscription Plan - matches backend C# enum
 */
export const SubscriptionPlan = {
  Basic: 0,
  Standard: 1,
  Premium: 2,
  Enterprise: 3
} as const;
export type SubscriptionPlan = typeof SubscriptionPlan[keyof typeof SubscriptionPlan];

/**
 * School Type - matches backend C# enum (SchoolType)
 * Public = 1, Private = 2, International = 3, NGO = 4
 */
export const SchoolType = {
  Public: 1,
  Private: 2,
  International: 3,
  NGO: 4
} as const;
export type SchoolType = typeof SchoolType[keyof typeof SchoolType];

/**
 * School Category - matches backend C# enum (SchoolCategory)
 * Day = 1, Boarding = 2, Mixed = 3
 */
export const SchoolCategory = {
  Day: 1,
  Boarding: 2,
  Mixed: 3
} as const;
export type SchoolCategory = typeof SchoolCategory[keyof typeof SchoolCategory];

// ==================== API Response ====================

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

// ==================== School DTOs (aligned to backend) ====================

/** Matches C# SchoolDto exactly */
export interface SchoolDto {
  id: string;
  slugName: string;
  name: string;
  registrationNumber?: string;
  knecCenterCode?: string;
  kraPin?: string;
  address?: string;
  county?: string;
  subCounty?: string;
  phoneNumber?: string;
  email?: string;
  logoUrl?: string;
  schoolType: SchoolType;
  category: SchoolCategory;
  isActive: boolean;
  createdOn: string;
}

/** Matches C# CreateSchoolRequest */
export interface CreateSchoolRequest {
  slugName: string;
  name: string;
  registrationNumber?: string;
  knecCenterCode?: string;
  kraPin?: string;
  address?: string;
  county?: string;
  subCounty?: string;
  phoneNumber?: string;
  email?: string;
  logoUrl?: string;
  schoolType: SchoolType;
  category: SchoolCategory;
  isActive: boolean;
}

/** Matches C# UpdateSchoolRequest */
export interface UpdateSchoolRequest {
  slugName: string;
  name: string;
  registrationNumber?: string;
  knecCenterCode?: string;
  kraPin?: string;
  address?: string;
  county?: string;
  subCounty?: string;
  phoneNumber?: string;
  email?: string;
  logoUrl?: string;
  schoolType: SchoolType;
  category: SchoolCategory;
  isActive: boolean;
}

/** Matches C# UpdateSchoolStatusRequest */
export interface UpdateSchoolStatusRequest {
  isActive: boolean;
}

// ==================== Subscription Types ====================

export interface Subscription {
  id: string;
  schoolId: string;
  plan: number;
  billingCycle: number;
  startDate: string;
  expiryDate: string;
  status: number;
  amount: number;
  currency: string;
  maxStudents: number;
  maxTeachers: number;
  maxStorageGB: number;
  enabledFeatures: string[];
  canAccess: boolean;
  daysRemaining: number;
  isExpired?: boolean;
  isInGracePeriod?: boolean;
  suspensionReason?: string;
  isActive?: boolean;
  adminNotes?: string;
}

export interface CreateSubscriptionRequest {
  schoolId: string;
  plan: number;
  billingCycle: number;
  amount: number;
  currency: string;
  maxStudents: number;
  maxTeachers: number;
  maxStorageGB: number;
  enabledFeatures: string[];
}

export interface UpdateSubscriptionRequest {
  plan?: number;
  billingCycle?: number;
  amount?: number;
  adminNotes?: string;
}

export interface SubscriptionStatusCheck {
  hasActiveSubscription: boolean;
  status: string;
  expiryDate?: string;
  daysRemaining: number;
  message: string;
  needsRenewal: boolean;
  isExpired: boolean;
  isInGracePeriod: boolean;
  canAccess: boolean;
}

// ==================== Combined / View Types ====================

export interface SchoolWithSubscription extends SchoolDto {
  subscription: Subscription | null;
  subscriptionStatus: number | null;
  subscriptionExpiry: string | null;
  isSubscriptionActive: boolean;
  isSubscriptionExpired: boolean;
  daysRemaining: number;
  needsSubscription: boolean;
}

export interface SchoolStats {
  totalSchools: number;
  activeSchools: number;
  inactiveSchools: number;
  withActiveSubscription: number;
  withExpiredSubscription: number;
  withoutSubscription: number;
}