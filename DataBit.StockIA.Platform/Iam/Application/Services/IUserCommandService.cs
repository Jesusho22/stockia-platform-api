using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.Iam.Application.Services;

public interface IUserCommandService
{
    /// <summary>Login Controller + Account Lock Service.</summary>
    Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken);

    Task<Result<(User user, string token)>> Handle(CreateOwnerAccountCommand command, CancellationToken cancellationToken);
    Task<Result<PasswordResetRequest>> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(UnlockUserCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(InviteMemberCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken);
}
