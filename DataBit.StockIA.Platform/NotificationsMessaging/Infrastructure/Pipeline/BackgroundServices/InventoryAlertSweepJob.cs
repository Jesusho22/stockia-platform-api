using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Pipeline.BackgroundServices;

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
            var restaurants = scope.ServiceProvider.GetRequiredService<IRestaurantRegistrationContextFacade>();
            var alerts = scope.ServiceProvider.GetRequiredService<IAlertCommandService>();
            foreach (var restaurantId in await restaurants.GetRestaurantIds(cancellationToken))
                await alerts.Handle(new EvaluateInventoryAlertsCommand(restaurantId, null), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Inventory alert sweep failed");
        }
    }
}