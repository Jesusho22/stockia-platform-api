using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

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
            SubscriptionPaymentsError.PlanNotFound => StatusCodes.Status404NotFound,
            SubscriptionPaymentsError.SubscriptionNotFound => StatusCodes.Status404NotFound,
            SubscriptionPaymentsError.InvalidPaymentMethod => StatusCodes.Status400BadRequest,
            SubscriptionPaymentsError.PaymentRejected => StatusCodes.Status402PaymentRequired,
            _ => StatusCodes.Status500InternalServerError
        }, onSuccess);
}