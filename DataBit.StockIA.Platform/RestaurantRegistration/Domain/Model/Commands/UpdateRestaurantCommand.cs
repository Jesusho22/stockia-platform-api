namespace DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Commands;

public record UpdateRestaurantCommand(int RestaurantId, string Name, string Address, string Phone);
