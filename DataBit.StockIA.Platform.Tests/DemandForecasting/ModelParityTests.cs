using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;
using DataBit.StockIA.Platform.DemandForecasting.Infrastructure.MachineLearning;

namespace DataBit.StockIA.Platform.Tests.DemandForecasting;

/// <summary>
///     ml/models/parity_fixture.json was written by the Python training script:
///     raw sales history → features → ONNX prediction. The API must rebuild the
///     same features in C# and get the same prediction from the same model.
/// </summary>
public class ModelParityTests
{
    private static JsonElement Fixture => JsonDocument.Parse(File.ReadAllText(Path.Combine(TestPaths.ModelsFolder, "parity_fixture.json"))).RootElement;

    private static OnnxDemandModel LoadModel() => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DemandModel:Folder"] = TestPaths.ModelsFolder }).Build(),
        NullLogger<OnnxDemandModel>.Instance);

    private static (float[] Features, double Prediction, float[] Expected) Case(JsonElement item)
    {
        var history = item.GetProperty("history").EnumerateObject()
            .ToDictionary(day => DateOnly.Parse(day.Name, CultureInfo.InvariantCulture), day => day.Value.GetDouble());
        var features = DemandFeatureBuilder.Build(history, DateOnly.Parse(item.GetProperty("date").GetString()!, CultureInfo.InvariantCulture),
            item.GetProperty("temp_max_c").GetDouble(), item.GetProperty("precipitation_mm").GetDouble());
        var expected = item.GetProperty("features").EnumerateArray().Select(value => (float)value.GetDouble()).ToArray();
        return (features, item.GetProperty("prediction").GetDouble(), expected);
    }

    [Fact]
    public void Feature_order_matches_the_trained_model()
    {
        var order = Fixture.GetProperty("feature_order").EnumerateArray().Select(value => value.GetString()).ToList();
        Assert.Equal(DemandFeatureBuilder.FeatureOrder, order);
    }

    [Fact]
    public void Csharp_features_equal_python_features()
    {
        foreach (var item in Fixture.GetProperty("cases").EnumerateArray())
        {
            var (features, _, expected) = Case(item);
            for (var index = 0; index < expected.Length; index++)
                Assert.True(Math.Abs(features[index] - expected[index]) < 1e-4,
                    $"{DemandFeatureBuilder.FeatureOrder[index]} on {item.GetProperty("date")}: C# {features[index]} vs Python {expected[index]}");
        }
    }

    [Fact]
    public void Onnx_model_reproduces_the_python_predictions()
    {
        using var model = LoadModel();
        Assert.True(model.IsReady, model.Info.Problem);
        var cases = Fixture.GetProperty("cases").EnumerateArray().Select(Case).ToList();
        var predictions = model.Predict(cases.Select(item => item.Features).ToList());
        for (var index = 0; index < cases.Count; index++)
            Assert.True(Math.Abs(predictions[index] - cases[index].Prediction) <= 1e-3 * Math.Max(1, cases[index].Prediction),
                $"case {index}: C# {predictions[index]} vs Python {cases[index].Prediction}");
    }

    [Fact]
    public void Model_metadata_is_exposed()
    {
        using var model = LoadModel();
        Assert.Equal("1.0.0", model.Info.ModelVersion);
        Assert.InRange(model.Info.ConfidenceBase, 0.5, 1.0);
    }
}
