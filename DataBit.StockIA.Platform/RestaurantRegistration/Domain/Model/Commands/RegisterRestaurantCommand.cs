namespace DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Commands;

/// <summary>Restaurant onboarding: the restaurant, its owner (first Administrator) and the free plan.</summary>
public record RegisterRestaurantCommand(string OwnerFullName, string RestaurantName, string Email, string Password);
