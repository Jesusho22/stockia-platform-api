namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;

/// <summary>
///     Weather of one day. Source: "open-meteo" or "climatology" (fallback).
///     WeatherCode is the WMO code given by Open-Meteo, when available.
/// </summary>
public record DailyWeather(DateOnly Date, double TempMaxC, double PrecipitationMm, int? WeatherCode, string Source);
