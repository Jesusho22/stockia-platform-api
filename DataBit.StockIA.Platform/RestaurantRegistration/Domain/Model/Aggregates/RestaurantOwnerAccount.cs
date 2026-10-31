namespace DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;

/// <summary>The owner account IAM created for a new restaurant, with its session token.</summary>
public record RestaurantOwnerAccount(int UserId, string FullName, string Email, string Role, string Token);

/// <summary>Result of the onboarding: the restaurant and its signed-in owner.</summary>
public record RestaurantRegistrationResult(Restaurant Restaurant, RestaurantOwnerAccount Owner);
