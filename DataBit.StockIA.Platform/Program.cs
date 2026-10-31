using Cortex.Mediator.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ProblemDetailsFactory = DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails.ProblemDetailsFactory;
using DataBit.StockIA.Platform.AnalyticsDashboard.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.AnalyticsDashboard.Application.Services;
using DataBit.StockIA.Platform.Iam.Application.ACL;
using DataBit.StockIA.Platform.Iam.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.Iam.Application.Services;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using DataBit.StockIA.Platform.Iam.Interfaces.ACL;
using DataBit.StockIA.Platform.MlRecommendations.Application.ACL;
using DataBit.StockIA.Platform.MlRecommendations.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.MlRecommendations.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.MlRecommendations.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;
using DataBit.StockIA.Platform.MlRecommendations.Infrastructure.MachineLearning;
using DataBit.StockIA.Platform.MlRecommendations.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.MlRecommendations.Infrastructure.Weather;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.ACL;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.ACL;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Repositories;
using DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Notifications;
using DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Pipeline.BackgroundServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.ACL;
using DataBit.StockIA.Platform.RecipesManagement.Application.ACL;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;
using DataBit.StockIA.Platform.RecipesManagement.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.ACL;
using DataBit.StockIA.Platform.Resources;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.ACL;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Repositories;
using DataBit.StockIA.Platform.RestaurantRegistration.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Security;
using DataBit.StockIA.Platform.Shared.Infrastructure.Seeding;
using DataBit.StockIA.Platform.Shared.Infrastructure.Serialization;
using DataBit.StockIA.Platform.Shared.Infrastructure.Time;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.StockManagement.Application.ACL;
using DataBit.StockIA.Platform.StockManagement.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.StockManagement.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.StockManagement.Application.Services;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.ACL;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.CommandServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.QueryServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Payments;
using DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;

var builder = WebApplication.CreateBuilder(args);

// Azure App Service, Render and similar hosts give the port in $PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Controllers: kebab-case routes (api/v1/[controller]), localized validation and RFC 7807 problems.
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddLocalization();
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new QuantityJsonConverter()))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
    {
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        var factory = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState,
            StatusCodes.Status400BadRequest, localizer["InvalidModelStateTitle"].Value);
        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    });
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.ProblemDetails.Status is not (null or >= 500)) return;
    var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
    context.ProblemDetails.Title ??= localizer["UnexpectedServerError"].Value;
    context.ProblemDetails.Detail ??= localizer["UnexpectedErrorProcessingRequest"].Value;
});

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

// OpenAPI document (/openapi/v1.json) with the JWT bearer scheme; Scalar UI at /scalar.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info = new OpenApiInfo
    {
        Title = "StockIA Platform API",
        Version = "v1",
        Description = "RESTful API de StockIA (DataBite Corp): inventario inteligente para restaurantes con predicción de demanda (ML)."
    };
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT from POST /api/v1/authentication/sign-in"
    };
    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    return Task.CompletedTask;
}));

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

// Identity & Access Management
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<ExternalRestaurantService>();
builder.Services.AddScoped<ExternalNotificationService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// Restaurant Registration
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<ExternalIamService>();
builder.Services.AddScoped<ExternalSubscriptionService>();
builder.Services.AddScoped<IRestaurantCommandService, RestaurantCommandService>();
builder.Services.AddScoped<IRestaurantQueryService, RestaurantQueryService>();
builder.Services.AddScoped<IRestaurantRegistrationContextFacade, RestaurantRegistrationContextFacade>();

// Subscription & Payments
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentGateway, SimulatedPaymentGateway>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
builder.Services.AddScoped<ISubscriptionContextFacade, SubscriptionContextFacade>();

// Stock Management
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IStockConsumptionRepository, StockConsumptionRepository>();
builder.Services.AddScoped<IInventoryItemCommandService, InventoryItemCommandService>();
builder.Services.AddScoped<IInventoryItemQueryService, InventoryItemQueryService>();
builder.Services.AddScoped<IStockManagementContextFacade, StockManagementContextFacade>();

// Recipes Management (recipes and the sales that consume them)
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ExternalStockService>();
builder.Services.AddScoped<IRecipeCommandService, RecipeCommandService>();
builder.Services.AddScoped<IRecipeQueryService, RecipeQueryService>();
builder.Services.AddScoped<ISaleCommandService, SaleCommandService>();
builder.Services.AddScoped<ISaleQueryService, SaleQueryService>();
builder.Services.AddScoped<IRecipesManagementContextFacade, RecipesManagementContextFacade>();

// ML & Recommendations (demand forecast with the ONNX model, purchase and menu recommendations)
builder.Services.AddSingleton<IDemandModel, OnnxDemandModel>();
builder.Services.AddHttpClient<IWeatherForecastService, OpenMeteoWeatherService>(client => client.Timeout = TimeSpan.FromSeconds(8));
builder.Services.AddScoped<IDemandForecastRepository, DemandForecastRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IDemandForecastCommandService, DemandForecastCommandService>();
builder.Services.AddScoped<IDemandForecastQueryService, DemandForecastQueryService>();
builder.Services.AddScoped<IRecommendationCommandService, RecommendationCommandService>();
builder.Services.AddScoped<IRecommendationQueryService, RecommendationQueryService>();
builder.Services.AddScoped<IMlRecommendationsContextFacade, MlRecommendationsContextFacade>();

// Notifications & Messaging
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<INotificationSender, SimulatedNotificationSender>();
builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<INotificationsMessagingContextFacade, NotificationsMessagingContextFacade>();
builder.Services.AddHostedService<InventoryAlertSweepJob>();

// Analytics & Dashboard (composition only)
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();

var app = builder.Build();

// Migrations, plan catalog and (optionally) the demo restaurant.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseExceptionHandler();

string[] supportedCultures = ["en", "en-US", "es", "es-PE"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.ApplyCurrentCultureToResponseHeaders = true;
app.UseRequestLocalization(localizationOptions);

app.UseCors("StockiaWebApp");
app.UseRouting();
app.UseRequestAuthorization();
app.MapControllers();

if (!bool.TryParse(app.Configuration["OpenApi:Enabled"], out var openApiEnabled) || openApiEnabled)
{
    app.MapOpenApi().WithMetadata(new AllowAnonymousAttribute());
    app.MapScalarApiReference().WithMetadata(new AllowAnonymousAttribute());
    app.MapGet("/", () => Results.Redirect("/scalar")).WithMetadata(new AllowAnonymousAttribute()).ExcludeFromDescription();
    app.MapGet("/swagger", () => Results.Redirect("/scalar")).WithMetadata(new AllowAnonymousAttribute()).ExcludeFromDescription();
}

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
