using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record InviteMemberCommand(int RestaurantId, string FullName, string Email, UserRole Role, string TemporaryPassword);
