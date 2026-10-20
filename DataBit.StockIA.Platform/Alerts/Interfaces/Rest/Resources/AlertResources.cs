using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Resources;

/// <summary>Codes: type LOW_STOCK | CRITICAL_STOCK | EXPIRING_SOON | IOT_FAULT; severity INFO | WARNING | CRITICAL; channel WHATSAPP | EMAIL.</summary>
public record AlertResource(int Id, string Type, string Severity, string Message, DateTimeOffset CreatedAt, bool Acknowledged,
    string Channel, IReadOnlyList<string> DeliveredChannels, IReadOnlyList<string> RequiredChannels, bool Delivered, int? InventoryItemId);

public record SaveAlertResource([Required] string Type, [Required] string Severity, [Required, MinLength(5), MaxLength(500)] string Message,
    [Required] string Channel);

public record RecommendationResource(int Id, string Type, string Message, string ExpectedImpact, bool Applied, DateTimeOffset CreatedAt,
    int? DemandForecastId);
