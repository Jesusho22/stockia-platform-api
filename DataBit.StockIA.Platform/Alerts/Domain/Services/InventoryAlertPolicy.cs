using System.Globalization;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Alerts.Domain.Services;

/// <summary>Stock data the policy needs (taken from Inventory &amp; Recipe Management).</summary>
public record StockSnapshot(int InventoryItemId, string Name, string Unit, decimal Quantity, decimal MinThreshold, int DaysToExpire,
    string Status);

public record AlertProposal(AlertType Type, AlertSeverity Severity, AlertChannel Channel, string Message);

/// <summary>
///     Which automatic alerts a supply lot needs right now:
///     expired → EXPIRING_SOON (critical); no stock → CRITICAL_STOCK (critical);
///     at or below the minimum → LOW_STOCK (warning); expires in 0-3 days →
///     EXPIRING_SOON (warning). Critical alerts go by WhatsApp and email.
/// </summary>
public static class InventoryAlertPolicy
{
    public const int ExpiringSoonDays = 3;

    public static IReadOnlyList<AlertProposal> Evaluate(StockSnapshot item)
    {
        var proposals = new List<AlertProposal>();
        if (item.Status == "EXPIRED")
        {
            if (item.Quantity > 0)
                proposals.Add(new AlertProposal(AlertType.ExpiringSoon, AlertSeverity.Critical, AlertChannel.Whatsapp,
                    $"{item.Name} está vencido ({Format(item.Quantity)} {item.Unit}): retíralo, no se puede vender."));
            return proposals;
        }

        if (item.Quantity <= 0)
            proposals.Add(new AlertProposal(AlertType.CriticalStock, AlertSeverity.Critical, AlertChannel.Whatsapp,
                $"{item.Name} sin stock disponible."));
        else if (item.Quantity <= item.MinThreshold)
            proposals.Add(new AlertProposal(AlertType.LowStock, AlertSeverity.Warning, AlertChannel.Whatsapp,
                $"{item.Name} por debajo del mínimo ({Format(item.Quantity)} {item.Unit} / {Format(item.MinThreshold)} {item.Unit})."));

        if (item.Quantity > 0 && item.DaysToExpire is >= 0 and <= ExpiringSoonDays)
        {
            var when = item.DaysToExpire switch
            {
                0 => "vence hoy",
                1 => "vence mañana",
                _ => $"vence en {item.DaysToExpire} días"
            };
            proposals.Add(new AlertProposal(AlertType.ExpiringSoon, AlertSeverity.Warning, AlertChannel.Email, $"{item.Name} {when}."));
        }

        return proposals;
    }

    private static string Format(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
