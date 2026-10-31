using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyRestaurantRegistrationConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(restaurant => restaurant.Id);
            entity.Property(restaurant => restaurant.Id).ValueGeneratedOnAdd();
            entity.Property(restaurant => restaurant.Name).IsRequired().HasMaxLength(150);
            entity.Property(restaurant => restaurant.Address).HasMaxLength(255);
            entity.Property(restaurant => restaurant.Phone).HasMaxLength(30);
            // OwnerUserId is not a database FK: the owner lives in IAM (see Restaurant.cs).
        });
    }
}
