using UnifiedServiceScheduler.Domain.Events;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Aggregates;

// ─────────────────────────────────────────────────────────────────────────────
// AggregateRoot — base class that carries domain events.
// No ORM annotations, no persistence concerns.
// ─────────────────────────────────────────────────────────────────────────────

public abstract class AggregateRoot<TId>
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; }

    /// <summary>Read-only view of pending domain events.</summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(DomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>
    /// Called by the application/infrastructure layer after all events are
    /// dispatched, so the aggregate doesn't carry stale events on re-read.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
