using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Errors;
using DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Transform;

public static class AlertResourceFromEntityAssembler
{
    public static AlertResource ToResourceFromEntity(Alert alert) => new(
        alert.Id, alert.Type.ToCode(), alert.Severity.ToCode(), alert.Message, alert.CreatedAt, alert.Acknowledged, alert.Channel.ToCode(),
        alert.DeliveredChannels, alert.RequiredChannels.Select(channel => channel.ToCode()).ToList(), alert.Delivered, alert.InventoryItemId);

    public static RecommendationResource ToResourceFromEntity(Recommendation recommendation) => new(
        recommendation.Id, recommendation.Type.ToCode(), recommendation.Message, recommendation.ExpectedImpact, recommendation.Applied,
        recommendation.CreatedAt, recommendation.DemandForecastId);
}

public static class AlertsActionResultAssembler
{
    private static int StatusCodeOf(Enum? error) => error switch
    {
        AlertsError.AlertNotFound => StatusCodes.Status404NotFound,
        AlertsError.RecommendationNotFound => StatusCodes.Status404NotFound,
        AlertsError.NotDelivered => StatusCodes.Status409Conflict,
        AlertsError.AlreadyDelivered => StatusCodes.Status409Conflict,
        AlertsError.DeliveryFailed => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
