using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.QueryServices;

public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    public Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) =>
        userRepository.FindByIdAsync(query.UserId, cancellationToken);

    public Task<IEnumerable<User>> Handle(GetUsersByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        userRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
