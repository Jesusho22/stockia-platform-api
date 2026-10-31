namespace DataBit.StockIA.Platform.Iam.Interfaces.ACL;

/// <summary>A user account created through the ACL, already signed in.</summary>
public record IamAccountInfo(int UserId, string FullName, string Email, string Role, string Token);

/// <summary>What other bounded contexts may ask Identity &amp; Access Management.</summary>
public interface IIamContextFacade
{
    Task<bool> EmailExists(string email, CancellationToken cancellationToken);

    /// <summary>Creates the owner (first Administrator) of a new restaurant; null when the email is already registered.</summary>
    Task<IamAccountInfo?> CreateOwnerAccount(string fullName, string email, string password, int restaurantId,
        CancellationToken cancellationToken);
}
