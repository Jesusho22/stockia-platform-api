namespace DataBit.StockIA.Platform.Shared.Application;

/// <summary>
///     Current date and time in the restaurant's time zone (America/Lima).
///     Expiration dates, "today's" sales and forecasts depend on the local
///     calendar day, not on UTC.
/// </summary>
public interface IRestaurantClock
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
}
