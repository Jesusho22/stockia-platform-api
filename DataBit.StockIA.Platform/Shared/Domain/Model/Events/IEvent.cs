using Cortex.Mediator.Notifications;

namespace DataBit.StockIA.Platform.Shared.Domain.Model.Events;

/// <summary>
///     Marker of the domain events exchanged between bounded contexts.
/// </summary>
public interface IEvent : INotification
{
}
