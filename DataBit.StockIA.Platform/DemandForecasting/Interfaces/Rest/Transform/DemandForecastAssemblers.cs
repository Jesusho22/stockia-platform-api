using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Errors;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest.Transform;

public static class DemandForecastResourceFromEntityAssembler
{
    public static DemandForecastResource ToResourceFromEntity(DemandForecast forecast)
    {
        var days = forecast.DataPoints.GroupBy(point => point.Date).OrderBy(group => group.Key).Select(group =>
        {
            var dishes = group.OrderByDescending(point => point.ProjectedUnits)
                .Select(point => new DishUnitsResource(point.DishName, point.ProjectedUnits)).ToList();
            var first = group.First();
            return new ForecastDayResource(group.Key.ToString("yyyy-MM-dd"),
                group.Key.ToString("ddd", CultureInfo.InvariantCulture), dishes.FirstOrDefault()?.DishName ?? string.Empty,
                dishes.Sum(dish => dish.ProjectedUnits), first.TempMaxC, first.PrecipitationMm, dishes);
        }).ToList();
        return new DemandForecastResource(forecast.Id, forecast.GeneratedAt, forecast.ConfidenceScore, forecast.WeatherCondition,
            forecast.WeatherSource, forecast.Method.ToCode(), forecast.ModelVersion, days);
    }
}

public static class DemandForecastActionResultAssembler
{
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, error => error switch
        {
            DemandForecastingError.NoDishes => StatusCodes.Status422UnprocessableEntity,
            DemandForecastingError.InvalidHorizon => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        }, onSuccess);
}
