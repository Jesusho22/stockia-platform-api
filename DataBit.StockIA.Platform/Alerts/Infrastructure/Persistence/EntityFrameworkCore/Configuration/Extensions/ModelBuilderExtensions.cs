using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace DataBit.StockIA.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyAlertsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Alert>(entity =>
        {
            entity.HasKey(alert => alert.Id);
            entity.Property(alert => alert.Id).ValueGeneratedOnAdd();
            entity.HasIndex(alert => new { alert.RestaurantId, alert.CreatedAt });
            entity.Property(alert => alert.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(alert => alert.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(alert => alert.Channel).HasConversion<string>().HasMaxLength(20);
            entity.Property(alert => alert.Message).IsRequired().HasMaxLength(500);
            entity.Property(alert => alert.DeliveredChannels).HasStringListConversion();
            entity.Ignore(alert => alert.RequiredChannels);
            entity.Ignore(alert => alert.PendingChannel);
            entity.Ignore(alert => alert.Delivered);
        });

        builder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(recommendation => recommendation.Id);
            entity.Property(recommendation => recommendation.Id).ValueGeneratedOnAdd();
            entity.HasIndex(recommendation => recommendation.RestaurantId);
            entity.Property(recommendation => recommendation.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(recommendation => recommendation.Message).IsRequired().HasMaxLength(500);
            entity.Property(recommendation => recommendation.ExpectedImpact).HasMaxLength(200);
        });
    }
}
