using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.Iam.Application.CommandServices;

public interface IRestaurantCommandService
{
    Task<Result<Restaurant>> Handle(UpdateRestaurantCommand command, CancellationToken cancellationToken);
}
