using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;

namespace DataBit.StockIA.Platform.DemandForecasting.Infrastructure.MachineLearning;

/// <summary>
///     Loads ml/models/demand_model.onnx (copied next to the API binaries) and
///     its metadata once, as a singleton. When the file is missing or its
///     feature order differs from DemandFeatureBuilder.FeatureOrder the model
///     stays "not ready" and forecasts use the baseline.
/// </summary>
public sealed class OnnxDemandModel : IDemandModel, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly string _inputName = "features";
    private readonly string _outputName = "units";

    public OnnxDemandModel(IConfiguration configuration, ILogger<OnnxDemandModel> logger)
    {
        var folder = configuration["DemandModel:Folder"] ?? "DemandForecasting/Infrastructure/MachineLearning/Models";
        var root = Path.IsPathRooted(folder) ? folder : Path.Combine(AppContext.BaseDirectory, folder);
        var modelPath = Path.Combine(root, "demand_model.onnx");
        var metadataPath = Path.Combine(root, "model_metadata.json");
        string? problem = null;
        JsonElement metadata = default;

        try
        {
            if (!File.Exists(modelPath) || !File.Exists(metadataPath))
                throw new FileNotFoundException($"Model files not found in {root}");

            metadata = JsonDocument.Parse(File.ReadAllText(metadataPath)).RootElement;
            var featureOrder = metadata.GetProperty("feature_order").EnumerateArray().Select(item => item.GetString()).ToList();
            if (!featureOrder.SequenceEqual(DemandFeatureBuilder.FeatureOrder))
                throw new InvalidOperationException("The model feature order does not match DemandFeatureBuilder.FeatureOrder.");

            _inputName = metadata.GetProperty("onnx_input").GetString() ?? _inputName;
            _outputName = metadata.GetProperty("onnx_output").GetString() ?? _outputName;
            _session = new InferenceSession(modelPath);
            logger.LogInformation("Demand model {Version} loaded from {Path}", metadata.GetProperty("model_version").GetString(), modelPath);
        }
        catch (Exception exception)
        {
            problem = exception.Message;
            logger.LogError(exception, "The demand model could not be loaded; forecasts will use the baseline");
        }

        Info = new DemandModelInfo(
            _session != null,
            TryGetString(metadata, "model_version") ?? "none",
            TryGetString(metadata, "trained_at") ?? string.Empty,
            TryGetString(metadata, "algorithm") ?? string.Empty,
            metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty("data_sources", out var sources)
                ? sources.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToList()
                : [],
            metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty("confidence_base", out var confidence)
                ? confidence.GetDouble()
                : 0.5,
            metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty("metrics", out var metrics) ? metrics.Clone() : null,
            problem);
    }

    public bool IsReady => _session != null;

    public DemandModelInfo Info { get; }

    public float[] Predict(IReadOnlyList<float[]> rows)
    {
        if (_session == null) throw new InvalidOperationException("The demand model is not loaded.");
        if (rows.Count == 0) return [];
        var columns = DemandFeatureBuilder.FeatureOrder.Count;
        var data = new float[rows.Count * columns];
        for (var row = 0; row < rows.Count; row++) Array.Copy(rows[row], 0, data, row * columns, columns);

        var tensor = new DenseTensor<float>(data, new[] { rows.Count, columns });
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };
        using var results = _session.Run(inputs, new List<string> { _outputName });
        return results.First().AsEnumerable<float>().ToArray();
    }

    public void Dispose() => _session?.Dispose();

    private static string? TryGetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.ToString() : null;
}
