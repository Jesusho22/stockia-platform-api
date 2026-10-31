using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.Iam.Domain.Services;

/// <summary>
///     Account Lock Service (C4): after <see cref="MaxFailedAttempts" /> wrong
///     passwords in a row the account is locked for
///     <see cref="LockDuration" />. A successful sign-in resets the count.
/// </summary>
public static class AccountLockService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    /// <summary>Registers a wrong password and returns whether the account is locked now.</summary>
    public static bool RegisterFailedAttempt(User user, DateTimeOffset now)
    {
        user.RegisterFailedSignIn(MaxFailedAttempts, now.Add(LockDuration));
        return user.IsLockedAt(now);
    }

    public static int RemainingAttempts(User user) => Math.Max(0, MaxFailedAttempts - user.FailedSignInAttempts);

    /// <summary>Whole minutes left (at least 1) until the account opens again.</summary>
    public static int MinutesLeft(User user, DateTimeOffset now) =>
        user.LockedUntil is { } until ? Math.Max(1, (int)Math.Ceiling((until - now).TotalMinutes)) : 0;
}
