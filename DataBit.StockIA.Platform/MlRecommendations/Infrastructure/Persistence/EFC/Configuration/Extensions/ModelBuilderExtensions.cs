using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace DataBit.StockIA.Platform.MlRecommendations.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyMlRecommendationsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<DemandForecast>(entity =>
        {
            entity.HasKey(forecast => forecast.Id);
            entity.Property(forecast => forecast.Id).ValueGeneratedOnAdd();
            entity.HasIndex(forecast => new { forecast.RestaurantId, forecast.GeneratedAt });
            entity.Property(forecast => forecast.ConfidenceScore).HasPrecision(5, 4);
            entity.Property(forecast => forecast.WeatherCondition).HasMaxLength(20);
            entity.Property(forecast => forecast.WeatherSource).HasMaxLength(20);
            entity.Property(forecast => forecast.ModelVersion).HasMaxLength(40);
            entity.Property(forecast => forecast.Method).HasConversion<string>().HasMaxLength(20);
            entity.OwnsMany(forecast => forecast.DataPoints, point =>
            {
                point.ToTable("demand_forecast_data_points");
                point.WithOwner().HasForeignKey("DemandForecastId");
                point.Property<int>("Id").ValueGeneratedOnAdd();
                point.HasKey("Id");
                point.Property(item => item.Date).HasDateOnlyConversion();
                point.Property(item => item.DishName).IsRequired().HasMaxLength(120);
            });
            entity.Navigation(forecast => forecast.DataPoints).HasField("_dataPoints").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(recommendation => recommendation.Id);
            entity.Property(recommendation => recommendation.Id).ValueGeneratedOnAdd();
            entity.HasIndex(recommendation => recommendation.RestaurantId);
            entity.Property(recommendation => recommendation.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(recommendation => recommendation.Message).IsRequired().HasMaxLength(500);
            entity.Property(recommendation => recommendation.ExpectedImpact).HasMaxLength(200);
        });
    }
}
