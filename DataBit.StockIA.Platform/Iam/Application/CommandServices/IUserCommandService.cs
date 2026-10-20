using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.Iam.Application.CommandServices;

public interface IUserCommandService
{
    Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken);

    /// <summary>Creates the restaurant, its owner (Administrator) and the free subscription, and signs in.</summary>
    Task<Result<(User user, string token)>> Handle(SignUpCommand command, CancellationToken cancellationToken);

    Task<Result<User>> Handle(InviteMemberCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken);
}
