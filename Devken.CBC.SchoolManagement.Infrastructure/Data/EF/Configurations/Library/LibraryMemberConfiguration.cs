using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class LibraryMemberConfiguration : IEntityTypeConfiguration<LibraryMember>
    {
        private readonly TenantContext _tenantContext;

        public LibraryMemberConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<LibraryMember> builder)
        {
            builder.ToTable("LibraryMembers");

            builder.HasKey(m => m.Id);

            // ── Global query filter (tenant isolation) ────────────────────────
            builder.HasQueryFilter(m =>
                _tenantContext.TenantId == null ||
                m.TenantId == _tenantContext.TenantId);

            // ── Indexes ───────────────────────────────────────────────────────
            // Unique MemberNumber per school
            builder.HasIndex(m => new { m.TenantId, m.MemberNumber }).IsUnique();
            // One membership per user per school
            builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();

            // ── Properties ────────────────────────────────────────────────────
            builder.Property(m => m.MemberNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(m => m.MemberType)
                   .IsRequired()
                   .HasConversion<string>();

            // ── Relationships ─────────────────────────────────────────────────
            // LibraryMember.UserId is a plain FK column — no navigation property exists
            // on the entity (User has a global query filter which would cause EF 10622).
            // EF Core will track it as a shadow FK. The DB constraint is still enforced.
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(m => m.UserId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(m => m.BorrowTransactions)
                   .WithOne(b => b.Member)
                   .HasForeignKey(b => b.MemberId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}