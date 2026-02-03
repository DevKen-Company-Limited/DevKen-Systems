using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    /// <summary>
    /// Service for seeding subscription data for schools
    /// </summary>
    public class SubscriptionSeedService : ISubscriptionSeedService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubscriptionSeedService> _logger;

        public SubscriptionSeedService(
            AppDbContext context,
            ILogger<SubscriptionSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────
        // SEED TRIAL SUBSCRIPTION
        // Called automatically when a new school registers
        // ─────────────────────────────────────────────────────────
        public async Task<Subscription> SeedTrialSubscriptionAsync(Guid schoolId)
        {
            try
            {
                // Check if school exists
                var schoolExists = await _context.Schools.AnyAsync(s => s.Id == schoolId);
                if (!schoolExists)
                {
                    _logger.LogError("Cannot seed subscription: School {SchoolId} not found", schoolId);
                    throw new InvalidOperationException($"School with ID {schoolId} not found");
                }

                // Check if subscription already exists
                if (await HasSubscriptionAsync(schoolId))
                {
                    _logger.LogWarning("School {SchoolId} already has a subscription", schoolId);
                    var existing = await _context.Subscriptions
                        .FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.IsActive);
                    return existing!;
                }

                var startDate = DateTime.UtcNow;
                var expiryDate = startDate.AddDays(30); // 30-day trial

                var trialSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    Plan = SubscriptionPlan.Trial,
                    BillingCycle = BillingCycle.Monthly,
                    StartDate = startDate,
                    ExpiryDate = expiryDate,
                    AutoRenew = false,
                    MaxStudents = 100,      // Limited students for trial
                    MaxTeachers = 10,       // Limited teachers for trial
                    MaxStorageGB = 5,       // Limited storage for trial
                    EnabledFeatures = "BasicReports,StudentManagement,TeacherManagement",
                    Amount = 0,             // Free trial
                    Currency = "KES",
                    Status = SubscriptionStatus.Active,
                    GracePeriodDays = 7,
                    AdminNotes = "Automatically created trial subscription during school registration"
                };

                _context.Subscriptions.Add(trialSubscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Trial subscription created for school {SchoolId}. Expires: {ExpiryDate}",
                    schoolId,
                    expiryDate);

                return trialSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding trial subscription for school {SchoolId}", schoolId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────
        // SEED DEMO SUBSCRIPTIONS
        // For testing purposes - creates multiple subscription scenarios
        // ─────────────────────────────────────────────────────────
        public async Task<bool> SeedDemoSubscriptionsAsync(Guid schoolId)
        {
            try
            {
                // Check if school exists
                var schoolExists = await _context.Schools.AnyAsync(s => s.Id == schoolId);
                if (!schoolExists)
                {
                    _logger.LogError("Cannot seed demo subscriptions: School {SchoolId} not found", schoolId);
                    return false;
                }

                // Deactivate any existing subscriptions
                var existing = await _context.Subscriptions
                    .Where(s => s.SchoolId == schoolId)
                    .ToListAsync();

                foreach (var sub in existing)
                {
                    sub.IsActive = false;
                    sub.Status = SubscriptionStatus.Cancelled;
                }

                // Create an active Standard subscription
                var activeSubscription = CreateDemoSubscription(
                    schoolId,
                    SubscriptionPlan.Standard,
                    BillingCycle.Monthly,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddMonths(1),
                    SubscriptionStatus.Active,
                    500,
                    50,
                    50,
                    "SMS,Reports,AdvancedAnalytics,StudentManagement,TeacherManagement,ParentPortal",
                    5000,
                    "Active Standard plan - Demo subscription"
                );

                _context.Subscriptions.Add(activeSubscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Demo subscriptions seeded for school {SchoolId}",
                    schoolId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding demo subscriptions for school {SchoolId}", schoolId);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // CHECK IF SUBSCRIPTION EXISTS
        // ─────────────────────────────────────────────────────────
        public async Task<bool> HasSubscriptionAsync(Guid schoolId)
        {
            return await _context.Subscriptions
                .AnyAsync(s => s.SchoolId == schoolId && s.IsActive);
        }

        // ─────────────────────────────────────────────────────────
        // HELPER METHODS
        // ─────────────────────────────────────────────────────────
        private static Subscription CreateDemoSubscription(
            Guid schoolId,
            SubscriptionPlan plan,
            BillingCycle billingCycle,
            DateTime startDate,
            DateTime expiryDate,
            SubscriptionStatus status,
            int maxStudents,
            int maxTeachers,
            decimal maxStorageGB,
            string enabledFeatures,
            decimal amount,
            string adminNotes)
        {
            return new Subscription
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Plan = plan,
                BillingCycle = billingCycle,
                StartDate = startDate,
                ExpiryDate = expiryDate,
                AutoRenew = false,
                MaxStudents = maxStudents,
                MaxTeachers = maxTeachers,
                MaxStorageGB = maxStorageGB,
                EnabledFeatures = enabledFeatures,
                Amount = amount,
                Currency = "KES",
                Status = status,
                GracePeriodDays = 7,
                AdminNotes = adminNotes
            };
        }
    }
}