// ═══════════════════════════════════════════════════════════════════
// PesaPalDtos.cs
// All PesaPal v3 request / response contracts + internal DTOs.
// Place in: Devken.CBC.SchoolManagement.Application/DTOs/PesaPal/
// ═══════════════════════════════════════════════════════════════════

using System.Text.Json.Serialization;

namespace Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;

// ─────────────────────────────────────────────────────────────────
// Auth
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalAuthRequest
{
    [JsonPropertyName("consumer_key")] public string ConsumerKey { get; set; } = string.Empty;
    [JsonPropertyName("consumer_secret")] public string ConsumerSecret { get; set; } = string.Empty;
}

public sealed class PesaPalAuthResponse
{
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; set; }
    [JsonPropertyName("error")] public PesaPalError? Error { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// IPN Registration
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalIpnRequest
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("ipn_notification_type")] public string IpnNotificationType { get; set; } = "GET";
}

public sealed class PesaPalIpnResponse
{
    [JsonPropertyName("ipn_id")] public string? IpnId { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("created_date")] public string? CreatedDate { get; set; }
    [JsonPropertyName("ipn_notification_type")] public string? IpnNotificationType { get; set; }
    [JsonPropertyName("error")] public PesaPalError? Error { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// Order Submit
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalBillingAddress
{
    [JsonPropertyName("email_address")] public string EmailAddress { get; set; } = string.Empty;
    [JsonPropertyName("phone_number")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "KE";
    [JsonPropertyName("first_name")] public string FirstName { get; set; } = string.Empty;
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("line_1")] public string? Line1 { get; set; }
    [JsonPropertyName("line_2")] public string? Line2 { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
    [JsonPropertyName("zip_code")] public string? ZipCode { get; set; }
}

/// <summary>
/// What PesaPal expects when we call SubmitOrderRequest.
/// </summary>
public sealed class PesaPalOrderRequest
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("currency")] public string Currency { get; set; } = "KES";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("callback_url")] public string CallbackUrl { get; set; } = string.Empty;
    [JsonPropertyName("notification_id")] public string NotificationId { get; set; } = string.Empty;
    [JsonPropertyName("branch")] public string? Branch { get; set; }
    [JsonPropertyName("billing_address")] public PesaPalBillingAddress BillingAddress { get; set; } = new();
}

/// <summary>
/// What the Angular front-end sends to our .NET API endpoint.
/// Mirrors PesaPalOrderRequest minus server-side fields (callback_url, notification_id).
/// </summary>
public sealed class SubmitOrderRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string Currency { get; set; } = "KES";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public PesaPalBillingAddress BillingAddress { get; set; } = new();
}

public sealed class PesaPalOrderResponse
{
    [JsonPropertyName("order_tracking_id")] public string? OrderTrackingId { get; set; }
    [JsonPropertyName("merchant_reference")] public string? MerchantReference { get; set; }
    [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
    [JsonPropertyName("error")] public PesaPalError? Error { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// Transaction Status
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalStatusResponse
{
    [JsonPropertyName("payment_method")] public string? PaymentMethod { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("created_date")] public string? CreatedDate { get; set; }
    [JsonPropertyName("confirmation_code")] public string? ConfirmationCode { get; set; }
    [JsonPropertyName("payment_status_description")] public string? PaymentStatusDescription { get; set; }
    [JsonPropertyName("order_tracking_id")] public string? OrderTrackingId { get; set; }
    [JsonPropertyName("merchant_reference")] public string? MerchantReference { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("payment_account")] public string? PaymentAccount { get; set; }
    [JsonPropertyName("status_code")] public int StatusCode { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("error")] public PesaPalError? Error { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// IPN Callback query params
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalIpnCallbackDto
{
    public string? OrderTrackingId { get; set; }
    public string? OrderMerchantReference { get; set; }
    public string? OrderNotificationType { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// Shared error shape
// ─────────────────────────────────────────────────────────────────

public sealed class PesaPalError
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}












public sealed class PesaPalTransactionPageDto
{
    public IEnumerable<PesaPalTransactionRowDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class PesaPalTransactionRowDto
{
    public string Id { get; init; } = string.Empty;
    public string OrderTrackingId { get; init; } = string.Empty;
    public string MerchantReference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "KES";
    public string? Description { get; init; }
    public string PaymentStatus { get; init; } = "PENDING";
    public string? PaymentMethod { get; init; }
    public string? ConfirmationCode { get; init; }
    public string? PaymentAccount { get; init; }
    public string? ErrorMessage { get; init; }
    public string? PayerFirstName { get; init; }
    public string? PayerLastName { get; init; }
    public string? PayerEmail { get; init; }
    public string? PayerPhone { get; init; }
    public string CreatedOn { get; init; } = string.Empty;
    public string? UpdatedOn { get; init; }
    public string? CompletedOn { get; init; }
}

public sealed class PesaPalSettingsResponseDto
{
    public string Environment { get; set; } = "Sandbox";
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public bool IpnRegistered { get; set; }
    public string? IpnId { get; set; }
}

public sealed class PesaPalSettingsSaveDto
{
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}