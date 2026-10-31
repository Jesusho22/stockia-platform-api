using DataBit.StockIA.Platform.Iam.Application.Services;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Iam.Application.ACL;

public class IamContextFacade(IUserRepository userRepository, IUserCommandService userCommandService) : IIamContextFacade
{
    public Task<bool> EmailExists(string email, CancellationToken cancellationToken) =>
        userRepository.ExistsByEmailAsync(User.NormalizeEmail(email), cancellationToken: cancellationToken);

    public async Task<IamAccountInfo?> CreateOwnerAccount(string fullName, string email, string password, int restaurantId,
        CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(new CreateOwnerAccountCommand(fullName, email, password, restaurantId),
            cancellationToken);
        if (result.IsFailure) return null;
        var (user, token) = result.Value;
        return new IamAccountInfo(user.Id, user.FullName, user.Email, user.Role.ToCode(), token);
    }
}
