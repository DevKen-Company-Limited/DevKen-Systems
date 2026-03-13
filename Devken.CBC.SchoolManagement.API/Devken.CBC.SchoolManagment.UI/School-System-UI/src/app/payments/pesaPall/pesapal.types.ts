// ═══════════════════════════════════════════════════════════════════
// pesapal.types.ts
// Shared PesaPal contracts used across the Angular finance module
// Mirrors .NET DTOs and domain models
// ═══════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────
// Enums
// ─────────────────────────────────────────────────────────────────

export enum PesaPalEnvironment {
  Sandbox    = 'Sandbox',
  Production = 'Production',
}

// Status values match the string literals used in the dialog component
// for direct comparison: s === 'COMPLETED', s === 'FAILED', etc.
export type PesaPalPaymentStatus =
  | 'PENDING'
  | 'COMPLETED'
  | 'FAILED'
  | 'INVALID'
  | 'REVERSED'
  | 'VOIDED';

export enum PesaPalPaymentMethod {
  Mpesa       = 'mpesa',
  AirtelMoney = 'airtel',
  Equitel     = 'equitel',   // ← added: dialog checks for 'equitel' in isPhoneMethod()
  Card        = 'card',
  Bank        = 'bank',
}

// ─────────────────────────────────────────────────────────────────
// Settings DTOs
// ─────────────────────────────────────────────────────────────────

export interface PesaPalSettingsDto {
  environment:    PesaPalEnvironment;
  consumerKey:    string;
  consumerSecret: string;
  baseUrl:        string;
  ipnUrl:         string;
  callbackUrl:    string;
  ipnRegistered?: boolean;
  ipnId?:         string;
}

export interface PesaPalIpnRegistrationResult {
  ipnId:     string;
  url:       string;
  createdAt: string;
}

// ─────────────────────────────────────────────────────────────────
// Auth
// ─────────────────────────────────────────────────────────────────

export interface PesaPalAuthRequest {
  consumer_key:    string;
  consumer_secret: string;
}

export interface PesaPalAuthResponse {
  token:      string;
  expiryDate: string;
  error?:     PesaPalError;
  status?:    string;
  message?:   string;
}

// ─────────────────────────────────────────────────────────────────
// IPN
// ─────────────────────────────────────────────────────────────────

export interface PesaPalIpnRequest {
  url:                    string;
  ipn_notification_type:  'GET' | 'POST';
}

export interface PesaPalIpnResponse {
  url:          string;
  created_date: string;
  ipn_id:       string;
  error?:       PesaPalError;
  status?:      string;
  message?:     string;
}

// ─────────────────────────────────────────────────────────────────
// Billing Address
// ─────────────────────────────────────────────────────────────────

export interface PesaPalBillingAddress {
  email_address?: string;
  phone_number?:  string;
  country_code?:  string;
  first_name?:    string;
  middle_name?:   string;
  last_name?:     string;
  line_1?:        string;
  line_2?:        string;
  city?:          string;
  state?:         string;
  postal_code?:   string;
  zip_code?:      string;
}

// ─────────────────────────────────────────────────────────────────
// Order Submission
// ─────────────────────────────────────────────────────────────────

export interface SubmitOrderRequestDto {
  id:               string;   // merchant reference
  currency?:        string;
  amount:           number;
  description?:     string;
  branch?:          string;   // tenant/school id
  billing_address:  PesaPalBillingAddress;
}

export interface PesaPalOrderResponse {
  redirect_url:       string;
  order_tracking_id:  string;
  merchant_reference: string;
  error?:             PesaPalError;
  status?:            string;
  message?:           string;
}

// ─────────────────────────────────────────────────────────────────
// Transaction Status
// ─────────────────────────────────────────────────────────────────

export interface PesaPalStatusResponse {
  payment_method?:              string;
  amount?:                      number;
  created_date?:                string;
  confirmation_code?:           string;
  merchant_reference?:          string;
  payment_status_description?:  PesaPalPaymentStatus;
  description?:                 string;
  message?:                     string;
  payment_account?:             string;
  status_code?:                 number;
  currency?:                    string;
  order_tracking_id?:           string;
  error?:                       PesaPalError;
}

// ─────────────────────────────────────────────────────────────────
// Transaction Listing (Finance → Transactions)
// ─────────────────────────────────────────────────────────────────

export interface PesaPalTransactionRow {
  id:                 string;
  orderTrackingId:    string;
  merchantReference:  string;
  amount:             number;
  currency:           string;
  description?:       string;
  paymentStatus:      PesaPalPaymentStatus;
  paymentMethod?:     string;
  confirmationCode?:  string;
  paymentAccount?:    string;
  errorMessage?:      string;
  payerFirstName?:    string;
  payerLastName?:     string;
  payerEmail?:        string;
  payerPhone?:        string;
  createdOn:          string;
  updatedOn?:         string;
  completedOn?:       string;
}

// ─────────────────────────────────────────────────────────────────
// Checkout Dialog
// ─────────────────────────────────────────────────────────────────

export interface PesaPalDialogData {
  amount:              number;
  description:         string;
  firstName:           string;
  lastName:            string;
  email?:              string;
  phone?:              string;
  reference:           string;
  tenantId?:           string;
  // ↓ fields used by the dialog component
  merchantReference?:  string;   // pre-set merchant ref (optional — service generates one if absent)
  schoolId?:           string;   // passed as branch on the order request
}

export interface PesaPalDialogResult {
  success:           boolean;
  orderTrackingId?:  string;
  confirmationCode?: string;
  paymentMethod?:    string;
  amount?:           number;
  status?:           PesaPalPaymentStatus;
  error?:            string;
}

// ─────────────────────────────────────────────────────────────────
// Payment Options (UI)
// ─────────────────────────────────────────────────────────────────

export interface PesaPalPaymentOption {
  id:     string;    // ← matches dialog: selectedMethod.id, isPhoneMethod() checks opt.id
  label:  string;
  icon:   string;
}

export const PESAPAL_PAYMENT_OPTIONS: PesaPalPaymentOption[] = [
  { id: PesaPalPaymentMethod.Mpesa,       label: 'M-Pesa',        icon: 'account_balance_wallet' },
  { id: PesaPalPaymentMethod.Card,        label: 'Card',          icon: 'credit_card'            },
  { id: PesaPalPaymentMethod.Bank,        label: 'Bank Transfer', icon: 'account_balance'        },
  { id: PesaPalPaymentMethod.AirtelMoney, label: 'Airtel Money',  icon: 'phone_android'          },
  { id: PesaPalPaymentMethod.Equitel,     label: 'Equitel',       icon: 'phone_android'          },
];

// ─────────────────────────────────────────────────────────────────
// Shared Error
// ─────────────────────────────────────────────────────────────────

export interface PesaPalError {
  error_type?: string;
  code?:       string;
  message?:    string;
}