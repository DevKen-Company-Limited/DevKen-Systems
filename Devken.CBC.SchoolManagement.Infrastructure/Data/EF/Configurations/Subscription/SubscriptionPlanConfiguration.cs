// Infrastructure/Data/EF/Configurations/Subscription/SubscriptionPlanConfiguration.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Subscription
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlanEntity>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlanEntity> builder)
        {
            builder.ToTable("SubscriptionPlans");

            builder.HasKey(sp => sp.Id);

            // Indexes
            builder.HasIndex(sp => sp.PlanType)
                .IsUnique()
                .HasDatabaseName("IX_SubscriptionPlans_PlanType");

            builder.HasIndex(sp => new { sp.IsActive, sp.IsVisible, sp.DisplayOrder })
                .HasDatabaseName("IX_SubscriptionPlans_Active_Visible_Order");

            builder.HasIndex(sp => sp.DisplayOrder)
                .HasDatabaseName("IX_SubscriptionPlans_DisplayOrder");

            // Properties
            builder.Property(sp => sp.PlanType)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(sp => sp.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sp => sp.Description)
                .HasMaxLength(500);

            builder.Property(sp => sp.MonthlyPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(sp => sp.QuarterlyPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(sp => sp.YearlyPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(sp => sp.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            builder.Property(sp => sp.MaxStudents)
                .IsRequired()
                .HasDefaultValue(100);

            builder.Property(sp => sp.MaxTeachers)
                .IsRequired()
                .HasDefaultValue(10);

            builder.Property(sp => sp.MaxStorageGB)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(5);

            builder.Property(sp => sp.EnabledFeatures)
                .HasMaxLength(1000);

            builder.Property(sp => sp.FeatureList)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("[]");

            builder.Property(sp => sp.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(sp => sp.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(sp => sp.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(sp => sp.IsMostPopular)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(sp => sp.QuarterlyDiscountPercent)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(10);

            builder.Property(sp => sp.YearlyDiscountPercent)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(20);

            // Audit fields
            builder.Property(sp => sp.CreatedOn)
                .IsRequired();

            builder.Property(sp => sp.UpdatedOn);

            // Relationships
            builder.HasMany(sp => sp.Subscriptions)
                .WithOne(s => s.PlanTemplate)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}