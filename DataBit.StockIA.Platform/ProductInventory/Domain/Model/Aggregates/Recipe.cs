using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;

/// <summary>
///     A dish of the menu and the supplies it consumes each time it is sold.
/// </summary>
public class Recipe : IAuditableEntity
{
    private readonly List<RecipeIngredient> _ingredients = [];

    private Recipe()
    {
    }

    public Recipe(int restaurantId, string dishName, IEnumerable<RecipeIngredient> ingredients)
    {
        RestaurantId = restaurantId;
        Update(dishName, ingredients);
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public string DishName { get; private set; } = string.Empty;
    public bool Active { get; private set; } = true;
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Recipe Update(string dishName, IEnumerable<RecipeIngredient> ingredients)
    {
        var lines = ingredients.ToList();
        if (string.IsNullOrWhiteSpace(dishName) || lines.Count == 0)
            throw new ArgumentException("A recipe needs a dish name and at least one ingredient.");
        DishName = dishName.Trim();
        _ingredients.Clear();
        _ingredients.AddRange(lines);
        return this;
    }
}
