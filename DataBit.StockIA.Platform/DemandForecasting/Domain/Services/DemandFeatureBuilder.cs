namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Services;

/// <summary>
///     Builds the model features of one dish for one day. Same definitions and
///     order as ml/stockia_ml/features.py (FEATURE_ORDER); the API checks the
///     order against model_metadata.json when it loads the model, and the tests
///     compare the result with ml/models/parity_fixture.json.
/// </summary>
public static class DemandFeatureBuilder
{
    public static readonly IReadOnlyList<string> FeatureOrder =
    [
        "lag_7", "lag_14", "mean_7_13", "mean_7_34", "same_weekday_mean_4w", "day_of_week", "is_weekend", "day_of_month",
        "month", "is_holiday", "is_holiday_eve", "is_payday_window", "temp_max_c", "precipitation_mm"
    ];

    /// <summary>Oldest day a feature reads: t-34.</summary>
    public const int HistoryDaysRequired = 34;

    /// <summary>Newest day a feature reads: t-7, so the next 7 days are forecast directly.</summary>
    public const int MinimumLagDays = 7;

    /// <param name="history">Units sold per day; missing days count as 0.</param>
    /// <param name="target">Day to forecast.</param>
    /// <param name="tempMaxC">Maximum temperature of that day (°C).</param>
    /// <param name="precipitationMm">Precipitation of that day (mm).</param>
    public static float[] Build(IReadOnlyDictionary<DateOnly, double> history, DateOnly target, double tempMaxC, double precipitationMm)
    {
        double Units(int daysBack) => history.TryGetValue(target.AddDays(-daysBack), out var units) ? units : 0d;
        double Mean(IEnumerable<int> daysBack) => daysBack.Select(Units).Average();

        var dayOfWeek = PeruCalendar.MondayBasedDayOfWeek(target);
        return
        [
            (float)Units(7),
            (float)Units(14),
            (float)Mean(Enumerable.Range(7, 7)),
            (float)Mean(Enumerable.Range(7, 28)),
            (float)Mean([7, 14, 21, 28]),
            dayOfWeek,
            dayOfWeek >= 5 ? 1f : 0f,
            target.Day,
            target.Month,
            PeruCalendar.IsHoliday(target) ? 1f : 0f,
            PeruCalendar.IsHolidayEve(target) ? 1f : 0f,
            PeruCalendar.IsPaydayWindow(target) ? 1f : 0f,
            (float)tempMaxC,
            (float)precipitationMm
        ];
    }

    /// <summary>Baseline: same weekday of the last 4 weeks, or the 4-week mean when that is zero.</summary>
    public static double Baseline(float[] features)
    {
        var sameWeekday = features[4];
        return sameWeekday > 0 ? sameWeekday : features[3];
    }
}
