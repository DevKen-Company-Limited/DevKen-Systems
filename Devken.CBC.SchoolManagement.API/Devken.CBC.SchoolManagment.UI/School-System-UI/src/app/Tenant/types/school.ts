export interface SchoolDto {
  id: string;
  name: string;
  slugName: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  website?: string;
  description?: string;
  logoUrl?: string;
  timeZone?: string;
  language?: string;
  currency?: string;
  isActive: boolean;
  maxUsers?: number;
  subscriptionType?: string;
  subscriptionExpiry?: string;
  createdOn: string;
  updatedOn?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface CreateSchoolRequest {
  name: string;
  slugName: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  website?: string;
  description?: string;
  timeZone?: string;
  language?: string;
  currency?: string;
  logoUrl?: string;
  maxUsers?: number;
  subscriptionType?: string;
  subscriptionExpiry?: string;
}

export interface UpdateSchoolRequest {
  name?: string;
  slugName?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  website?: string;
  description?: string;
  timeZone?: string;
  language?: string;
  currency?: string;
  logoUrl?: string;
  maxUsers?: number;
  subscriptionType?: string;
  subscriptionExpiry?: string;
}

export interface UpdateSchoolStatusRequest {
  isActive: boolean;
}

export interface SchoolStats {
  total: number;
  active: number;
  inactive: number;
  recent: number;
  bySubscriptionType: {
    [key: string]: number;
  };
  byTimeZone: {
    [key: string]: number;
  };
  growth: {
    currentMonth: number;
    previousMonth: number;
    percentage: number;
  };
}

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface PagedResponse<T = any> {
  success: boolean;
  data?: {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
  };
  message?: string;
  errors?: string[];
}

export interface BulkOperationRequest {
  schoolIds: string[];
  isActive?: boolean;
}

export interface SearchSchoolsRequest {
  searchTerm: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface ExportSchoolsRequest {
  format: 'csv' | 'excel' | 'pdf';
  includeInactive?: boolean;
  columns?: string[];
}