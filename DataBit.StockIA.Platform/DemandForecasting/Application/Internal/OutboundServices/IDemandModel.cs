namespace DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;

/// <summary>Information about the loaded model (GET /demand-forecasts/model).</summary>
public record DemandModelInfo(
    bool Ready, string ModelVersion, string TrainedAt, string Algorithm, IReadOnlyList<string> DataSources,
    double ConfidenceBase, object? Metrics, string? Problem);

/// <summary>
///     The demand model: one row of features per dish and day (see
///     DemandFeatureBuilder) → expected units. Implemented with ONNX Runtime.
/// </summary>
public interface IDemandModel
{
    bool IsReady { get; }
    DemandModelInfo Info { get; }
    float[] Predict(IReadOnlyList<float[]> rows);
}
