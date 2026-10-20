namespace DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Roles of the restaurant team. The Administrator has full access; the
///     Employee cannot open Roles &amp; permissions, Recommendations or the
///     subscription.
/// </summary>
public enum UserRole
{
    Admin,
    Employee
}
