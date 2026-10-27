using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Events;

// ─────────────────────────────────────────────────────────────────────────────
// Domain Events — raised inside aggregate methods, collected by the aggregate,
// and dispatched by the application layer AFTER persistence succeeds.
// All events are immutable records.
// ─────────────────────────────────────────────────────────────────────────────

public abstract record DomainEvent
{
    public Guid            EventId    { get; } = Guid.NewGuid();
    public DateTimeOffset  OccurredOn { get; } = DateTimeOffset.UtcNow;
}

// ── Appointment events ────────────────────────────────────────────────────────

/// <summary>Raised when a new appointment has been successfully booked.</summary>
public sealed record AppointmentScheduled(
    AppointmentId  AppointmentId,
    CustomerId     CustomerId,
    VehicleId      VehicleId,
    TechnicianId   TechnicianId,
    ServiceBayId   ServiceBayId,
    DealershipId   DealershipId,
    ServiceTypeId  ServiceTypeId,
    TimeSlot       Slot
) : DomainEvent;

/// <summary>Raised when an appointment is confirmed (resources locked).</summary>
public sealed record AppointmentConfirmed(
    AppointmentId AppointmentId,
    TimeSlot      Slot
) : DomainEvent;

/// <summary>Raised when an appointment is cancelled before service begins.</summary>
public sealed record AppointmentCancelled(
    AppointmentId AppointmentId,
    string        Reason
) : DomainEvent;

/// <summary>Raised when service work on a vehicle has started.</summary>
public sealed record ServiceStarted(
    AppointmentId AppointmentId,
    DateTimeOffset ActualStart
) : DomainEvent;

/// <summary>Raised when service is completed and the bay is released.</summary>
public sealed record ServiceCompleted(
    AppointmentId  AppointmentId,
    DateTimeOffset ActualEnd
) : DomainEvent;

// ── Technician events ─────────────────────────────────────────────────────────

/// <summary>Raised when a new certification is added to a technician's profile.</summary>
public sealed record TechnicianCertificationAdded(
    TechnicianId                           TechnicianId,
    ValueObjects.Certification             Certification
) : DomainEvent;
