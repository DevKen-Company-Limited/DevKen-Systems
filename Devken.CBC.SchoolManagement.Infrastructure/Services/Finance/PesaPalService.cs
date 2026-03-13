// ═══════════════════════════════════════════════════════════════════
// PesaPalService.cs
// Place in: Devken.CBC.SchoolManagement.Infrastructure/Services/Finance/
// ═══════════════════════════════════════════════════════════════════

using Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Finance;

public sealed class PesaPalService : IPesaPalService
{
    private readonly PesaPalSettings _cfg;
    private readonly IHttpClientFactory _httpFactory;
    private readonly AppDbContext _db;
    private readonly ILogger<PesaPalService> _logger;

    // ── Process-level token cache ─────────────────────────────────
    private static string? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    // ── Process-level IPN id cache ────────────────────────────────
    private static string? _ipnId;
    private static readonly SemaphoreSlim _ipnLock = new(1, 1);

    // ── PesaPal status string → PaymentStatus enum ────────────────
    private static readonly Dictionary<string, PaymentStatus> _statusMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PENDING"] = PaymentStatus.Pending,
            ["COMPLETED"] = PaymentStatus.Completed,
            ["FAILED"] = PaymentStatus.Failed,
            ["INVALID"] = PaymentStatus.Failed,
            ["REVERSED"] = PaymentStatus.Reversed,
            ["VOIDED"] = PaymentStatus.Reversed,
        };

    // ── PesaPal payment method string → PaymentMethod enum ────────
    private static readonly Dictionary<string, PaymentMethod> _methodMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["mpesa"] = PaymentMethod.Mpesa,
            ["m-pesa"] = PaymentMethod.Mpesa,
            ["safaricom"] = PaymentMethod.Mpesa,
            ["airtel"] = PaymentMethod.Mpesa,
            ["equitel"] = PaymentMethod.Mpesa,
            ["visa"] = PaymentMethod.CreditCard,
            ["mastercard"] = PaymentMethod.CreditCard,
            ["card"] = PaymentMethod.CreditCard,
            ["bank"] = PaymentMethod.BankTransfer,
            ["bank transfer"] = PaymentMethod.BankTransfer,
            ["pesalink"] = PaymentMethod.BankTransfer,
        };

    // ── Case-insensitive JSON options ─────────────────────────────
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public PesaPalService(
        IHttpClientFactory httpFactory,
        IOptions<PesaPalSettings> options,
        AppDbContext db,
        ILogger<PesaPalService> logger)
    {
        _cfg = options.Value;
        _httpFactory = httpFactory;
        _db = db;
        _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════
    // 1. Auth — thread-safe token caching
    // ═════════════════════════════════════════════════════════════

    public async Task<string> GetTokenAsync()
    {
        // Fast path — cache is still valid
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddSeconds(-30))
            return _cachedToken;

        await _tokenLock.WaitAsync();
        try
        {
            // Double-checked locking
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddSeconds(-30))
                return _cachedToken;

            var client = CreateClient();
            var body = Serialize(new PesaPalAuthRequest
            {
                ConsumerKey = _cfg.ConsumerKey,
                ConsumerSecret = _cfg.ConsumerSecret,
            });

            using var resp = await client.PostAsync(
                "api/Auth/RequestToken",
                new StringContent(body, Encoding.UTF8, "application/json"));

            // Read raw JSON ONCE — this is the only read of the body stream
            var raw = await resp.Content.ReadAsStringAsync();

            _logger.LogDebug("PesaPal auth raw response: {Raw}", raw);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"PesaPal auth HTTP {(int)resp.StatusCode}: {raw}");

            // ── Parse with JsonNode so we are immune to field name casing
            //    and the slash-date format PesaPal uses ("01/01/2025 00:00:00")
            var node = JsonNode.Parse(raw)
                ?? throw new InvalidOperationException(
                       $"PesaPal returned empty/null auth response. Raw: {raw}");

            // PesaPal v3 uses camelCase: "token", "expiryDate"
            var token = node["token"]?.GetValue<string>()
                     ?? node["Token"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(token))
            {
                // Surface any error message PesaPal embedded in the response
                var errMsg = node["message"]?.GetValue<string>()
                          ?? node["Message"]?.GetValue<string>()
                          ?? node["error"]?["message"]?.GetValue<string>()
                          ?? raw;

                throw new InvalidOperationException($"PesaPal auth failed: {errMsg}");
            }

            // ── Parse expiry robustly — PesaPal may return:
            //    "2025-01-01T00:00:00"   (ISO-8601)
            //    "01/01/2025 00:00:00"   (slash-date — causes the "/" JSON error
            //                            only if the field is unquoted; if quoted
            //                            it is just an odd date string)
            var expiryRaw = node["expiryDate"]?.GetValue<string>()
                         ?? node["ExpiryDate"]?.GetValue<string>();

            _tokenExpiry = TryParseExpiry(expiryRaw);
            _cachedToken = token;

            _logger.LogInformation(
                "PesaPal token acquired. Expires: {Expiry:u}", _tokenExpiry);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 2. IPN registration — once per process
    // ═════════════════════════════════════════════════════════════

    public async Task<string> RegisterIpnAsync()
    {
        if (!string.IsNullOrWhiteSpace(_ipnId))
            return _ipnId;

        await _ipnLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_ipnId))
                return _ipnId;

            var client = await AuthenticatedClientAsync();
            var body = Serialize(new PesaPalIpnRequest
            {
                Url = _cfg.IpnUrl,
                IpnNotificationType = "GET",
            });

            using var resp = await client.PostAsync(
                "api/URLSetup/RegisterIPN",
                new StringContent(body, Encoding.UTF8, "application/json"));

            var raw = await resp.Content.ReadAsStringAsync();
            _logger.LogDebug("PesaPal RegisterIPN raw response: {Raw}", raw);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"IPN registration HTTP {(int)resp.StatusCode}: {raw}");

            var ipn = JsonSerializer.Deserialize<PesaPalIpnResponse>(raw, _json);

            if (ipn?.Error is not null || string.IsNullOrWhiteSpace(ipn?.IpnId))
                throw new InvalidOperationException(
                    $"IPN registration failed: {ipn?.Error?.Message ?? raw}");

            _ipnId = ipn.IpnId;
            _logger.LogInformation("PesaPal IPN registered. IpnId: {Id}", _ipnId);
            return _ipnId;
        }
        finally
        {
            _ipnLock.Release();
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 3. Submit order
    // ═════════════════════════════════════════════════════════════

    public async Task<PesaPalOrderResponse> SubmitOrderAsync(SubmitOrderRequestDto dto)
    {
        var ipnId = await RegisterIpnAsync();
        var client = await AuthenticatedClientAsync();

        var order = new PesaPalOrderRequest
        {
            Id = dto.Id,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "KES" : dto.Currency,
            Amount = dto.Amount,
            Description = dto.Description,
            Branch = dto.Branch,
            CallbackUrl = _cfg.CallbackUrl,
            NotificationId = ipnId,
            BillingAddress = dto.BillingAddress,
        };

        _logger.LogInformation(
            "PesaPal SubmitOrder — Ref: {Ref}, Amount: {Amount:N2} {Curr}",
            dto.Id, dto.Amount, order.Currency);

        var body = Serialize(order);

        using var resp = await client.PostAsync(
            "api/Transactions/SubmitOrderRequest",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var raw = await resp.Content.ReadAsStringAsync();
        _logger.LogDebug("PesaPal SubmitOrder raw response: {Raw}", raw);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"PesaPal SubmitOrder HTTP {(int)resp.StatusCode}: {raw}");

        var result = JsonSerializer.Deserialize<PesaPalOrderResponse>(raw, _json);

        if (result?.Error is not null || string.IsNullOrWhiteSpace(result?.RedirectUrl))
            throw new InvalidOperationException(
                $"Order submission failed: {result?.Error?.Message ?? "no redirect_url returned"}. Raw: {raw}");

        // Persist Pending transaction row
        var txn = new PesaPalTransaction
        {
            OrderTrackingId = result.OrderTrackingId!,
            MerchantReference = result.MerchantReference ?? dto.Id,
            Amount = dto.Amount,
            Currency = order.Currency,
            Description = dto.Description,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = null,
            ConfirmationCode = null,
            PaymentAccount = null,
            ErrorMessage = null,
            PayerFirstName = dto.BillingAddress?.FirstName,
            PayerLastName = dto.BillingAddress?.LastName,
            PayerEmail = dto.BillingAddress?.EmailAddress,
            PayerPhone = dto.BillingAddress?.PhoneNumber,
            CreatedOn = DateTime.UtcNow,
        };

        _db.PesaPalTransactions.Add(txn);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PesaPal transaction persisted (Pending). TrackingId: {Id}",
            result.OrderTrackingId);

        return result;
    }

    // ═════════════════════════════════════════════════════════════
    // 4. Transaction status
    // ═════════════════════════════════════════════════════════════

    public async Task<PesaPalStatusResponse> GetTransactionStatusAsync(string orderTrackingId)
    {
        var client = await AuthenticatedClientAsync();

        using var resp = await client.GetAsync(
            $"api/Transactions/GetTransactionStatus?orderTrackingId={Uri.EscapeDataString(orderTrackingId)}");

        var raw = await resp.Content.ReadAsStringAsync();
        _logger.LogDebug("PesaPal GetTransactionStatus raw response: {Raw}", raw);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"PesaPal GetTransactionStatus HTTP {(int)resp.StatusCode}: {raw}");

        var result = JsonSerializer.Deserialize<PesaPalStatusResponse>(raw, _json);

        if (result?.Error is not null)
            throw new InvalidOperationException(
                $"Status check failed: {result.Error.Message}");

        var rawStatus = result!.PaymentStatusDescription ?? "PENDING";
        var domainStatus = _statusMap.GetValueOrDefault(rawStatus, PaymentStatus.Pending);

        if (domainStatus != PaymentStatus.Pending)
        {
            var txn = await _db.PesaPalTransactions
                .FirstOrDefaultAsync(t => t.OrderTrackingId == orderTrackingId);

            if (txn is not null)
            {
                txn.PaymentStatus = domainStatus;
                txn.PaymentMethod = ResolvePaymentMethod(result.PaymentMethod);
                txn.ConfirmationCode = result.ConfirmationCode;
                txn.PaymentAccount = result.PaymentAccount;
                txn.ErrorMessage = domainStatus == PaymentStatus.Failed
                                           ? result.Description ?? result.Message
                                           : null;
                txn.UpdatedOn = DateTime.UtcNow;
                txn.CompletedOn = domainStatus == PaymentStatus.Completed
                                           ? DateTime.UtcNow
                                           : txn.CompletedOn;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "PesaPal transaction updated. TrackingId: {Id}, Status: {Status}",
                    orderTrackingId, domainStatus);
            }
        }

        return result;
    }

    // ═════════════════════════════════════════════════════════════
    // 5. List registered IPNs
    // ═════════════════════════════════════════════════════════════

    public async Task<IEnumerable<PesaPalIpnResponse>> GetRegisteredIpnsAsync()
    {
        var client = await AuthenticatedClientAsync();

        using var resp = await client.GetAsync("api/URLSetup/GetIpnList");
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"PesaPal GetIpnList HTTP {(int)resp.StatusCode}: {raw}");

        return JsonSerializer.Deserialize<List<PesaPalIpnResponse>>(raw, _json) ?? [];
    }

    // ═════════════════════════════════════════════════════════════
    // Private helpers
    // ═════════════════════════════════════════════════════════════

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient("PesaPal");
        client.BaseAddress = new Uri(_cfg.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var token = await GetTokenAsync();
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static PaymentMethod? ResolvePaymentMethod(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return _methodMap.TryGetValue(raw.Trim(), out var m) ? m : null;
    }

    private static string Serialize<T>(T obj) =>
        JsonSerializer.Serialize(obj, _json);

    /// <summary>
    /// Parses PesaPal's expiry date string, which can be either:
    ///   • ISO-8601:  "2025-01-01T00:00:00"
    ///   • Slash-date: "01/01/2025 00:00:00"
    /// Falls back to 55 minutes from now if parsing fails.
    /// </summary>
    private DateTime TryParseExpiry(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogDebug("PesaPal expiry date missing — defaulting to 55 min.");
            return DateTime.UtcNow.AddMinutes(55);
        }

        // Try ISO-8601 first (most reliable)
        if (DateTime.TryParse(raw, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var iso))
            return iso;

        // Try common slash / dot formats PesaPal has been known to return
        var formats = new[]
        {
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "M/d/yyyy H:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
        };

        if (DateTime.TryParseExact(raw, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var slashDate))
            return slashDate;

        _logger.LogWarning(
            "PesaPal expiry date '{Raw}' could not be parsed — defaulting to 55 min.", raw);

        return DateTime.UtcNow.AddMinutes(55);
    }
}