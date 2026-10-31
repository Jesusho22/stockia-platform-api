using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.MlRecommendations.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.RecipesManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.RestaurantRegistration.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;
using DataBit.StockIA.Platform.StockManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Single DbContext of the platform (modular monolith over one MySQL
///     database). Each bounded context adds its own Apply...Configuration;
///     the schema is created by the EF Core migrations in ../Migrations.
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

        builder.ApplyIamConfiguration();
        builder.ApplyRestaurantRegistrationConfiguration();
        builder.ApplySubscriptionPaymentsConfiguration();
        builder.ApplyStockManagementConfiguration();
        builder.ApplyRecipesManagementConfiguration();
        builder.ApplyMlRecommendationsConfiguration();
        builder.ApplyNotificationsMessagingConfiguration();

        // snake_case tables and columns — must run last.
        builder.UseSnakeCaseNamingConvention();
    }
}
