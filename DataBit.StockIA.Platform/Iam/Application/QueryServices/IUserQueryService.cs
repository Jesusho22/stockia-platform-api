using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.Iam.Application.QueryServices;

public interface IUserQueryService
{
    Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<User>> Handle(GetUsersByRestaurantIdQuery query, CancellationToken cancellationToken);
}
