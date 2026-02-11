using System;
using System.Text.Json.Serialization;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Payments
{
    // ================= REQUEST =================

    public record MpesaPaymentRequest(
        [property: JsonPropertyName("phoneNumber")] string PhoneNumber,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("accountReference")] string AccountReference,
        [property: JsonPropertyName("transactionDesc")] string TransactionDesc
    );
    public sealed class MpesaAuthResponse
    {
        public string? AccessToken { get; set; }
        public string? ExpiresIn { get; set; }
    }
    // ================= CONFIG =================

    public sealed class MpesaConfig
    {
        public string ConsumerKey { get; init; } = string.Empty;
        public string ConsumerSecret { get; init; } = string.Empty;
        public string ShortCode { get; init; } = string.Empty;
        public string PassKey { get; init; } = string.Empty;
        public string CallbackUrl { get; init; } = string.Empty;

        public string StkPushUrl { get; init; } = string.Empty;
        public string StkQueryUrl { get; init; } = string.Empty;
        public string AuthUrl { get; init; } = string.Empty;
    }

    // ================= AUTH =================
    public sealed class MpesaStkResponse
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? CustomerMessage { get; set; }
    }
    public sealed class MpesaStkQueryResponse
    {
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResultCode { get; set; }
        public string? ResultDesc { get; set; }
    }

    // ================= STK PUSH RESPONSE =================

    public class MpesaStkPushResponse
    {
        [JsonPropertyName("MerchantRequestID")]
        public string MerchantRequestID { get; set; } = null!;

        [JsonPropertyName("CheckoutRequestID")]
        public string CheckoutRequestID { get; set; } = null!;

        [JsonPropertyName("ResponseCode")]
        public string ResponseCode { get; set; } = null!;

        [JsonPropertyName("ResponseDescription")]
        public string ResponseDescription { get; set; } = null!;

        [JsonPropertyName("CustomerMessage")]
        public string CustomerMessage { get; set; } = null!;
    }

    // ================= CALLBACK =================

    public class MpesaCallbackRequest
    {
        [JsonPropertyName("Body")]
        public CallbackBody? Body { get; set; }
    }

    public class CallbackBody
    {
        [JsonPropertyName("stkCallback")]
        public StkCallback? StkCallback { get; set; }
    }

    public class StkCallback
    {
        [JsonPropertyName("MerchantRequestID")]
        public string MerchantRequestID { get; set; } = null!;

        [JsonPropertyName("CheckoutRequestID")]
        public string CheckoutRequestID { get; set; } = null!;

        [JsonPropertyName("ResultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("ResultDesc")]
        public string ResultDesc { get; set; } = null!;

        [JsonPropertyName("CallbackMetadata")]
        public CallbackMetadata? CallbackMetadata { get; set; }
    }

    public class CallbackMetadata
    {
        [JsonPropertyName("Item")]
        public CallbackItem[]? Item { get; set; }
    }

    public class CallbackItem
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("Value")]
        public object? Value { get; set; }
    }
}
