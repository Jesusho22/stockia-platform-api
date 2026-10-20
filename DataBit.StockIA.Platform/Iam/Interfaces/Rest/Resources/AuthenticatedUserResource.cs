namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

public record AuthenticatedUserResource(
    int Id,
    string FullName,
    string Email,
    string Role,
    int RestaurantId,
    string RestaurantName,
    string RestaurantAddress,
    string RestaurantPhone,
    string Token);
