using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace DataBit.StockIA.Platform.Subscription.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySubscriptionConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Plan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).ValueGeneratedOnAdd();
            entity.Property(plan => plan.Name).IsRequired().HasMaxLength(60);
            entity.Property(plan => plan.MonthlyPrice).HasPrecision(10, 2);
            entity.Property(plan => plan.Features).HasStringListConversion();
            entity.Ignore(plan => plan.IsFree);
        });

        builder.Entity<RestaurantSubscription>(entity =>
        {
            entity.ToTable("Subscription");
            entity.HasKey(subscription => subscription.Id);
            entity.Property(subscription => subscription.Id).ValueGeneratedOnAdd();
            entity.HasIndex(subscription => subscription.RestaurantId).IsUnique();
            entity.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(subscription => subscription.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(subscription => subscription.RenewalDate).HasDateOnlyConversion();
            entity.HasOne<Plan>().WithMany().HasForeignKey(subscription => subscription.PlanId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
