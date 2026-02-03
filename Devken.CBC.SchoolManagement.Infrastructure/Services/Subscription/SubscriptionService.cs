using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;

using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.DTOs.Subscription.SubscriptionPlans;


namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            AppDbContext context,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────
        // CREATE SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            // Verify school exists
            var schoolExists = await _context.Schools.AnyAsync(s => s.Id == request.SchoolId);
            if (!schoolExists)
                throw new InvalidOperationException($"School with ID {request.SchoolId} not found");

            // Deactivate any existing active subscriptions for this school
            var existingActive = await _context.Subscriptions
                .Where(s => s.SchoolId == request.SchoolId && s.IsActive)
                .ToListAsync();

            foreach (var sub in existingActive)
            {
                sub.IsActive = false;
                sub.Status = SubscriptionStatus.Cancelled;
            }

            // Calculate expiry date based on billing cycle
            var expiryDate = CalculateExpiryDate(request.StartDate, request.BillingCycle);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                SchoolId = request.SchoolId,
                Plan = request.Plan,
                BillingCycle = request.BillingCycle,
                StartDate = request.StartDate,
                ExpiryDate = expiryDate,
                IsActive = true,
                AutoRenew = request.AutoRenew,
                MaxStudents = request.MaxStudents,
                MaxTeachers = request.MaxTeachers,
                MaxStorageGB = request.MaxStorageGB,
                EnabledFeatures = request.EnabledFeatures != null
                    ? string.Join(",", request.EnabledFeatures)
                    : null,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = SubscriptionStatus.Active,
                GracePeriodDays = request.GracePeriodDays,
                AdminNotes = request.AdminNotes
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Subscription created for school {SchoolId}. Plan: {Plan}, Expires: {ExpiryDate}",
                request.SchoolId,
                request.Plan,
                expiryDate);

            return subscription;
        }

        // ─────────────────────────────────────────────────────────
        // UPDATE SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription?> UpdateSubscriptionAsync(
            Guid subscriptionId,
            UpdateSubscriptionRequest request)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return null;

            // Update only provided fields
            if (request.Plan.HasValue)
                subscription.Plan = request.Plan.Value;

            if (request.MaxStudents.HasValue)
                subscription.MaxStudents = request.MaxStudents.Value;

            if (request.MaxTeachers.HasValue)
                subscription.MaxTeachers = request.MaxTeachers.Value;

            if (request.MaxStorageGB.HasValue)
                subscription.MaxStorageGB = request.MaxStorageGB.Value;

            if (request.EnabledFeatures != null)
                subscription.EnabledFeatures = string.Join(",", request.EnabledFeatures);

            if (request.AutoRenew.HasValue)
                subscription.AutoRenew = request.AutoRenew.Value;

            if (request.GracePeriodDays.HasValue)
                subscription.GracePeriodDays = request.GracePeriodDays.Value;

            if (request.ExpiryDate.HasValue)
                subscription.ExpiryDate = request.ExpiryDate.Value;

            if (request.AdminNotes != null)
                subscription.AdminNotes = request.AdminNotes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubscriptionId} updated", subscriptionId);

            return subscription;
        }

        // ─────────────────────────────────────────────────────────
        // SUSPEND SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<bool> SuspendSubscriptionAsync(Guid subscriptionId, string reason)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Suspended;
            subscription.AdminNotes = $"[SUSPENDED] {reason}\n{subscription.AdminNotes}";

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Subscription {SubscriptionId} suspended. Reason: {Reason}",
                subscriptionId,
                reason);

            return true;
        }

        // ─────────────────────────────────────────────────────────
        // ACTIVATE SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<bool> ActivateSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Active;
            subscription.IsActive = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubscriptionId} activated", subscriptionId);

            return true;
        }

        // ─────────────────────────────────────────────────────────
        // CANCEL SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.IsActive = false;
            subscription.AutoRenew = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubscriptionId} cancelled", subscriptionId);

            return true;
        }

        // ─────────────────────────────────────────────────────────
        // GET ALL SUBSCRIPTIONS (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<List<Subscription>> GetAllSubscriptionsAsync(SubscriptionFilter? filter = null)
        {
            var query = _context.Subscriptions
                .Include(s => s.School)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.Status.HasValue)
                    query = query.Where(s => s.Status == filter.Status.Value);

                if (filter.Plan.HasValue)
                    query = query.Where(s => s.Plan == filter.Plan.Value);

                if (filter.IsExpiringSoon == true)
                {
                    var threshold = DateTime.UtcNow.AddDays(7);
                    query = query.Where(s => s.ExpiryDate <= threshold && s.ExpiryDate > DateTime.UtcNow);
                }

                if (filter.IsExpired == true)
                    query = query.Where(s => s.ExpiryDate < DateTime.UtcNow);

                if (filter.ExpiringBefore.HasValue)
                    query = query.Where(s => s.ExpiryDate < filter.ExpiringBefore.Value);
            }

            return await query
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────
        // GET SCHOOL SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription?> GetSchoolSubscriptionAsync(Guid schoolId)
        {
            return await _context.Subscriptions
                .Include(s => s.School)
                .Where(s => s.SchoolId == schoolId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();
        }

        // ─────────────────────────────────────────────────────────
        // GET MY SUBSCRIPTION (School can view their own)
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription?> GetMySubscriptionAsync(Guid schoolId)
        {
            return await _context.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.IsActive)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();
        }

        // ─────────────────────────────────────────────────────────
        // RENEW SUBSCRIPTION (SuperAdmin Only)
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription> RenewSubscriptionAsync(Guid subscriptionId, BillingCycle newCycle)
        {
            var oldSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (oldSubscription == null)
                throw new InvalidOperationException($"Subscription {subscriptionId} not found");

            // Deactivate old subscription
            oldSubscription.IsActive = false;
            oldSubscription.Status = SubscriptionStatus.Expired;

            // Create new subscription with same details
            var startDate = DateTime.UtcNow;
            var expiryDate = CalculateExpiryDate(startDate, newCycle);

            var newSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                SchoolId = oldSubscription.SchoolId,
                Plan = oldSubscription.Plan,
                BillingCycle = newCycle,
                StartDate = startDate,
                ExpiryDate = expiryDate,
                IsActive = true,
                AutoRenew = oldSubscription.AutoRenew,
                MaxStudents = oldSubscription.MaxStudents,
                MaxTeachers = oldSubscription.MaxTeachers,
                MaxStorageGB = oldSubscription.MaxStorageGB,
                EnabledFeatures = oldSubscription.EnabledFeatures,
                Amount = oldSubscription.Amount,
                Currency = oldSubscription.Currency,
                Status = SubscriptionStatus.Active,
                GracePeriodDays = oldSubscription.GracePeriodDays,
                AdminNotes = $"Renewed from subscription {subscriptionId}"
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Subscription renewed for school {SchoolId}. Old: {OldId}, New: {NewId}",
                oldSubscription.SchoolId,
                subscriptionId,
                newSubscription.Id);

            return newSubscription;
        }

        // ─────────────────────────────────────────────────────────
        // CHECK ACTIVE SUBSCRIPTION (System)
        // ─────────────────────────────────────────────────────────
        public async Task<bool> HasActiveSubscriptionAsync(Guid schoolId)
        {
            var subscription = await _context.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.IsActive)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();

            return subscription?.CanAccess ?? false;
        }

        // ─────────────────────────────────────────────────────────
        // CHECK FEATURE ACCESS (System)
        // ─────────────────────────────────────────────────────────
        public async Task<bool> HasFeatureAccessAsync(Guid schoolId, string featureName)
        {
            var subscription = await _context.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.IsActive)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();

            if (subscription == null || !subscription.CanAccess)
                return false;

            if (string.IsNullOrWhiteSpace(subscription.EnabledFeatures))
                return false;

            var features = subscription.EnabledFeatures
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim());

            return features.Contains(featureName, StringComparer.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────
        private static DateTime CalculateExpiryDate(DateTime startDate, BillingCycle cycle)
        {
            return cycle switch
            {
                BillingCycle.Daily => startDate.AddDays(1),
                BillingCycle.Weekly => startDate.AddDays(7),
                BillingCycle.Monthly => startDate.AddMonths(1),
                BillingCycle.Quarterly => startDate.AddMonths(3),
                BillingCycle.Yearly => startDate.AddYears(1),
                _ => startDate.AddMonths(1) // Default to monthly
            };
        }
    }
}