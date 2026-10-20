using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

/// <summary>
///     Lots expire just by the passage of time (no event fires for that), so
///     every hour the automatic alerts of every restaurant are re-evaluated.
/// </summary>
public class InventoryAlertSweepJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<InventoryAlertSweepJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = int.TryParse(configuration["Alerts:SweepIntervalMinutes"], out var value) ? value : 60;
        if (minutes <= 0) return;
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
            do
            {
                await SweepAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Application stopping.
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var iam = scope.ServiceProvider.GetRequiredService<IIamContextFacade>();
            var alerts = scope.ServiceProvider.GetRequiredService<IAlertCommandService>();
            foreach (var restaurantId in await iam.GetRestaurantIds(cancellationToken))
                await alerts.Handle(new EvaluateInventoryAlertsCommand(restaurantId, null), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Inventory alert sweep failed");
        }
    }
}
