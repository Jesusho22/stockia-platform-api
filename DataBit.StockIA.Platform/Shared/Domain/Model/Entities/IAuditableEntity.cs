namespace DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

/// <summary>
///     Entities with CreatedAt/UpdatedAt stamped by the server
///     (AuditableEntityInterceptor), never taken from client input.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
