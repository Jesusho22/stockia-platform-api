using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyNotificationsMessagingConfiguration(this ModelBuilder builder)
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
    }
}