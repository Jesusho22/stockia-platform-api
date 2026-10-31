using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.CommandServices;

public class SubscriptionCommandService(
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    IPaymentGateway paymentGateway,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SubscriptionPaymentsMessages> localizer) : ISubscriptionCommandService
{
    /// <summary>Every new restaurant starts with the free plan (called by IAM's sign-up).</summary>
    public async Task<Result<RestaurantSubscription>> Handle(CreateDefaultSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var existing = await subscriptionRepository.FindByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        if (existing != null) return Result<RestaurantSubscription>.Success(existing);

        var plan = await planRepository.FindFreePlanAsync(cancellationToken);
        if (plan == null)
            return Result<RestaurantSubscription>.Failure(SubscriptionPaymentsError.PlanNotFound, localizer[nameof(SubscriptionPaymentsError.PlanNotFound)]);

        var subscription = new RestaurantSubscription(command.RestaurantId, plan.Id).Activate(plan.Id, PaymentMethod.None, clock.Today);
        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<RestaurantSubscription>.Success(subscription);
    }

    /// <summary>Simulated checkout with Stripe or PayPal, then the plan is activated.</summary>
    public async Task<Result<RestaurantSubscription>> Handle(ChangePlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await planRepository.FindByIdAsync(command.PlanId, cancellationToken);
        if (plan == null)
            return Result<RestaurantSubscription>.Failure(SubscriptionPaymentsError.PlanNotFound, localizer[nameof(SubscriptionPaymentsError.PlanNotFound)]);
        if (!plan.IsFree && command.PaymentMethod == PaymentMethod.None)
            return Result<RestaurantSubscription>.Failure(SubscriptionPaymentsError.InvalidPaymentMethod,
                localizer[nameof(SubscriptionPaymentsError.InvalidPaymentMethod)]);
        if (!plan.IsFree && !await paymentGateway.ChargeAsync(command.RestaurantId, plan, command.PaymentMethod, cancellationToken))
            return Result<RestaurantSubscription>.Failure(SubscriptionPaymentsError.PaymentRejected, localizer[nameof(SubscriptionPaymentsError.PaymentRejected)]);

        var subscription = await subscriptionRepository.FindByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        if (subscription == null)
        {
            subscription = new RestaurantSubscription(command.RestaurantId, plan.Id);
            await subscriptionRepository.AddAsync(subscription, cancellationToken);
        }

        subscription.Activate(plan.Id, command.PaymentMethod, clock.Today);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<RestaurantSubscription>.Success(subscription);
    }
}
