using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.Extensions.Logging;


namespace Devken.CBC.SchoolManagement.Infrastructure
{
    public class UserActivityService : IUserActivityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserActivityService> _logger;

        public UserActivityService(
            AppDbContext context,
            ILogger<UserActivityService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogActivityAsync(
            Guid userId,
            Guid? tenantId,
            string activityType,
            string? details = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(activityType))
                throw new ArgumentException("Activity type cannot be null or empty.", nameof(activityType));

            var activity = new UserActivity
            {
                UserId = userId,
                TenantId = tenantId,
                ActivityType = activityType.Trim(),
                ActivityDetails = details?.Trim() ?? string.Empty
            };

            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Activity logged | User: {UserId} | Tenant: {TenantId} | Type: {ActivityType}",
                userId,
                tenantId?.ToString() ?? "N/A",
                activityType);
        }
    }
}
