namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record UpdateRestaurantCommand(int RestaurantId, string Name, string Address, string Phone);
