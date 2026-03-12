using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using ValidationException = Devken.CBC.SchoolManagement.Application.Exceptions.ValidationException;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance;

public sealed class PaymentService(IRepositoryManager repo, AppDbContext db) : IPaymentService
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────────

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

    private IQueryable<Payment> WithIncludes(bool track) =>
        repo.Payment
            .FindAll(track)
            .Include(p => p.Student)
            .Include(p => p.Invoice)
            .Include(p => p.ReceivedByStaff);

    private static void EnforceTenantAccess(Guid paymentTenantId, Guid? userSchoolId, bool isSuperAdmin)
    {
        if (!isSuperAdmin && paymentTenantId != userSchoolId)
            throw new UnauthorizedException("You do not have access to this payment.");
    }

    private static Guid ResolveTenant(Guid? dtoTenantId, Guid? userSchoolId, bool isSuperAdmin)
    {
        if (!isSuperAdmin)
            return userSchoolId!.Value;

        if (dtoTenantId is null || dtoTenantId == Guid.Empty)
            throw new ValidationException("TenantId is required for SuperAdmin.");

        return dtoTenantId.Value;
    }

    private async Task<string> NextReferenceAsync(Guid tenantId)
    {
        try
        {
            return await repo.DocumentNumberSeries.GenerateAsync("PAYMENT", tenantId);
        }
        catch (InvalidOperationException)
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10_000, 99_999)}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // In-memory paired sort
    //
    // Sorts originals by CreatedOn DESCENDING (newest first).
    // Each original's reversal entry is inserted directly below it.
    // This runs after the full filtered set is fetched; Skip/Take is applied
    // on the sorted list so pagination respects the paired ordering.
    // ─────────────────────────────────────────────────────────────────────────────
    private static List<Payment> SortPaired(List<Payment> payments)
    {
        // Index all reversal entries by the original payment Id they belong to
        var reversalMap = payments
            .Where(p => p.IsReversal && p.ReversedFromPaymentId.HasValue)
            .ToDictionary(p => p.ReversedFromPaymentId!.Value);

        // Sort originals newest-first
        var originals = payments
            .Where(p => !p.IsReversal)
            .OrderByDescending(p => p.CreatedOn)
            .ToList();

        var result = new List<Payment>(payments.Count);

        foreach (var original in originals)
        {
            result.Add(original);
            // If a paired reversal entry exists, insert it directly below its original
            if (reversalMap.TryGetValue(original.Id!, out var reversal))
                result.Add(reversal);
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync(
        Guid? schoolId, Guid? studentId, Guid? invoiceId,
        PaymentMethod? method, PaymentStatus? status,
        DateTime? from, DateTime? to, bool? isReversal,
        Guid? userSchoolId, bool isSuperAdmin)
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

        var raw = await query.ToListAsync();
        return SortPaired(raw).Select(Map).ToList();
    }

    public async Task<PaymentPagedResultDto> GetPagedAsync(
        Guid? schoolId, Guid? studentId, Guid? invoiceId,
        PaymentMethod? method, PaymentStatus? status,
        DateTime? from, DateTime? to, bool? isReversal,
        string? search,
        int page, int pageSize,
        Guid? userSchoolId, bool isSuperAdmin)
    {
        var targetSchoolId = isSuperAdmin ? schoolId : userSchoolId;

        // ── Base query (no navigation includes — used for stats) ──────────
        var baseQuery = repo.Payment.FindAll(false);

        // ── Filters ──────────────────────────────────────────────────────
        if (targetSchoolId.HasValue) baseQuery = baseQuery.Where(p => p.TenantId == targetSchoolId.Value);
        if (studentId.HasValue) baseQuery = baseQuery.Where(p => p.StudentId == studentId.Value);
        if (invoiceId.HasValue) baseQuery = baseQuery.Where(p => p.InvoiceId == invoiceId.Value);
        if (method.HasValue) baseQuery = baseQuery.Where(p => p.PaymentMethod == method.Value);
        if (status.HasValue) baseQuery = baseQuery.Where(p => p.StatusPayment == status.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(p => p.PaymentDate <= to.Value);
        if (isReversal.HasValue) baseQuery = baseQuery.Where(p => p.IsReversal == isReversal.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(p =>
                p.PaymentReference.Contains(search) ||
                (p.MpesaCode != null && p.MpesaCode.Contains(search)) ||
                (p.TransactionReference != null && p.TransactionReference.Contains(search)) ||
                (p.Student != null && p.Student.FirstName.Contains(search)) ||
                (p.Student != null && p.Student.LastName.Contains(search)) ||
                (p.Student != null && p.Student.AdmissionNumber != null
                                   && p.Student.AdmissionNumber.Contains(search)) ||
                (p.Invoice != null && p.Invoice.InvoiceNumber.Contains(search)));
        }

        // ── Stats — run on the full filtered set before paging ────────────
        var totalCount = await baseQuery.CountAsync();
        var reversalCount = await baseQuery.CountAsync(p => p.IsReversal);

        var pendingCount = await baseQuery.CountAsync(
            p => p.StatusPayment == PaymentStatus.Pending && !p.IsReversal);

        var mpesaCount = await baseQuery.CountAsync(
            p => p.PaymentMethod == PaymentMethod.Mpesa && !p.IsReversal);

        var collected = await baseQuery
            .Where(p => !p.IsReversal &&
                        (p.StatusPayment == PaymentStatus.Completed ||
                         p.StatusPayment == PaymentStatus.Reversed))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var reversedSum = await baseQuery
            .Where(p => p.IsReversal)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;  // stored as negative

        var schoolCount = isSuperAdmin
            ? await baseQuery.Select(p => p.TenantId).Distinct().CountAsync()
            : 0;

        // ── Fetch all matching rows with navigation includes ───────────────
        // We must fetch the full set, sort in-memory to preserve pair ordering,
        // then apply Skip/Take so that page boundaries are always correct and
        // an original never gets separated from its reversal entry mid-page.
        pageSize = Math.Clamp(pageSize, 5, 100);
        page = Math.Max(1, page);

        var allItems = await baseQuery
            .Include(p => p.Student)
            .Include(p => p.Invoice)
            .Include(p => p.ReceivedByStaff)
            .ToListAsync();

        // Sort paired in-memory, then page the sorted list
        var sorted = SortPaired(allItems);

        var items = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // totalCount reflects actual DB rows; totalPages is derived from sorted count
        // (sorted.Count == totalCount since SortPaired never drops items)
        var totalPages = (int)Math.Ceiling(sorted.Count / (double)pageSize);

        // And remove TotalPages from the return object
        return new PaymentPagedResultDto
        {
            Items = items.Select(Map).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            // TotalPages        ← DELETE, it's a computed property on the record
            TotalCollected = collected,
            TotalReversed = -reversedSum,
            NetAvailable = collected + reversedSum,
            PendingCount = pendingCount,
            MpesaCount = mpesaCount,
            TotalReversalCount = reversalCount,
            SchoolCount = schoolCount,
        };
    }

    public async Task<PaymentResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
    {
        var payment = await WithIncludes(false).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Payment '{id}' was not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);
        return Map(payment);
    }

    public async Task<PaymentResponseDto> GetByReferenceAsync(
        string reference, Guid? userSchoolId, bool isSuperAdmin)
    {
        var payment = await WithIncludes(false)
            .FirstOrDefaultAsync(p => p.PaymentReference == reference)
            ?? throw new NotFoundException($"Payment reference '{reference}' was not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);
        return Map(payment);
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetByStudentAsync(
        Guid studentId, Guid? userSchoolId, bool isSuperAdmin)
    {
        var query = WithIncludes(false).Where(p => p.StudentId == studentId);
        if (!isSuperAdmin) query = query.Where(p => p.TenantId == userSchoolId);

        var raw = await query.ToListAsync();
        return SortPaired(raw).Select(Map).ToList();
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetByInvoiceAsync(
        Guid invoiceId, Guid? userSchoolId, bool isSuperAdmin)
    {
        var query = WithIncludes(false).Where(p => p.InvoiceId == invoiceId);
        if (!isSuperAdmin) query = query.Where(p => p.TenantId == userSchoolId);

        var raw = await query.ToListAsync();
        return SortPaired(raw).Select(Map).ToList();
    }

    public async Task<object> GetSummaryAsync(
        Guid? schoolId, Guid? studentId,
        DateTime? from, DateTime? to,
        Guid? userSchoolId, bool isSuperAdmin)
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
                .Select(g => new { Method = g.Key.ToString(), Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync(),
            ByStatus = await query
                .GroupBy(p => p.StatusPayment)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Commands — all wrapped in CreateExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────────

    public async Task<PaymentResponseDto> CreateAsync(
        CreatePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId)
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

        PaymentResponseDto result = null!;

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
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
                result = Map(saved);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        return result;
    }

    public async Task<PaymentResponseDto> UpdateAsync(
        Guid id, UpdatePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId)
    {
        var payment = await WithIncludes(true).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Payment '{id}' not found.");

        EnforceTenantAccess(payment.TenantId, userSchoolId, isSuperAdmin);

        if (payment.IsReversal)
            throw new ValidationException("Reversal payments cannot be edited.");

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

        PaymentResponseDto result = null!;

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                repo.Payment.Update(payment);
                await repo.SaveAsync();
                await tx.CommitAsync();

                var updated = await WithIncludes(false).FirstAsync(p => p.Id == id);
                result = Map(updated);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        return result;
    }

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

        if (payment.IsReversal)
            throw new ValidationException(
                "Reversal entries cannot be deleted. They are permanent audit records.");

        if (payment.StatusPayment == PaymentStatus.Reversed)
            throw new ValidationException(
                "This payment has been reversed and cannot be deleted.");

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                repo.Payment.Delete(payment);
                await repo.SaveAsync();
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        });
    }

    public async Task<PaymentResponseDto> ReverseAsync(
        Guid id, ReversePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId)
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

        PaymentResponseDto result = null!;

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var reversal = new Payment
                {
                    Id = Guid.NewGuid(),
                    TenantId = original.TenantId,
                    PaymentReference = await NextReferenceAsync(original.TenantId),
                    StudentId = original.StudentId,
                    InvoiceId = original.InvoiceId,
                    ReceivedBy = dto.ReceivedBy ?? original.ReceivedBy,
                    PaymentDate = now,
                    Amount = -original.Amount,
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
                result = Map(saved);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        return result;
    }

    public async Task<BulkPaymentResultDto> BulkCreateAsync(
        BulkPaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId)
    {
        var tenantId = ResolveTenant(dto.TenantId, userSchoolId, isSuperAdmin);

        if (dto.Payments is not { Count: > 0 })
            throw new ValidationException("At least one payment item is required.");

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

            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();
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
                    await tx.CommitAsync();

                    var saved = await WithIncludes(false).FirstAsync(p => p.Id == payment.Id);
                    result.CreatedPayments.Add(Map(saved));
                    result.TotalAmountPosted += item.Amount;
                    result.Succeeded++;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    result.Failed++;
                    result.Errors.Add(new BulkPaymentErrorDto
                    {
                        StudentId = item.StudentId,
                        InvoiceId = item.InvoiceId,
                        Reason = ex.Message,
                    });
                }
            });
        }

        return result;
    }
}