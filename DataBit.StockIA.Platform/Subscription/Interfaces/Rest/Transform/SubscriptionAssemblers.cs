using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Errors;
using DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Transform;

public static class PlanResourceFromEntityAssembler
{
    public static PlanResource ToResourceFromEntity(Plan plan) =>
        new(plan.Id, plan.Name, plan.MonthlyPrice, plan.Features, plan.Highlighted);
}

public static class SubscriptionResourceFromEntityAssembler
{
    public static SubscriptionResource ToResourceFromEntity(RestaurantSubscription subscription) => new(
        subscription.Id, subscription.PlanId, subscription.Status.ToCode(), subscription.RenewalDate.ToString("yyyy-MM-dd"),
        subscription.PaymentMethod.ToCode());
}

public static class SubscriptionActionResultAssembler
{
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, error => error switch
        {
            SubscriptionError.PlanNotFound => StatusCodes.Status404NotFound,
            SubscriptionError.SubscriptionNotFound => StatusCodes.Status404NotFound,
            SubscriptionError.InvalidPaymentMethod => StatusCodes.Status400BadRequest,
            SubscriptionError.PaymentRejected => StatusCodes.Status402PaymentRequired,
            _ => StatusCodes.Status500InternalServerError
        }, onSuccess);
}
