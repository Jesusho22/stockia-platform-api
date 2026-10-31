namespace DataBit.StockIA.Platform.MlRecommendations.Application.Errors;

/// <summary>Errors of the ML &amp; Recommendations bounded context (their names are the problem titles).</summary>
public enum MlRecommendationsError
{
    NoDishes,
    InvalidHorizon,
    RecommendationNotFound
}
