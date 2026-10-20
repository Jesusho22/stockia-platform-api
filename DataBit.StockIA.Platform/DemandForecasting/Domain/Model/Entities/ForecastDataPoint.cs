namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Entities;

/// <summary>Projected units of one dish on one day, with the weather the model used.</summary>
public class ForecastDataPoint
{
    private ForecastDataPoint()
    {
    }

    public ForecastDataPoint(DateOnly date, string dishName, int projectedUnits, double tempMaxC, double precipitationMm)
    {
        Date = date;
        DishName = dishName;
        ProjectedUnits = Math.Max(0, projectedUnits);
        TempMaxC = tempMaxC;
        PrecipitationMm = precipitationMm;
    }

    public DateOnly Date { get; private set; }
    public string DishName { get; private set; } = string.Empty;
    public int ProjectedUnits { get; private set; }
    public double TempMaxC { get; private set; }
    public double PrecipitationMm { get; private set; }
}
