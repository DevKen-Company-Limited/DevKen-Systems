using System.ComponentModel.DataAnnotations;
using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = Devken.CBC.SchoolManagement.Application.Exceptions.ValidationException;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance;

/// <summary>
/// Handles all payment operations: queries, creation, updates, reversal, and bulk processing.
/// </summary>
public sealed class PaymentService(IRepositoryManager repo) : IPaymentService
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="Payment"/> entity to its response DTO.</summary>
    private static PaymentResponseDto Map(Payment p) => new()
    {
        Id = p.Id!,
        TenantId = p.TenantId,
        PaymentReference = p.PaymentReference,
        ReceiptNumber = p.ReceiptNumber,
        StudentId = p.StudentId,
        StudentName = p.Student is { } s ? $"{s.FirstName} {s.LastName}" : null,
        AdmissionNumber = p.Student?.AdmissionNumber,
        InvoiceId = p.InvoiceId,
        InvoiceNumber = p.Invoice?.InvoiceNumber,
        ReceivedBy = p.ReceivedBy,
        ReceivedByName = p.ReceivedByStaff is { } staff
                                    ? $"{staff.FirstName} {staff.LastName}"
                                    : null,
        PaymentDate = p.PaymentDate,
        ReceivedDate = p.ReceivedDate,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod.ToString(),
        StatusPayment = p.StatusPayment.ToString(),
        TransactionReference = p.TransactionReference,
        Description = p.Description,
        Notes = p.Notes,
        MpesaCode = p.MpesaCode,
        PhoneNumber = p.PhoneNumber,
        BankName = p.BankName,
        AccountNumber = p.AccountNumber,
        ChequeNumber = p.ChequeNumber,
        ChequeClearanceDate = p.ChequeClearanceDate,
        ReversedFromPaymentId = p.ReversedFromPaymentId,
        IsReversal = p.IsReversal,
        ReversalReason = p.ReversalReason,
        IsCompleted = p.IsCompleted,
        IsMpesa = p.IsMpesa,
        CreatedOn = p.CreatedOn,
        UpdatedOn = p.UpdatedOn,
        CreatedBy = p.CreatedBy,
        UpdatedBy = p.UpdatedBy,
    };

    /// <summary>
    /// Returns a queryable of <see cref="Payment"/> with all navigation properties eagerly loaded.
    /// </summary>
    private IQueryable<Payment> WithIncludes(bool track) =>
        repo.Payment
            .FindAll(track)
            .Include(p => p.Student)
            .Include(p => p.Invoice)
            .Include(p => p.ReceivedByStaff);

    /// <summary>Throws <see cref="UnauthorizedException"/> when the caller cannot access the payment's tenant.</summary>
    private static void EnforceTenantAccess(Guid paymentTenantId, Guid? userSchoolId, bool isSuperAdmin)
    {
        if (!isSuperAdmin && paymentTenantId != userSchoolId)
            throw new UnauthorizedException("You do not have access to this payment.");
    }

    /// <summary>
    /// Returns the effective tenant ID: the calling user's school for regular users,
    /// or the DTO-supplied value for super-admins.
    /// </summary>
    private static Guid ResolveTenant(Guid? dtoTenantId, Guid? userSchoolId, bool isSuperAdmin)
    {
        if (!isSuperAdmin)
            return userSchoolId!.Value;

        if (dtoTenantId is null || dtoTenantId == Guid.Empty)
            throw new ValidationException("TenantId is required for SuperAdmin.");

        return dtoTenantId.Value;
    }

    /// <summary>Generates the next payment reference number, with a safe fallback when no series is configured.</summary>
    private async Task<string> NextReferenceAsync(Guid tenantId)
    {
        try
        {
            return await repo.DocumentNumberSeries.GenerateAsync("PAYMENT", tenantId);
        }
        catch (InvalidOperationException)
        {
            // Series not yet configured for this tenant — safe fallback
            return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10_000, 99_999)}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync(
        Guid? schoolId,
        Guid? studentId,
        Guid? invoiceId,
        PaymentMethod? method,
        PaymentStatus? status,
        DateTime? from,
        DateTime? to,
        bool? isReversal,
        Guid? userSchoolId,
        bool isSuperAdmin)
    {
        var targetSchoolId = isSuperAdmin ? schoolId : userSchoolId;

        var query = WithIncludes(false);

        if (targetSchoolId.HasValue) query = query.Where(p => p.TenantId == targetSchoolId.Value);
        if (studentId.HasValue) query = query.Where(p => p.StudentId == studentId.Value);
        if (invoiceId.HasValue) query = query.Where(p => p.InvoiceId == invoiceId.Value);
        if (method.HasValue) query = query.Where(p => p.PaymentMethod == method.Value);
        if (status.HasValue) query = query.Where(p => p.StatusPayment == status.Value);
        if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value);
        if (isReversal.HasValue) query = query.Where(p => p.IsReversal == isReversal.Value);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => Map(p))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<PaymentResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
    {
        var payment = await WithIncludes(false).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Payment '{id}' was not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);
        return Map(payment);
    }

    /// <inheritdoc/>
    public async Task<PaymentResponseDto> GetByReferenceAsync(
        string reference, Guid? userSchoolId, bool isSuperAdmin)
    {
        var payment = await WithIncludes(false)
            .FirstOrDefaultAsync(p => p.PaymentReference == reference)
            ?? throw new NotFoundException($"Payment reference '{reference}' was not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);
        return Map(payment);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PaymentResponseDto>> GetByStudentAsync(
        Guid studentId, Guid? userSchoolId, bool isSuperAdmin)
    {
        var query = WithIncludes(false).Where(p => p.StudentId == studentId);

        if (!isSuperAdmin)
            query = query.Where(p => p.TenantId == userSchoolId);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => Map(p))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PaymentResponseDto>> GetByInvoiceAsync(
        Guid invoiceId, Guid? userSchoolId, bool isSuperAdmin)
    {
        var query = WithIncludes(false).Where(p => p.InvoiceId == invoiceId);

        if (!isSuperAdmin)
            query = query.Where(p => p.TenantId == userSchoolId);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => Map(p))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<object> GetSummaryAsync(
        Guid? schoolId,
        Guid? studentId,
        DateTime? from,
        DateTime? to,
        Guid? userSchoolId,
        bool isSuperAdmin)
    {
        var targetSchoolId = isSuperAdmin ? schoolId : userSchoolId;
        var query = repo.Payment.FindAll(false);

        if (targetSchoolId.HasValue) query = query.Where(p => p.TenantId == targetSchoolId.Value);
        if (studentId.HasValue) query = query.Where(p => p.StudentId == studentId.Value);
        if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value);

        var active = query.Where(p => !p.IsReversal);
        var reversed = query.Where(p => p.IsReversal);

        return new
        {
            TotalPayments = await query.CountAsync(),
            TotalActivePayments = await active.CountAsync(),
            TotalReversals = await reversed.CountAsync(),
            TotalAmountPosted = await active.SumAsync(p => (decimal?)p.Amount) ?? 0m,
            TotalAmountReversed = await reversed.SumAsync(p => (decimal?)p.Amount) ?? 0m,
            ByMethod = await active
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key.ToString(),
                    Count = g.Count(),
                    Total = g.Sum(p => p.Amount),
                })
                .ToListAsync(),
            ByStatus = await query
                .GroupBy(p => p.StatusPayment)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    Total = g.Sum(p => p.Amount),
                })
                .ToListAsync(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<PaymentResponseDto> CreateAsync(
        CreatePaymentDto dto,
        Guid? userSchoolId,
        bool isSuperAdmin,
        Guid currentUserId)
    {
        var tenantId = ResolveTenant(dto.TenantId, userSchoolId, isSuperAdmin);

        if (dto.PaymentMethod == PaymentMethod.Mpesa && string.IsNullOrWhiteSpace(dto.MpesaCode))
            throw new ValidationException("MpesaCode is required when PaymentMethod is M-Pesa.");

        var invoiceExists = await repo.Invoice
            .FindByCondition(i => i.Id == dto.InvoiceId && i.TenantId == tenantId, false)
            .AnyAsync();
        if (!invoiceExists)
            throw new NotFoundException($"Invoice '{dto.InvoiceId}' not found for this school.");

        var studentExists = await repo.Student
            .FindByCondition(s => s.Id == dto.StudentId && s.TenantId == tenantId, false)
            .AnyAsync();
        if (!studentExists)
            throw new NotFoundException($"Student '{dto.StudentId}' not found for this school.");

        if (!string.IsNullOrWhiteSpace(dto.MpesaCode))
        {
            var codeUsed = await repo.Payment
                .FindByCondition(p => p.MpesaCode == dto.MpesaCode && p.TenantId == tenantId, false)
                .AnyAsync();
            if (codeUsed)
                throw new ConflictException($"M-Pesa code '{dto.MpesaCode}' has already been used.");
        }

        var now = DateTime.UtcNow;
        var tx = await repo.BeginTransactionAsync();

        try
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PaymentReference = await NextReferenceAsync(tenantId),
                StudentId = dto.StudentId,
                InvoiceId = dto.InvoiceId,
                ReceivedBy = dto.ReceivedBy,
                PaymentDate = dto.PaymentDate,
                ReceivedDate = dto.ReceivedDate,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                StatusPayment = dto.StatusPayment,
                TransactionReference = dto.TransactionReference,
                Description = dto.Description,
                Notes = dto.Notes,
                MpesaCode = dto.MpesaCode,
                PhoneNumber = dto.PhoneNumber,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                ChequeNumber = dto.ChequeNumber,
                ChequeClearanceDate = dto.ChequeClearanceDate,
                IsReversal = false,
                CreatedOn = now,
                UpdatedOn = now,
                CreatedBy = currentUserId,
            };

            repo.Payment.Create(payment);
            await repo.SaveAsync();
            await tx.CommitAsync();

            var saved = await WithIncludes(false).FirstAsync(p => p.Id == payment.Id);
            return Map(saved);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentResponseDto> UpdateAsync(
        Guid id,
        UpdatePaymentDto dto,
        Guid? userSchoolId,
        bool isSuperAdmin,
        Guid currentUserId)
    {
        var payment = await WithIncludes(true).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Payment '{id}' not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);

        if (payment.IsReversal)
            throw new ValidationException("Reversal payments cannot be edited.");

        // Apply only fields that were explicitly provided
        if (dto.PaymentDate.HasValue) payment.PaymentDate = dto.PaymentDate.Value;
        if (dto.ReceivedDate.HasValue) payment.ReceivedDate = dto.ReceivedDate;
        if (dto.Amount.HasValue) payment.Amount = dto.Amount.Value;
        if (dto.PaymentMethod.HasValue) payment.PaymentMethod = dto.PaymentMethod.Value;
        if (dto.StatusPayment.HasValue) payment.StatusPayment = dto.StatusPayment.Value;
        if (dto.ReceivedBy.HasValue) payment.ReceivedBy = dto.ReceivedBy;
        if (dto.TransactionReference != null) payment.TransactionReference = dto.TransactionReference;
        if (dto.Description != null) payment.Description = dto.Description;
        if (dto.Notes != null) payment.Notes = dto.Notes;
        if (dto.MpesaCode != null) payment.MpesaCode = dto.MpesaCode;
        if (dto.PhoneNumber != null) payment.PhoneNumber = dto.PhoneNumber;
        if (dto.BankName != null) payment.BankName = dto.BankName;
        if (dto.AccountNumber != null) payment.AccountNumber = dto.AccountNumber;
        if (dto.ChequeNumber != null) payment.ChequeNumber = dto.ChequeNumber;
        if (dto.ChequeClearanceDate.HasValue) payment.ChequeClearanceDate = dto.ChequeClearanceDate;

        payment.UpdatedOn = DateTime.UtcNow;
        payment.UpdatedBy = currentUserId;

        var tx = await repo.BeginTransactionAsync();
        try
        {
            repo.Payment.Update(payment);
            await repo.SaveAsync();
            await tx.CommitAsync();

            var updated = await WithIncludes(false).FirstAsync(p => p.Id == id);
            return Map(updated);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
    {
        var payment = await repo.Payment
            .FindByCondition(p => p.Id == id, true)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Payment '{id}' not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);

        if (payment.StatusPayment == PaymentStatus.Completed && !isSuperAdmin)
            throw new ValidationException(
                "Completed payments cannot be deleted. Use the reversal endpoint instead.");

        var tx = await repo.BeginTransactionAsync();
        try
        {
            repo.Payment.Delete(payment);
            await repo.SaveAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentResponseDto> ReverseAsync(
        Guid id,
        ReversePaymentDto dto,
        Guid? userSchoolId,
        bool isSuperAdmin,
        Guid currentUserId)
    {
        var original = await WithIncludes(false).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Payment '{id}' not found.");

        EnforceTenantAccess(original.TenantId, userSchoolId, isSuperAdmin);

        if (original.IsReversal)
            throw new ValidationException("Cannot reverse a reversal payment.");

        var alreadyReversed = await repo.Payment
            .FindByCondition(p => p.ReversedFromPaymentId == id, false)
            .AnyAsync();

        if (alreadyReversed)
            throw new ConflictException("This payment has already been reversed.");

        var now = DateTime.UtcNow;
        var tx = await repo.BeginTransactionAsync();

        try
        {
            var reversal = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = original.TenantId,
                PaymentReference = await NextReferenceAsync(original.TenantId),
                StudentId = original.StudentId,
                InvoiceId = original.InvoiceId,
                ReceivedBy = dto.ReceivedBy ?? original.ReceivedBy,
                PaymentDate = now,
                Amount = -original.Amount,   // counter-entry
                PaymentMethod = original.PaymentMethod,
                StatusPayment = PaymentStatus.Reversed,
                Description = $"Reversal of {original.PaymentReference}",
                ReversalReason = dto.ReversalReason,
                ReversedFromPaymentId = original.Id,
                IsReversal = true,
                CreatedOn = now,
                UpdatedOn = now,
                CreatedBy = currentUserId,
            };

            original.StatusPayment = PaymentStatus.Reversed;
            original.UpdatedOn = now;
            original.UpdatedBy = currentUserId;

            repo.Payment.Create(reversal);
            repo.Payment.Update(original);
            await repo.SaveAsync();
            await tx.CommitAsync();

            var saved = await WithIncludes(false).FirstAsync(p => p.Id == reversal.Id);
            return Map(saved);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BulkPaymentResultDto> BulkCreateAsync(
        BulkPaymentDto dto,
        Guid? userSchoolId,
        bool isSuperAdmin,
        Guid currentUserId)
    {
        var tenantId = ResolveTenant(dto.TenantId, userSchoolId, isSuperAdmin);

        if (dto.Payments is not { Count: > 0 })
            throw new ValidationException("At least one payment item is required.");

        // Pre-fetch lookup sets once to avoid N+1 queries per line
        var invoiceIds = dto.Payments.Select(p => p.InvoiceId).Distinct().ToList();
        var studentIds = dto.Payments.Select(p => p.StudentId).Distinct().ToList();

        var validInvoices = await repo.Invoice
            .FindByCondition(i => invoiceIds.Contains(i.Id) && i.TenantId == tenantId, false)
            .Select(i => i.Id)
            .ToHashSetAsync();

        var validStudents = await repo.Student
            .FindByCondition(s => studentIds.Contains(s.Id) && s.TenantId == tenantId, false)
            .Select(s => s.Id)
            .ToHashSetAsync();

        var result = new BulkPaymentResultDto { TotalRequested = dto.Payments.Count };

        foreach (var item in dto.Payments)
        {
            // ── Per-line validation (uses pre-fetched sets — no extra DB round-trip) ──
            string? lineError = null;

            if (!validInvoices.Contains(item.InvoiceId))
                lineError = $"Invoice '{item.InvoiceId}' not found.";

            else if (!validStudents.Contains(item.StudentId))
                lineError = $"Student '{item.StudentId}' not found.";

            else if (dto.PaymentMethod == PaymentMethod.Mpesa && string.IsNullOrWhiteSpace(item.MpesaCode))
                lineError = "MpesaCode is required for M-Pesa payments.";

            else if (!string.IsNullOrWhiteSpace(item.MpesaCode))
            {
                var codeUsed = await repo.Payment
                    .FindByCondition(p => p.MpesaCode == item.MpesaCode && p.TenantId == tenantId, false)
                    .AnyAsync();

                if (codeUsed)
                    lineError = $"M-Pesa code '{item.MpesaCode}' already used.";
            }

            if (lineError is not null)
            {
                result.Errors.Add(new BulkPaymentErrorDto
                {
                    StudentId = item.StudentId,
                    InvoiceId = item.InvoiceId,
                    Reason = lineError,
                });
                result.Failed++;
                continue;
            }

            // ── Per-line transaction: one failure never aborts the remaining items ──
            var itemTx = await repo.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PaymentReference = await NextReferenceAsync(tenantId),
                    StudentId = item.StudentId,
                    InvoiceId = item.InvoiceId,
                    ReceivedBy = dto.ReceivedBy,
                    PaymentDate = dto.PaymentDate,
                    ReceivedDate = now,
                    Amount = item.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    StatusPayment = dto.StatusPayment,
                    Description = dto.Description,
                    Notes = item.Notes,
                    MpesaCode = item.MpesaCode,
                    PhoneNumber = item.PhoneNumber,
                    BankName = dto.BankName,
                    AccountNumber = dto.AccountNumber,
                    TransactionReference = item.TransactionReference,
                    IsReversal = false,
                    CreatedOn = now,
                    UpdatedOn = now,
                    CreatedBy = currentUserId,
                };

                repo.Payment.Create(payment);
                await repo.SaveAsync();
                await itemTx.CommitAsync();

                var saved = await WithIncludes(false).FirstAsync(p => p.Id == payment.Id);
                result.CreatedPayments.Add(Map(saved));
                result.TotalAmountPosted += item.Amount;
                result.Succeeded++;
            }
            catch (Exception ex)
            {
                await itemTx.RollbackAsync();
                result.Failed++;
                result.Errors.Add(new BulkPaymentErrorDto
                {
                    StudentId = item.StudentId,
                    InvoiceId = item.InvoiceId,
                    Reason = ex.Message,
                });
            }
        }

        return result;
    }
}