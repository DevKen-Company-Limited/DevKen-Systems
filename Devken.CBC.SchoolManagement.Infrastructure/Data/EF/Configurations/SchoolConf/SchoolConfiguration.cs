using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.SchoolConf
{
    public class SchoolConfiguration : IEntityTypeConfiguration<School>
    {
        public void Configure(EntityTypeBuilder<School> builder)
        {
            builder.ToTable("Schools");

            // ─────────────────────────────────────────────
            // Primary Key
            // ─────────────────────────────────────────────
            builder.HasKey(x => x.Id);

            // ─────────────────────────────────────────────
            // Required Fields
            // ─────────────────────────────────────────────
            builder.Property(x => x.SlugName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(250);

            // ─────────────────────────────────────────────
            // Optional Fields with Length Limits
            // ─────────────────────────────────────────────
            builder.Property(x => x.RegistrationNumber)
                   .HasMaxLength(100);

            builder.Property(x => x.KnecCenterCode)
                   .HasMaxLength(50);

            builder.Property(x => x.KraPin)
                   .HasMaxLength(50);

            builder.Property(x => x.Address)
                   .HasMaxLength(500);

            builder.Property(x => x.County)
                   .HasMaxLength(100);

            builder.Property(x => x.SubCounty)
                   .HasMaxLength(100);

            builder.Property(x => x.PhoneNumber)
                   .HasMaxLength(50);

            builder.Property(x => x.Email)
                   .HasMaxLength(150);

            builder.Property(x => x.LogoUrl)
                   .HasMaxLength(500);

            // ─────────────────────────────────────────────
            // Enum Configuration (Stored as INT)
            // ─────────────────────────────────────────────
            builder.Property(x => x.SchoolType)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Category)
                   .HasConversion<int>()
                   .IsRequired();

            // ─────────────────────────────────────────────
            // Default Values
            // ─────────────────────────────────────────────
            builder.Property(x => x.IsActive)
                   .HasDefaultValue(true);

            // ─────────────────────────────────────────────
            // Indexes (Important)
            // ─────────────────────────────────────────────
            builder.HasIndex(x => x.SlugName)
                   .IsUnique();

            builder.HasIndex(x => x.Email)
                   .IsUnique();

            builder.HasIndex(x => x.RegistrationNumber)
                   .IsUnique()
                   .HasFilter("[RegistrationNumber] IS NOT NULL");

            // ─────────────────────────────────────────────
            // Relationships
            // ─────────────────────────────────────────────
            builder.HasMany(x => x.Users)
                   .WithOne()
                   .HasForeignKey("SchoolId")
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Roles)
                   .WithOne()
                   .HasForeignKey("SchoolId")
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.AcademicYears)
                   .WithOne()
                   .HasForeignKey("SchoolId")
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Teachers)
                   .WithOne()
                   .HasForeignKey("SchoolId")
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
