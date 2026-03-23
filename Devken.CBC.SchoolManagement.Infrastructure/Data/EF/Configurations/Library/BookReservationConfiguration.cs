using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookReservationConfiguration : IEntityTypeConfiguration<BookReservation>
    {
        private readonly TenantContext _tenantContext;

        public BookReservationConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookReservation> builder)
        {
            builder.ToTable("BookReservations");

            builder.HasKey(r => r.Id);

            // ── Global query filter (tenant isolation) ────────────────────────
            builder.HasQueryFilter(r =>
                _tenantContext.TenantId == null ||
                r.TenantId == _tenantContext.TenantId);

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(r => r.BookId);
            builder.HasIndex(r => r.MemberId);
            builder.HasIndex(r => r.TenantId);

            // ── Relationships ─────────────────────────────────────────────────
            builder.HasOne(r => r.Book)
                   .WithMany()
                   .HasForeignKey(r => r.BookId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Member)
                   .WithMany()
                   .HasForeignKey(r => r.MemberId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}