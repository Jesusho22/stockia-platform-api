using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;

public record CreateAlertCommand(int RestaurantId, AlertType Type, AlertSeverity Severity, string Message, AlertChannel Channel);

public record UpdateAlertCommand(int AlertId, int RestaurantId, AlertType Type, AlertSeverity Severity, string Message, AlertChannel Channel);

public record DeleteAlertCommand(int AlertId, int RestaurantId);

public record AcknowledgeAlertCommand(int AlertId, int RestaurantId);

/// <summary>New delivery attempt on the first required channel without a successful one.</summary>
public record RetryAlertDeliveryCommand(int AlertId, int RestaurantId);

/// <summary>Creates or refreshes the automatic alerts of one supply, or of all when InventoryItemId is null.</summary>
public record EvaluateInventoryAlertsCommand(int RestaurantId, int? InventoryItemId);

public record GenerateRecommendationsCommand(int RestaurantId, int DemandForecastId);

public record ApplyRecommendationCommand(int RecommendationId, int RestaurantId);
