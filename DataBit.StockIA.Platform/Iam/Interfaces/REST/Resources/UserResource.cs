namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

/// <summary>A team member. LockedUntil is set while the account is locked by wrong passwords.</summary>
public record UserResource(
    int Id,
    string FullName,
    string Email,
    string Role,
    int RestaurantId,
    string RestaurantName,
    string RestaurantAddress,
    string RestaurantPhone,
    bool Locked,
    DateTimeOffset? LockedUntil);
