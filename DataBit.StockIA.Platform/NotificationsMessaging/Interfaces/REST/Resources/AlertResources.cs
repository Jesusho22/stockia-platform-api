using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST.Resources;

public record AlertResource(int Id, string Type, string Severity, string Message, DateTimeOffset CreatedAt, 
    bool Acknowledged, string Channel, IReadOnlyList<string> DeliveredChannels, IReadOnlyList<string> RequiredChannels, 
    bool Delivered, int? InventoryItemId);

public record SaveAlertResource([Required] string Type, [Required] string Severity, 
    [Required, MinLength(5), MaxLength(500)] string Message, [Required] string Channel);