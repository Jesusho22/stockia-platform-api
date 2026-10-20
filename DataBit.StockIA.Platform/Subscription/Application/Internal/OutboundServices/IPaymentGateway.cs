using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Subscription.Application.Internal.OutboundServices;

/// <summary>
///     Charges a plan with Stripe or PayPal. The current implementation
///     simulates the payment (SimulatedPaymentGateway); a real one only needs
///     to implement this interface.
/// </summary>
public interface IPaymentGateway
{
    Task<bool> ChargeAsync(int restaurantId, Plan plan, PaymentMethod paymentMethod, CancellationToken cancellationToken);
}
