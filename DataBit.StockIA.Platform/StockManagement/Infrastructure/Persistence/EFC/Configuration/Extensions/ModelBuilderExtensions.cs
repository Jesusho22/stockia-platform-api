using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyStockManagementConfiguration(this ModelBuilder builder)
    {
        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();
            entity.HasIndex(item => item.RestaurantId);
            entity.Property(item => item.Name).IsRequired().HasMaxLength(120);
            entity.Property(item => item.Unit).IsRequired().HasMaxLength(20);
            entity.Property(item => item.Quantity).HasQuantityConversion();
            entity.Property(item => item.MinThreshold).HasQuantityConversion();
            entity.Property(item => item.StorageType).HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.ExpirationDate).HasDateOnlyConversion();
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Ignore(item => item.ProductKey);
            entity.Ignore(item => item.StockValue);
        });

        builder.Entity<StockConsumption>(entity =>
        {
            entity.HasKey(consumption => consumption.Id);
            entity.Property(consumption => consumption.Id).ValueGeneratedOnAdd();
            entity.HasIndex(consumption => new { consumption.RestaurantId, consumption.ConsumedOn });
            entity.Property(consumption => consumption.ItemName).IsRequired().HasMaxLength(120);
            entity.Property(consumption => consumption.Unit).IsRequired().HasMaxLength(20);
            entity.Property(consumption => consumption.Quantity).HasQuantityConversion();
            entity.Property(consumption => consumption.UnitCost).HasPrecision(18, 4);
            entity.Property(consumption => consumption.LotExpirationDate).HasDateOnlyConversion();
            entity.Property(consumption => consumption.ConsumedOn).HasDateOnlyConversion();
            entity.Ignore(consumption => consumption.Value);
            entity.Ignore(consumption => consumption.DaysBeforeExpiration);
        });
    }
}
