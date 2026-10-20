using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.Alerts.Application.CommandServices;

public interface IRecommendationCommandService
{
    Task<IReadOnlyList<Recommendation>> Handle(GenerateRecommendationsCommand command, CancellationToken cancellationToken);
    Task<Result<Recommendation>> Handle(ApplyRecommendationCommand command, CancellationToken cancellationToken);
}
