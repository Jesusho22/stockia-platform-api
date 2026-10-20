using DataBit.StockIA.Platform.Shared.Application;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Time;

/// <summary>
///     Clock in the restaurant time zone. Lima has no daylight saving time, so
///     a fixed UTC-5 offset is used when the tz database is not installed.
/// </summary>
public class RestaurantClock(TimeProvider timeProvider, IConfiguration configuration) : IRestaurantClock
{
    private readonly TimeZoneInfo _timeZone = ResolveTimeZone(configuration["Restaurant:TimeZone"] ?? "America/Lima");

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("UTC-05", TimeSpan.FromHours(-5), "UTC-05", "UTC-05");
        }
    }
}
