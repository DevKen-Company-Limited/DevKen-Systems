// ═══════════════════════════════════════════════════════════════════
// PesaPalController.cs
// Place in: Devken.CBC.SchoolManagement.Api/Controllers/Finance/
// ═══════════════════════════════════════════════════════════════════

using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance;

[Route("api/pesapal")]
[ApiController]
public sealed class PesaPalController(
    IPesaPalService pesaPalService,
    IUserActivityService? activityService = null)
    : BaseApiController(activityService)
{

    // ═════════════════════════════════════════════════════════════
    // SETTINGS
    // ═════════════════════════════════════════════════════════════

    [HttpGet("settings")]
    [Authorize]
    public IActionResult GetSettings(
        [FromServices] IOptionsSnapshot<PesaPalSettings> snapshot)
    {
        try
        {
            var cfg = snapshot.Value;

            var result = new PesaPalSettingsResponseDto
            {
                Environment = cfg.BaseUrl.Contains("cybqa", StringComparison.OrdinalIgnoreCase)
                    ? "Sandbox"
                    : "Production",

                ConsumerKey = cfg.ConsumerKey,
                ConsumerSecret = "••••••••",
                BaseUrl = cfg.BaseUrl,
                IpnUrl = cfg.IpnUrl,
                CallbackUrl = cfg.CallbackUrl,
                IpnRegistered = !string.IsNullOrWhiteSpace(cfg.RegisteredIpnId),
                IpnId = cfg.RegisteredIpnId
            };

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> SaveSettings(
        [FromBody] PesaPalSettingsSaveDto dto,
        [FromServices] IWritablePesaPalSettings writable)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse(ModelState);

        try
        {
            const string masked = "••••••••";

            await writable.UpdateAsync(s =>
            {
                s.ConsumerKey = dto.ConsumerKey;

                if (!string.IsNullOrWhiteSpace(dto.ConsumerSecret)
                    && dto.ConsumerSecret != masked)
                {
                    s.ConsumerSecret = dto.ConsumerSecret;
                }

                s.BaseUrl = dto.BaseUrl;
                s.IpnUrl = dto.IpnUrl;
                s.CallbackUrl = dto.CallbackUrl;
            });

            return SuccessResponse(masked, "Settings updated successfully.");
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // TOKEN
    // ═════════════════════════════════════════════════════════════

    [HttpPost("token")]
    [Authorize]
    public async Task<IActionResult> GetToken()
    {
        try
        {
            var token = await pesaPalService.GetTokenAsync();
            return SuccessResponse(new { token });
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // IPN
    // ═════════════════════════════════════════════════════════════

    [HttpPost("ipn/register")]
    [Authorize]
    public async Task<IActionResult> RegisterIpn(
        [FromServices] IOptionsSnapshot<PesaPalSettings> snapshot)
    {
        try
        {
            var ipnId = await pesaPalService.RegisterIpnAsync();

            var result = new PesaPalIpnResponse
            {
                IpnId = ipnId,
                Url = snapshot.Value.IpnUrl,
                CreatedDate = DateTime.UtcNow.ToString("o"),
                IpnNotificationType = "GET"
            };

            return SuccessResponse(result, "IPN registered successfully.");
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    [HttpGet("ipn/list")]
    [Authorize]
    public async Task<IActionResult> GetIpnList()
    {
        try
        {
            var list = await pesaPalService.GetRegisteredIpnsAsync();
            return SuccessResponse(list);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    [HttpGet("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> IpnCallback([FromQuery] PesaPalIpnCallbackDto dto)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(dto.OrderTrackingId))
            {
                await pesaPalService.GetTransactionStatusAsync(dto.OrderTrackingId);
            }
        }
        catch
        {
            // IPN must always return HTTP 200
        }

        return Ok(new
        {
            dto.OrderNotificationType,
            dto.OrderTrackingId,
            dto.OrderMerchantReference
        });
    }

    // ═════════════════════════════════════════════════════════════
    // ORDER SUBMISSION
    // ═════════════════════════════════════════════════════════════

    [HttpPost("order/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequestDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse(ModelState);

        try
        {
            var result = await pesaPalService.SubmitOrderAsync(dto);

            await LogUserActivityAsync("pesapal.order.submit",
                $"Order submitted — Ref: {dto.Id}, Amount: {dto.Amount:N2}");

            return SuccessResponse(new
            {
                redirectUrl = result.RedirectUrl,
                orderTrackingId = result.OrderTrackingId,
                merchantReference = result.MerchantReference
            });
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // TRANSACTION STATUS
    // ═════════════════════════════════════════════════════════════

    [HttpGet("order/status")]
    [Authorize]
    public async Task<IActionResult> GetStatus([FromQuery] string orderTrackingId)
    {
        if (string.IsNullOrWhiteSpace(orderTrackingId))
            return BadRequest("orderTrackingId is required.");

        try
        {
            PesaPalStatusResponse status =
                await pesaPalService.GetTransactionStatusAsync(orderTrackingId);

            return SuccessResponse(status);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // CALLBACK
    // ═════════════════════════════════════════════════════════════

    [HttpGet("callback")]
    [AllowAnonymous]
    public IActionResult Callback(
        [FromQuery] string? orderTrackingId,
        [FromQuery] string? orderMerchantReference)
    {
        return Ok(new
        {
            orderTrackingId,
            orderMerchantReference,
            message = "Callback received."
        });
    }

    // ═════════════════════════════════════════════════════════════
    // TRANSACTION LOG
    // ═════════════════════════════════════════════════════════════

    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> GetTransactions(
        [FromServices] IPesaPalTransactionQuery query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            PesaPalTransactionPageDto result =
                await query.GetPagedAsync(page, pageSize, status);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }
}