using Cortex.Mediator.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using DataBit.StockIA.Platform.Alerts.Application.Acl;
using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.Alerts.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Alerts.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.Alerts.Application.QueryServices;
using DataBit.StockIA.Platform.Alerts.Domain.Repositories;
using DataBit.StockIA.Platform.Alerts.Infrastructure.Notifications;
using DataBit.StockIA.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;
using DataBit.StockIA.Platform.Alerts.Interfaces.Acl;
using DataBit.StockIA.Platform.Dashboard.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.Dashboard.Application.QueryServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Acl;
using DataBit.StockIA.Platform.DemandForecasting.Application.CommandServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.QueryServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Repositories;
using DataBit.StockIA.Platform.DemandForecasting.Infrastructure.MachineLearning;
using DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Weather;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Acl;
using DataBit.StockIA.Platform.Iam.Application.Acl;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using DataBit.StockIA.Platform.Iam.Interfaces.Acl;
using DataBit.StockIA.Platform.ProductInventory.Application.Acl;
using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.ProductInventory.Application.QueryServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;
using DataBit.StockIA.Platform.ProductInventory.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;
using DataBit.StockIA.Platform.SalesOrder.Application.Acl;
using DataBit.StockIA.Platform.SalesOrder.Application.CommandServices;
using DataBit.StockIA.Platform.SalesOrder.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.SalesOrder.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.SalesOrder.Application.QueryServices;
using DataBit.StockIA.Platform.SalesOrder.Domain.Repositories;
using DataBit.StockIA.Platform.SalesOrder.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Acl;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using DataBit.StockIA.Platform.Shared.Infrastructure.Security;
using DataBit.StockIA.Platform.Shared.Infrastructure.Seeding;
using DataBit.StockIA.Platform.Shared.Infrastructure.Serialization;
using DataBit.StockIA.Platform.Shared.Infrastructure.Time;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using DataBit.StockIA.Platform.Subscription.Application.Acl;
using DataBit.StockIA.Platform.Subscription.Application.CommandServices;
using DataBit.StockIA.Platform.Subscription.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.Subscription.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Subscription.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.Subscription.Application.QueryServices;
using DataBit.StockIA.Platform.Subscription.Domain.Repositories;
using DataBit.StockIA.Platform.Subscription.Infrastructure.Payments;
using DataBit.StockIA.Platform.Subscription.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using DataBit.StockIA.Platform.Subscription.Interfaces.Acl;

var builder = WebApplication.CreateBuilder(args);

// Render, Railway and similar hosts give the port in $PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers()
    .AddDataAnnotationsLocalization()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new QuantityJsonConverter()));
builder.Services.AddProblemDetails();

// CORS — the Vue Web Application runs on another origin. "https://*.vercel.app" style patterns are allowed.
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Concat((Environment.GetEnvironmentVariable("STOCKIA_CORS_ORIGINS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .ToArray();
builder.Services.AddCors(options => options.AddPolicy("StockiaWebApp", policy => policy
    .SetIsOriginAllowed(origin => allowedOrigins.Any(allowed => OriginMatches(allowed, origin)))
    .AllowAnyMethod()
    .AllowAnyHeader()));

// Database — MySQL with EF Core. %VAR% placeholders are expanded from environment variables.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var template = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(template)) throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set.");
    options.UseMySQL(Environment.ExpandEnvironmentVariables(template))
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors();
    if (builder.Environment.IsDevelopment()) options.EnableSensitiveDataLogging();
});

// Localization — each bounded context keeps its Resources/*.resx (en, es).
builder.Services.AddLocalization();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockIA Platform API",
        Version = "v1",
        Description = "RESTful API de StockIA (DataBite Corp): inventario inteligente para restaurantes con predicción de demanda (ML)."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT from POST /api/v1/authentication/sign-in",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    options.EnableAnnotations();
    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

// Mediator — domain events between bounded contexts (StockLevelChangedEvent, DemandForecastGeneratedEvent).
builder.Services.AddCortexMediator([typeof(Program)]);

// Shared
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRestaurantClock, RestaurantClock>();
builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<DatabaseSeeder>();

// Subscription & Billing
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentGateway, SimulatedPaymentGateway>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
builder.Services.AddScoped<ISubscriptionContextFacade, SubscriptionContextFacade>();

// User & Access Management
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IRestaurantCommandService, RestaurantCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IRestaurantQueryService, RestaurantQueryService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// Inventory & Recipe Management
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IInventoryItemCommandService, InventoryItemCommandService>();
builder.Services.AddScoped<IRecipeCommandService, RecipeCommandService>();
builder.Services.AddScoped<IProductInventoryQueryService, ProductInventoryQueryService>();
builder.Services.AddScoped<IProductInventoryContextFacade, ProductInventoryContextFacade>();

// Sales & Order Management
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ISaleCommandService, SaleCommandService>();
builder.Services.AddScoped<ISaleQueryService, SaleQueryService>();
builder.Services.AddScoped<ISalesOrderContextFacade, SalesOrderContextFacade>();

// Demand Forecasting (Machine Learning)
builder.Services.AddSingleton<IDemandModel, OnnxDemandModel>();
builder.Services.AddHttpClient<IWeatherForecastService, OpenMeteoWeatherService>(client => client.Timeout = TimeSpan.FromSeconds(8));
builder.Services.AddScoped<IDemandForecastRepository, DemandForecastRepository>();
builder.Services.AddScoped<IDemandForecastCommandService, DemandForecastCommandService>();
builder.Services.AddScoped<IDemandForecastQueryService, DemandForecastQueryService>();
builder.Services.AddScoped<IDemandForecastingContextFacade, DemandForecastingContextFacade>();

// Alerts & Recommendations
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<INotificationSender, SimulatedNotificationSender>();
builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<IRecommendationCommandService, RecommendationCommandService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<IAlertsContextFacade, AlertsContextFacade>();
builder.Services.AddHostedService<InventoryAlertSweepJob>();

// Dashboard & Analytics (composition only)
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();

var app = builder.Build();

// Database schema, plan catalog and (optionally) the demo restaurant.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseGlobalExceptionHandler();

var supportedCultures = new[] { "en", "es" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

if (!bool.TryParse(app.Configuration["Swagger:Enabled"], out var swaggerEnabled) || swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "StockIA Platform API");
}

app.UseCors("StockiaWebApp");
app.UseRouting();
app.UseRequestAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).WithMetadata(new AllowAnonymousAttribute()).ExcludeFromDescription();
app.MapGet("/api/v1/health", (IRestaurantClock clock, IDemandModel model) => Results.Ok(new
{
    status = "ok",
    time = clock.Now,
    demandModel = model.IsReady ? model.Info.ModelVersion : "baseline"
})).WithMetadata(new AllowAnonymousAttribute()).WithTags("Health");

app.Run();

static bool OriginMatches(string allowed, string origin)
{
    if (allowed == "*") return true;
    if (!allowed.Contains("*.")) return string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase);
    var parts = allowed.Split("*.", 2);
    return origin.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
           origin.EndsWith("." + parts[1], StringComparison.OrdinalIgnoreCase);
}

/// <summary>Entry point (public so the integration tests can reference it).</summary>
public partial class Program
{
}
