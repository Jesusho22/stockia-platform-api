using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Services;

public interface IRecommendationCommandService
{
    Task<IReadOnlyList<Recommendation>> Handle(GenerateRecommendationsCommand command, CancellationToken cancellationToken);
    Task<Result<Recommendation>> Handle(ApplyRecommendationCommand command, CancellationToken cancellationToken);
}
