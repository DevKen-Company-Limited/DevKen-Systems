using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class LibraryFeeConfiguration : IEntityTypeConfiguration<LibraryFee>
    {
        private readonly TenantContext _tenantContext;

        public LibraryFeeConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<LibraryFee> builder)
        {
            builder.ToTable("LibraryFees");

            builder.HasKey(f => f.Id);

            // ── Global query filter (tenant isolation) ────────────────────────
            builder.HasQueryFilter(f =>
                _tenantContext.TenantId == null ||
                f.TenantId == _tenantContext.TenantId);

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(f => new { f.TenantId, f.MemberId });
            builder.HasIndex(f => new { f.TenantId, f.FeeStatus });
            builder.HasIndex(f => f.BookBorrowId);

            // ── Properties ────────────────────────────────────────────────────
            builder.Property(f => f.Amount)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            builder.Property(f => f.AmountPaid)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)")
                   .HasDefaultValue(0m);

            builder.Property(f => f.FeeType)
                   .IsRequired()
                   .HasConversion<string>();

            builder.Property(f => f.FeeStatus)
                   .IsRequired()
                   .HasConversion<string>();

            builder.Property(f => f.Description)
                   .HasMaxLength(500);

            builder.Property(f => f.WaivedReason)
                   .HasMaxLength(500);

            // ── Relationships ─────────────────────────────────────────────────
            builder.HasOne(f => f.Member)
                   .WithMany()
                   .HasForeignKey(f => f.MemberId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.BookBorrow)
                   .WithMany()
                   .HasForeignKey(f => f.BookBorrowId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(f => f.School)
                   .WithMany()
                   .HasForeignKey(f => f.TenantId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}