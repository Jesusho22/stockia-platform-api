namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record UpdateUserProfileCommand(int UserId, string FullName, string Email);
