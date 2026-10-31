using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.MlRecommendations.Application.Errors;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST.Transform;

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

public static class RecommendationResourceFromEntityAssembler
{
    public static RecommendationResource ToResourceFromEntity(Recommendation recommendation) => new(
        recommendation.Id, recommendation.Type.ToCode(), recommendation.Message, recommendation.ExpectedImpact, recommendation.Applied,
        recommendation.CreatedAt, recommendation.DemandForecastId);
}

/// <summary>Maps MlRecommendationsError to HTTP status codes.</summary>
public static class MlRecommendationsActionResultAssembler
{
    public static int StatusCodeOf(Enum? error) => error switch
    {
        MlRecommendationsError.NoDishes => StatusCodes.Status422UnprocessableEntity,
        MlRecommendationsError.InvalidHorizon => StatusCodes.Status400BadRequest,
        MlRecommendationsError.RecommendationNotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
