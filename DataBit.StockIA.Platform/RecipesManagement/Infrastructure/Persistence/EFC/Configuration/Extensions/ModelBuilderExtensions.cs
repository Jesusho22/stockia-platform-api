using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace DataBit.StockIA.Platform.RecipesManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyRecipesManagementConfiguration(this ModelBuilder builder)
    {
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
