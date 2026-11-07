using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;

public class Alert : IAuditableEntity
{
    private Alert()
    {
    }

    public Alert(int restaurantId, AlertType type, AlertSeverity severity, string message, AlertChannel channel,
        DateTimeOffset createdAt, int? inventoryItemId = null)
    {
        RestaurantId = restaurantId;
        Type = type;
        Severity = severity;
        Message = message.Trim();
        Channel = channel;
        CreatedAt = createdAt;
        InventoryItemId = inventoryItemId;
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public AlertType Type { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public AlertChannel Channel { get; private set; }
    public List<string> DeliveredChannels { get; private set; } = [];
    public bool Acknowledged { get; private set; }

    public int? InventoryItemId { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public IReadOnlyList<AlertChannel> RequiredChannels =>
        Severity == AlertSeverity.Critical ? [AlertChannel.Whatsapp, AlertChannel.Email] : [Channel];

    public AlertChannel? PendingChannel =>
        RequiredChannels.Cast<AlertChannel?>().FirstOrDefault(channel => !DeliveredChannels.Contains(channel!.Value.ToCode()));

    public bool Delivered => PendingChannel == null;

    public Alert Update(AlertType type, AlertSeverity severity, string message, AlertChannel channel)
    {
        Type = type;
        Severity = severity;
        Message = message.Trim();
        Channel = channel;
        return this;
    }

    public Alert RegisterDelivery(AlertChannel channel)
    {
        if (!DeliveredChannels.Contains(channel.ToCode())) DeliveredChannels = [..DeliveredChannels, channel.ToCode()];
        return this;
    }

    public bool Acknowledge()
    {
        if (!Delivered) return false;
        Acknowledged = true;
        return true;
    }
}