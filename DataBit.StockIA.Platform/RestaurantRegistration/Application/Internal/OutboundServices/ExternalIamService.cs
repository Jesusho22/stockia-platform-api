using DataBit.StockIA.Platform.Iam.Interfaces.ACL;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.OutboundServices;

/// <summary>Anti-corruption layer towards Identity &amp; Access Management.</summary>
public class ExternalIamService(IIamContextFacade iamContextFacade)
{
    public Task<bool> EmailExists(string email, CancellationToken cancellationToken) =>
        iamContextFacade.EmailExists(email, cancellationToken);

    /// <summary>Creates the owner as the first Administrator; null when the email is already registered.</summary>
    public async Task<RestaurantOwnerAccount?> CreateOwnerAccount(string fullName, string email, string password, int restaurantId,
        CancellationToken cancellationToken)
    {
        var account = await iamContextFacade.CreateOwnerAccount(fullName, email, password, restaurantId, cancellationToken);
        return account == null
            ? null
            : new RestaurantOwnerAccount(account.UserId, account.FullName, account.Email, account.Role, account.Token);
    }
}
