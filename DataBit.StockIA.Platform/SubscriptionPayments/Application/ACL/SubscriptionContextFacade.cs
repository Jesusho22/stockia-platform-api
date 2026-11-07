using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.ACL;

public class SubscriptionContextFacade(ISubscriptionCommandService subscriptionCommandService) : ISubscriptionContextFacade
{
    public async Task<int> CreateDefaultSubscription(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await subscriptionCommandService.Handle(new CreateDefaultSubscriptionCommand(restaurantId), cancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Message);
        return result.Value!.Id;
    }
}