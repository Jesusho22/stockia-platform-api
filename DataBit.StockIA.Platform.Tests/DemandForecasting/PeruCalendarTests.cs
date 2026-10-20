using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;

namespace DataBit.StockIA.Platform.Tests.DemandForecasting;

public class PeruCalendarTests
{
    [Fact]
    public void Holidays_2026_match_the_official_calendar()
    {
        // Same list as holidays.Peru(2026) without Easter Sunday (see ml/tests).
        DateOnly[] expected =
        [
            new(2026, 1, 1), new(2026, 4, 2), new(2026, 4, 3), new(2026, 5, 1), new(2026, 6, 7), new(2026, 6, 29),
            new(2026, 7, 23), new(2026, 7, 28), new(2026, 7, 29), new(2026, 8, 6), new(2026, 8, 30), new(2026, 10, 8),
            new(2026, 11, 1), new(2026, 12, 8), new(2026, 12, 9), new(2026, 12, 25)
        ];
        var actual = Enumerable.Range(0, 365).Select(day => new DateOnly(2026, 1, 1).AddDays(day)).Where(PeruCalendar.IsHoliday);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    public void Easter_sunday(int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), PeruCalendar.EasterSunday(year));
    }

    [Fact]
    public void Payday_window_and_weekday()
    {
        Assert.True(PeruCalendar.IsPaydayWindow(new DateOnly(2026, 2, 28)));
        Assert.True(PeruCalendar.IsPaydayWindow(new DateOnly(2026, 10, 16)));
        Assert.False(PeruCalendar.IsPaydayWindow(new DateOnly(2026, 10, 20)));
        Assert.Equal(0, PeruCalendar.MondayBasedDayOfWeek(new DateOnly(2026, 10, 19)));
        Assert.Equal(6, PeruCalendar.MondayBasedDayOfWeek(new DateOnly(2026, 10, 25)));
    }
}
