namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Services;

/// <summary>
///     Peruvian calendar used as model features. Same rules as
///     ml/stockia_ml/data/calendar_pe.py (checked against the "holidays"
///     package for 2020-2030 in the Python tests).
/// </summary>
public static class PeruCalendar
{
    /// <summary>(month, day, first year it applies).</summary>
    private static readonly (int Month, int Day, int Since)[] FixedHolidays =
    [
        (1, 1, 1900), // Año Nuevo
        (5, 1, 1900), // Día del Trabajo
        (6, 7, 2024), // Batalla de Arica y Día de la Bandera (Ley 31788)
        (6, 29, 1900), // San Pedro y San Pablo
        (7, 23, 2023), // Día de la Fuerza Aérea del Perú (Ley 31822)
        (7, 28, 1900), // Fiestas Patrias
        (7, 29, 1900), // Fiestas Patrias
        (8, 6, 2022), // Batalla de Junín
        (8, 30, 1900), // Santa Rosa de Lima
        (10, 8, 1900), // Combate de Angamos
        (11, 1, 1900), // Todos los Santos
        (12, 8, 1900), // Inmaculada Concepción
        (12, 9, 2022), // Batalla de Ayacucho
        (12, 25, 1900) // Navidad
    ];

    /// <summary>Anonymous Gregorian algorithm (Meeus/Jones/Butcher).</summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    public static bool IsHoliday(DateOnly day)
    {
        foreach (var (month, dayOfMonth, since) in FixedHolidays)
            if (day.Year >= since && day.Month == month && day.Day == dayOfMonth) return true;
        var easter = EasterSunday(day.Year);
        return day == easter.AddDays(-3) || day == easter.AddDays(-2); // Jueves y Viernes Santo
    }

    public static bool IsHolidayEve(DateOnly day) => IsHoliday(day.AddDays(1));

    /// <summary>Quincena: payday on the 15th and the last day of the month, and the day after.</summary>
    public static bool IsPaydayWindow(DateOnly day)
    {
        var lastDay = DateTime.DaysInMonth(day.Year, day.Month);
        return day.Day is 1 or 15 or 16 || day.Day == lastDay;
    }

    /// <summary>Monday = 0 … Sunday = 6 (Python's date.weekday()).</summary>
    public static int MondayBasedDayOfWeek(DateOnly day) => ((int)day.DayOfWeek + 6) % 7;
}
