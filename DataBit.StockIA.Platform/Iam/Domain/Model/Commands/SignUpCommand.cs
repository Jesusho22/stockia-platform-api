namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record SignUpCommand(string FullName, string RestaurantName, string Email, string Password);
