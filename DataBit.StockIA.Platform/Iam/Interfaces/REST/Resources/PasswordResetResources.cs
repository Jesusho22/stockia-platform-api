using System.ComponentModel.DataAnnotations;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

public record RequestPasswordResetResource([Required, EmailAddress, MaxLength(255)] string Email);

/// <summary>Email and code come from the recovery link; same password rules as sign-up.</summary>
public record ResetPasswordResource(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, RegularExpression("^\\d{6}$", ErrorMessage = "The code has 6 digits.")] string Code,
    [Required, MinLength(8), MaxLength(72), RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$", ErrorMessage = "The password needs letters and numbers.")] string NewPassword);

/// <summary>Copy of the simulated email (only in demo mode).</summary>
public record SimulatedEmailResource(string To, string Subject, string ResetLink, string Code, DateTimeOffset ExpiresAt);

/// <summary>Always the same answer, registered email or not.</summary>
public record PasswordResetRequestResource(string Message, string SentTo, SimulatedEmailResource? SimulatedEmail);
