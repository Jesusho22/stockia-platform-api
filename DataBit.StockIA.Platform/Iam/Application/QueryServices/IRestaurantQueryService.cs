using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.Iam.Application.QueryServices;

public interface IRestaurantQueryService
{
    Task<Restaurant?> Handle(GetRestaurantByIdQuery query, CancellationToken cancellationToken);
}
