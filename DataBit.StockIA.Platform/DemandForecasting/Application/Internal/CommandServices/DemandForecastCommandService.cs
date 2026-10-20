using Cortex.Mediator;
using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.DemandForecasting.Application.CommandServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Commands;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Entities;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Errors;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Events;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Repositories;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;
using DataBit.StockIA.Platform.DemandForecasting.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.Acl;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Acl;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.DemandForecasting.Application.Internal.CommandServices;

/// <summary>
///     ForecastGenerationService. For each active dish and each of the next
///     days it builds the features (sales history from Sales &amp; Order
///     Management, Peruvian calendar, weather forecast) and asks the ONNX
///     model for the expected units. With less than 14 days of sales, or
///     without a model, it uses the same-weekday baseline.
/// </summary>
public class DemandForecastCommandService(
    IDemandForecastRepository repository,
    IDemandModel demandModel,
    IWeatherForecastService weatherForecastService,
    IProductInventoryContextFacade productInventoryContextFacade,
    ISalesOrderContextFacade salesOrderContextFacade,
    IIamContextFacade iamContextFacade,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<DemandForecastCommandService> logger,
    IStringLocalizer<DemandForecastingMessages> localizer) : IDemandForecastCommandService
{
    /// <summary>Days with sales needed (out of the last 35) to trust the model.</summary>
    public const int MinimumHistoryDays = 14;

    public async Task<Result<DemandForecast>> Handle(GenerateDemandForecastCommand command, CancellationToken cancellationToken)
    {
        if (command.HorizonDays is < 1 or > DemandFeatureBuilder.MinimumLagDays)
            return Result<DemandForecast>.Failure(DemandForecastingError.InvalidHorizon, localizer[nameof(DemandForecastingError.InvalidHorizon)]);

        var recipes = await productInventoryContextFacade.GetActiveRecipes(command.RestaurantId, cancellationToken);
        var dishes = recipes.Select(recipe => recipe.DishName).Distinct().ToList();
        if (dishes.Count == 0)
            return Result<DemandForecast>.Failure(DemandForecastingError.NoDishes, localizer[nameof(DemandForecastingError.NoDishes)]);

        var today = clock.Today;
        var historyFrom = today.AddDays(-(DemandFeatureBuilder.HistoryDaysRequired + 1));
        var sales = await salesOrderContextFacade.GetDailyUnitsByDish(command.RestaurantId, historyFrom, today.AddDays(-1), cancellationToken);
        var history = dishes.ToDictionary(dish => dish, dish => (IReadOnlyDictionary<DateOnly, double>)sales
            .Where(row => row.DishName == dish).ToDictionary(row => row.Date, row => (double)row.Units));
        var daysWithSales = sales.Select(row => row.Date).Distinct().Count();
        var coverage = Math.Min(1.0, daysWithSales / (double)(DemandFeatureBuilder.HistoryDaysRequired + 1));

        var location = await iamContextFacade.GetRestaurantLocation(command.RestaurantId, cancellationToken) ?? (-12.0464, -77.0428);
        var weather = await weatherForecastService.GetDailyForecastAsync(location.Item1, location.Item2, today, command.HorizonDays,
            cancellationToken);

        var rows = new List<(DailyWeather Day, string Dish, float[] Features)>();
        foreach (var day in weather)
        foreach (var dish in dishes)
            rows.Add((day, dish, DemandFeatureBuilder.Build(history[dish], day.Date, day.TempMaxC, day.PrecipitationMm)));

        var useModel = demandModel.IsReady && daysWithSales >= MinimumHistoryDays;
        float[] predictions;
        if (useModel)
        {
            try
            {
                predictions = demandModel.Predict(rows.Select(row => row.Features).ToList());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The demand model failed; using the baseline");
                useModel = false;
                predictions = [];
            }
        }
        else
        {
            predictions = [];
        }

        if (!useModel) predictions = rows.Select(row => (float)DemandFeatureBuilder.Baseline(row.Features)).ToArray();

        var points = rows.Select((row, index) => new ForecastDataPoint(row.Day.Date, row.Dish,
            (int)Math.Round(predictions[index], MidpointRounding.AwayFromZero), row.Day.TempMaxC, row.Day.PrecipitationMm));
        var confidence = useModel
            ? Math.Clamp(demandModel.Info.ConfidenceBase * coverage, 0.3, 0.95)
            : Math.Clamp(0.5 * coverage, 0.2, 0.6);
        var forecast = new DemandForecast(command.RestaurantId, clock.Now.ToUniversalTime(), Math.Round((decimal)confidence, 2),
            WeatherConditionClassifier.Classify(weather[0]), weather[0].Source,
            useModel ? ForecastMethod.MachineLearning : ForecastMethod.Baseline,
            useModel ? demandModel.Info.ModelVersion : "baseline-same-weekday-4w", points);

        await repository.AddAsync(forecast, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        try
        {
            await mediator.PublishAsync(new DemandForecastGeneratedEvent(command.RestaurantId, forecast.Id), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Recommendations could not be generated for forecast {Id}", forecast.Id);
        }

        return Result<DemandForecast>.Success(forecast);
    }
}
