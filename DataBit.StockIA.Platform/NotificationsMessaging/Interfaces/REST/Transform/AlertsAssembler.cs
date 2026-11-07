using DataBit.StockIA.Platform.NotificationsMessaging.Application.Errors;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST.Transform;

public static class AlertResourceFromEntityAssembler
{
    public static AlertResource ToResourceFromEntity(Alert alert) => new(
        alert.Id, alert.Type.ToCode(), alert.Severity.ToCode(), alert.Message, alert.CreatedAt, alert.Acknowledged, alert.Channel.ToCode(),
        alert.DeliveredChannels, alert.RequiredChannels.Select(channel => channel.ToCode()).ToList(), alert.Delivered, alert.InventoryItemId);
}

public static class NotificationsMessagingActionResultAssembler
{
    private static int StatusCodeOf(Enum? error) => error switch
    {
        NotificationsMessagingError.AlertNotFound => StatusCodes.Status404NotFound,
        NotificationsMessagingError.NotDelivered => StatusCodes.Status409Conflict,
        NotificationsMessagingError.AlreadyDelivered => StatusCodes.Status409Conflict,
        NotificationsMessagingError.DeliveryFailed => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}