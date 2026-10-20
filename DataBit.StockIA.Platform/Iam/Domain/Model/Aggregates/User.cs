using System.Text.Json.Serialization;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     A member of the restaurant team. Only the BCrypt hash of the password is
///     stored, and it is never serialized.
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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsAdmin => Role == UserRole.Admin;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

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
}
