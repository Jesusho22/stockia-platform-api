using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.ProductInventory.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.SalesOrder.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using DataBit.StockIA.Platform.Subscription.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Single DbContext of the platform (modular monolith over one MySQL
///     database). Each bounded context adds its own Apply...Configuration.
/// </summary>
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Subscription & Billing — before IAM, a restaurant gets a free plan on sign-up.
        builder.ApplySubscriptionConfiguration();

        // User & Access Management
        builder.ApplyIamConfiguration();

        // Inventory & Recipe Management
        builder.ApplyProductInventoryConfiguration();

        // Sales & Order Management
        builder.ApplySalesOrderConfiguration();

        // Demand Forecasting (ML)
        builder.ApplyDemandForecastingConfiguration();

        // Alerts & Recommendations
        builder.ApplyAlertsConfiguration();

        // snake_case tables and columns — must run last.
        builder.UseSnakeCaseNamingConvention();
    }
}
