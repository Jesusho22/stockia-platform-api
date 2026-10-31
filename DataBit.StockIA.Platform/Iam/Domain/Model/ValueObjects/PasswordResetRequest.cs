namespace DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Outcome of "Forgot your password?". The response is the same whether the
///     email exists or not; <see cref="SimulatedEmail" /> is only filled when
///     the email provider is simulated and the demo mode exposes it.
/// </summary>
public record PasswordResetRequest(string MaskedEmail, SimulatedEmail? SimulatedEmail);

/// <summary>Copy of the email the simulated provider "sent" (demo only).</summary>
public record SimulatedEmail(string To, string Subject, string ResetLink, string Code, DateTimeOffset ExpiresAt);
