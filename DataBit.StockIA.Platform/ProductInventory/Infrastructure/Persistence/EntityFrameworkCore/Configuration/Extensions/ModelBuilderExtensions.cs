using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace DataBit.StockIA.Platform.ProductInventory.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyProductInventoryConfiguration(this ModelBuilder builder)
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

        builder.Entity<Recipe>(entity =>
        {
            entity.HasKey(recipe => recipe.Id);
            entity.Property(recipe => recipe.Id).ValueGeneratedOnAdd();
            entity.HasIndex(recipe => recipe.RestaurantId);
            entity.Property(recipe => recipe.DishName).IsRequired().HasMaxLength(120);
            entity.OwnsMany(recipe => recipe.Ingredients, line =>
            {
                line.ToTable("recipe_ingredients");
                line.WithOwner().HasForeignKey("RecipeId");
                line.Property<int>("Id").ValueGeneratedOnAdd();
                line.HasKey("Id");
                line.Property(ingredient => ingredient.InventoryItemName).IsRequired().HasMaxLength(120);
                line.Property(ingredient => ingredient.Unit).IsRequired().HasMaxLength(20);
                line.Property(ingredient => ingredient.QuantityRequired).HasQuantityConversion();
            });
            entity.Navigation(recipe => recipe.Ingredients).HasField("_ingredients").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
