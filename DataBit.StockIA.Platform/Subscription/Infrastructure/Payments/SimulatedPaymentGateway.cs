using DataBit.StockIA.Platform.Subscription.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Subscription.Infrastructure.Payments;

/// <summary>Simulated checkout: always approves and logs the charge. No real money moves.</summary>
public class SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger) : IPaymentGateway
{
    public Task<bool> ChargeAsync(int restaurantId, Plan plan, PaymentMethod paymentMethod, CancellationToken cancellationToken)
    {
        logger.LogInformation("Simulated {Provider} charge of S/ {Amount} for plan {Plan} (restaurant {RestaurantId})",
            paymentMethod, plan.MonthlyPrice, plan.Name, restaurantId);
        return Task.FromResult(true);
    }
}
