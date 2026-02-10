using Devken.CBC.SchoolManagement.Api.Controllers.Common;

using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Devken.CBC.SchoolManagement.API.Controllers.Payments;

[ApiController]
[Route("api/mpesa")]
[Authorize]
public sealed class MpesaController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IRepositoryManager repositoryManager,
    ILogger<MpesaController> logger
) : BaseApiController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex NonDigitRegex = new(@"\D", RegexOptions.Compiled);

    private readonly MpesaConfig _config =
        configuration.GetSection("Mpesa").Get<MpesaConfig>()
        ?? throw new InvalidOperationException("Mpesa configuration missing");

    private static DateTime EastAfricaTime =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time"));

    private async Task<string?> GetAccessTokenAsync()
    {
        var client = httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ConsumerKey}:{_config.ConsumerSecret}")
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var response = await client.GetAsync(_config.AuthUrl);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MpesaAuthResponse>(json, JsonOptions)?.AccessToken;
    }

    private static (bool Valid, string? Phone) NormalizePhone(string input)
    {
        var clean = NonDigitRegex.Replace(input, "");
        return clean.Length switch
        {
            12 when clean.StartsWith("254") => (true, clean),
            10 when clean.StartsWith("0") => (true, $"254{clean[1..]}"),
            9 when clean.StartsWith("7") => (true, $"254{clean}"),
            _ => (false, null)
        };
    }

    // ---------------- INITIATE PAYMENT ----------------
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(MpesaPaymentRequest request)
    {
        if (!HasPermission(PermissionKeys.PaymentWrite))
            return ForbiddenResponse("Permission denied");

        var (valid, phone) = NormalizePhone(request.PhoneNumber);
        if (!valid) return ValidationErrorResponse("Invalid phone number");

        var token = await GetAccessTokenAsync();
        if (token is null) return ErrorResponse("Mpesa unavailable");

        var timestamp = EastAfricaTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var password = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ShortCode}{_config.PassKey}{timestamp}")
        );

        var payload = new
        {
            BusinessShortCode = _config.ShortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = (int)request.Amount,
            PartyA = phone,
            PartyB = _config.ShortCode,
            PhoneNumber = phone,
            CallBackURL = _config.CallbackUrl,
            AccountReference = "SCHOOL_FEE",
            TransactionDesc = "School Fee Payment"
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync(
            _config.StkPushUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var stk = JsonSerializer.Deserialize<MpesaStkResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

        if (stk?.ResponseCode != "0")
            return ErrorResponse(stk?.CustomerMessage ?? "STK failed");

        repositoryManager.MpesaPayment.Create(new MpesaPaymentRecord
        {
            CheckoutRequestId = stk.CheckoutRequestID!,
            MerchantRequestId = stk.MerchantRequestID!,
            PhoneNumber = phone!,
            Amount = request.Amount,
            PaymentStatus = PaymentStatus.Pending,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow,
            CreatedBy = CurrentUserId,
            UpdatedBy = CurrentUserId,
            TenantId = CurrentTenantId!.Value
        });

        await repositoryManager.SaveAsync();
        return SuccessResponse(stk, "Check your phone");
    }

    // ---------------- QUERY STATUS ----------------
    [HttpPost("query/{checkoutRequestId}")]
    public async Task<IActionResult> Query(string checkoutRequestId)
    {
        if (!HasPermission(PermissionKeys.PaymentRead))
            return ForbiddenResponse("Permission denied");

        var token = await GetAccessTokenAsync();
        if (token is null) return ErrorResponse("Mpesa unavailable");

        var timestamp = EastAfricaTime.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ShortCode}{_config.PassKey}{timestamp}")
        );

        var payload = new
        {
            BusinessShortCode = _config.ShortCode,
            Password = password,
            Timestamp = timestamp,
            CheckoutRequestID = checkoutRequestId
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync(
            _config.StkQueryUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var result = JsonSerializer.Deserialize<MpesaStkQueryResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

        return SuccessResponse(result, "Query successful");
    }
}