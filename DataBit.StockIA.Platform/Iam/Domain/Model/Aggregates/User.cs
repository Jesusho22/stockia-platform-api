using System.Text.Json.Serialization;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     A member of the restaurant team. Only BCrypt hashes are stored (password
///     and password-reset code) and they are never serialized.
/// </summary>
public class User(string fullName, string email, string passwordHash, int restaurantId, UserRole role) : IAuditableEntity
{
    public User() : this(string.Empty, string.Empty, string.Empty, 0, UserRole.Employee)
    {
    }

    public int Id { get; private set; }
    public string FullName { get; private set; } = fullName.Trim();
    public string Email { get; private set; } = NormalizeEmail(email);
    [JsonIgnore] public string PasswordHash { get; private set; } = passwordHash;
    public int RestaurantId { get; private set; } = restaurantId;
    public UserRole Role { get; private set; } = role;

    /// <summary>Wrong passwords in a row (Account Lock Service).</summary>
    public int FailedSignInAttempts { get; private set; }

    /// <summary>While this moment has not passed, sign-in is refused even with the right password.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    [JsonIgnore] public string? PasswordResetCodeHash { get; private set; }
    public DateTimeOffset? PasswordResetExpiresAt { get; private set; }
    public int PasswordResetAttempts { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsAdmin => Role == UserRole.Admin;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public bool IsLockedAt(DateTimeOffset now) => LockedUntil is { } until && until > now;

    public User UpdateProfile(string fullName, string email)
    {
        FullName = fullName.Trim();
        Email = NormalizeEmail(email);
        return this;
    }

    public User UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        return this;
    }

    public User ChangeRole(UserRole role)
    {
        Role = role;
        return this;
    }

    /// <summary>Counts a wrong password; reaching the limit locks the account until <paramref name="lockUntil" />.</summary>
    public User RegisterFailedSignIn(int maxAttempts, DateTimeOffset lockUntil)
    {
        FailedSignInAttempts++;
        if (FailedSignInAttempts >= maxAttempts) LockedUntil = lockUntil;
        return this;
    }

    /// <summary>A successful sign-in, an Administrator or a password reset clears the lock.</summary>
    public User Unlock()
    {
        FailedSignInAttempts = 0;
        LockedUntil = null;
        return this;
    }

    public User StartPasswordReset(string codeHash, DateTimeOffset expiresAt)
    {
        PasswordResetCodeHash = codeHash;
        PasswordResetExpiresAt = expiresAt;
        PasswordResetAttempts = 0;
        return this;
    }

    public User RegisterFailedPasswordReset()
    {
        PasswordResetAttempts++;
        return this;
    }

    public User ClearPasswordReset()
    {
        PasswordResetCodeHash = null;
        PasswordResetExpiresAt = null;
        PasswordResetAttempts = 0;
        return this;
    }
}
