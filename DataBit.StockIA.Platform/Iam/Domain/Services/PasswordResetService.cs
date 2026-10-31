using System.Security.Cryptography;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.Iam.Domain.Services;

/// <summary>
///     Password Reset Service (C4): a 6-digit code valid for
///     <see cref="CodeLifetime" />, at most <see cref="MaxAttempts" /> tries.
///     The code travels in the recovery link sent by email; only its hash is
///     stored.
/// </summary>
public static class PasswordResetService
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(30);
    public const int MaxAttempts = 5;

    public static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>Whether the user has a reset in progress that can still be used.</summary>
    public static bool CanUseCode(User user, DateTimeOffset now) =>
        user.PasswordResetCodeHash != null && user.PasswordResetExpiresAt is { } expiresAt && expiresAt > now &&
        user.PasswordResetAttempts < MaxAttempts;

    /// <summary>"maria@gmail.com" → "m***a@gmail.com" (to confirm where the link went without exposing the address).</summary>
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        var name = email[..at];
        return $"{name[0]}{new string('*', Math.Max(1, name.Length - 2))}{name[^1]}{email[at..]}";
    }
}
