namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record RemoveMemberCommand(int RestaurantId, int UserId, int ActingUserId);
