namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;

/// <summary>
///     MachineLearning: ONNX model trained in ml/. Baseline: average of the
///     same weekday of the last 4 weeks, used when the model is not loaded or
///     the restaurant has less than 14 days of sales.
/// </summary>
public enum ForecastMethod
{
    MachineLearning,
    Baseline
}
