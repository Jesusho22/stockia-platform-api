using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.SalesOrder.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySalesOrderConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Sale>(entity =>
        {
            entity.HasKey(sale => sale.Id);
            entity.Property(sale => sale.Id).ValueGeneratedOnAdd();
            entity.HasIndex(sale => new { sale.RestaurantId, sale.SaleDate });
            entity.Property(sale => sale.Channel).HasConversion<string>().HasMaxLength(20);
            entity.Property(sale => sale.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(sale => sale.Total);
            entity.Ignore(sale => sale.IsConfirmed);
            entity.OwnsMany(sale => sale.LineItems, line =>
            {
                line.ToTable("sale_line_items");
                line.WithOwner().HasForeignKey("SaleId");
                line.Property<int>("Id").ValueGeneratedOnAdd();
                line.HasKey("Id");
                line.Property(item => item.DishName).IsRequired().HasMaxLength(120);
                line.Property(item => item.UnitPrice).HasPrecision(12, 2);
            });
            entity.Navigation(sale => sale.LineItems).HasField("_lineItems").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
