namespace DataBit.StockIA.Platform.RecipesManagement.Application.Errors;

/// <summary>Errors of the Recipes Management bounded context (their names are the problem titles).</summary>
public enum RecipesManagementError
{
    RecipeNotFound,
    IngredientNotFound,
    InvalidData,
    CannotSell,
    SaleNotFound,
    AlreadyVoided,
    DatabaseError
}
