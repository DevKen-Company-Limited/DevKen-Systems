using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        private readonly TenantContext _tenantContext;

        public UserRoleConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(ur => ur.Id);

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                   .IsUnique();

            builder.Property(ur => ur.UserId)
                   .IsRequired();

            builder.Property(ur => ur.RoleId)
                   .IsRequired();

            // 🚨 THIS IS CRITICAL
            builder.HasOne(ur => ur.User)
                   .WithMany(u => u.UserRoles)
                   .HasForeignKey(ur => ur.UserId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ur => ur.Role)
                   .WithMany(r => r.UserRoles)
                   .HasForeignKey(ur => ur.RoleId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(ur =>
                _tenantContext.TenantId == null ||
                ur.TenantId == _tenantContext.TenantId);
        }
    }
}
