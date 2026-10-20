using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record ChangeUserRoleCommand(int RestaurantId, int UserId, UserRole Role);
