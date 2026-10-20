using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(restaurant => restaurant.Id);
            entity.Property(restaurant => restaurant.Id).ValueGeneratedOnAdd();
            entity.Property(restaurant => restaurant.Name).IsRequired().HasMaxLength(150);
            entity.Property(restaurant => restaurant.Address).HasMaxLength(255);
            entity.Property(restaurant => restaurant.Phone).HasMaxLength(30);
            // OwnerUserId is not a database FK (see Restaurant.cs).
        });

        builder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedOnAdd();
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(120);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(user => user.IsAdmin);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(user => user.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
