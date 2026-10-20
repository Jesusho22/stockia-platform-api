using Cortex.Mediator.Notifications;
using DataBit.StockIA.Platform.Shared.Domain.Model.Events;

namespace DataBit.StockIA.Platform.Shared.Application.Internal.EventHandlers;

/// <summary>
///     Reaction of a bounded context to a domain event raised by another one
///     (e.g. Alerts reacting to StockLevelChangedEvent). Implementations are
///     discovered by Cortex.Mediator and run by IMediator.PublishAsync.
/// </summary>
/// <typeparam name="TEvent">Domain event handled.</typeparam>
public interface IEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IEvent
{
}
