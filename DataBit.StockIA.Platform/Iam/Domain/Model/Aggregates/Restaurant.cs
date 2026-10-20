using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The restaurant (tenant). Every other bounded context scopes its data by
///     RestaurantId. StockIA serves one restaurant per account.
/// </summary>
public class Restaurant(string name) : IAuditableEntity
{
    public Restaurant() : this(string.Empty)
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = name;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    /// <summary>Coordinates used for the weather of the demand forecast (Lima by default).</summary>
    public double Latitude { get; private set; } = -12.0464;
    public double Longitude { get; private set; } = -77.0428;

    /// <summary>
    ///     Owner account. Not a database foreign key: User.RestaurantId already
    ///     points here and a circular FK could not be inserted (same approach as
    ///     QULLQA's Business.UserId).
    /// </summary>
    public int OwnerUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Restaurant UpdateProfile(string name, string address, string phone)
    {
        Name = name.Trim();
        Address = address.Trim();
        Phone = phone.Trim();
        return this;
    }

    public Restaurant UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        return this;
    }

    public Restaurant LinkOwner(int userId)
    {
        OwnerUserId = userId;
        return this;
    }
}
