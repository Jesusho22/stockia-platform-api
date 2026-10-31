namespace DataBit.StockIA.Platform.Iam.Domain.Model.Commands;

/// <summary>"Forgot your password?": sends a recovery link to the email if it is registered.</summary>
public record RequestPasswordResetCommand(string Email);

/// <summary>Sets a new password with the code of the recovery link; it also unlocks the account.</summary>
public record ResetPasswordCommand(string Email, string Code, string NewPassword);

/// <summary>An Administrator unlocks a member locked by wrong passwords.</summary>
public record UnlockUserCommand(int RestaurantId, int UserId);

/// <summary>Creates the owner of a new restaurant (called by Restaurant Registration through the ACL).</summary>
public record CreateOwnerAccountCommand(string FullName, string Email, string Password, int RestaurantId);
