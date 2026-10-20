namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword);
