/**
 * Subscription Status - Numeric constants matching backend C# enum
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
 * Billing Cycle - Numeric constants matching backend C# enum
 */
export const BillingCycle = {
  Monthly: 1,
  Quarterly: 3,
  Yearly: 4
} as const;

export type BillingCycle = typeof BillingCycle[keyof typeof BillingCycle];

/**
 * Subscription Plan - Numeric constants matching backend C# enum
 */
export const SubscriptionPlan = {
  Basic: 0,
  Standard: 1,
  Premium: 2,
  Enterprise: 3
} as const;

export type SubscriptionPlan = typeof SubscriptionPlan[keyof typeof SubscriptionPlan];

// ==================== API Response Types ====================

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface Subscription {
  id: string;
  schoolId: string;
  plan: number; // SubscriptionPlan value
  billingCycle: number; // BillingCycle value
  startDate: Date;
  expiryDate: Date;
  status: number; // SubscriptionStatus value
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

export interface SchoolDto {
  id: string;
  name: string;
  slug: string;
  email: string;
  phone: string;
  location: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface SchoolWithSubscription extends SchoolDto {
  createdOn: string | number | Date;
  phoneNumber: any;
  subscription: Subscription | null;
  subscriptionStatus: number | null;
  subscriptionExpiry: Date | null;
  isSubscriptionActive: boolean;
  isSubscriptionExpired: boolean;
  daysRemaining: number;
  needsSubscription: boolean;
}

export interface CreateSchoolRequest {
  name: string;
  slug: string;
  email: string;
  phone: string;
  location: string;
}

export interface UpdateSchoolRequest {
  name?: string;
  email?: string;
  phone?: string;
  location?: string;
}

export interface UpdateSchoolStatusRequest {
  isActive: boolean;
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

export interface SchoolStats {
  totalSchools: number;
  activeSchools: number;
  inactiveSchools: number;
  totalWithSubscriptions: number;
}

export interface SubscriptionStatusCheck {
  hasActiveSubscription: boolean;
  status: string;
  expiryDate?: Date;
  daysRemaining: number;
  message: string;
  needsRenewal: boolean;
  isExpired: boolean;
  isInGracePeriod: boolean;
  canAccess: boolean;
}