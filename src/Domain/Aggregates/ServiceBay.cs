using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Aggregates;

// ─────────────────────────────────────────────────────────────────────────────
// ServiceBay  (Aggregate Root)
//
// Enforced invariants:
//   1. A bay's bookings are strictly conflict-free for the full [Start, End).
//   2. A bay only accepts services whose required BayCapability matches its own.
//   3. A bay under maintenance cannot be booked.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class ServiceBay : AggregateRoot<ServiceBayId>
{
    // Private constructor required by EF Core for materialization.
    private ServiceBay() : base(ServiceBayId.New())
    {
        DealershipId = null!;
        BayCode      = string.Empty;
    }

    private readonly List<BayBooking> _bookings = [];

    private ServiceBay(
        ServiceBayId  id,
        DealershipId  dealershipId,
        string        bayCode,
        BayCapability capability)
        : base(id)
    {
        DealershipId       = dealershipId;
        BayCode            = bayCode;
        Capability         = capability;
        IsUnderMaintenance = false;
    }

    public DealershipId  DealershipId       { get; }
    public string        BayCode            { get; }        // e.g. "BAY-04" — immutable label
    public BayCapability Capability         { get; }
    public bool          IsUnderMaintenance { get; private set; }

    public IReadOnlyList<BayBooking> Bookings => _bookings.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ServiceBay Commission(
        ServiceBayId  id,
        DealershipId  dealershipId,
        string        bayCode,
        BayCapability capability)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(bayCode);

        return new ServiceBay(
            id, dealershipId,
            bayCode.ToUpperInvariant().Trim(),
            capability);
    }

    // ── Capability check ──────────────────────────────────────────────────────

    /// <summary>Returns <see langword="true"/> when this bay's capability matches the service type.</summary>
    public bool Supports(ServiceType serviceType)
        => Capability == serviceType.RequiredBayCapability;

    // ── Scheduling ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reserves this bay for <paramref name="slot"/> and returns the booking evidence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Bay is under maintenance or wrong capability.</exception>
    /// <exception cref="SchedulingConflictException">Slot overlaps an existing booking.</exception>
    public BayBooking Reserve(
        AppointmentId appointmentId,
        ServiceType   serviceType,
        TimeSlot      slot)
    {
        ArgumentNullException.ThrowIfNull(appointmentId);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(slot);

        if (IsUnderMaintenance)
            throw new InvalidOperationException(
                $"Bay '{BayCode}' is under maintenance and cannot be booked.");

        // Invariant 2 — capability check
        if (!Supports(serviceType))
            throw new InvalidOperationException(
                $"Bay '{BayCode}' ({Capability}) cannot service " +
                $"'{serviceType.Name}' which requires {serviceType.RequiredBayCapability}.");

        // Invariant 1 — conflict check using half-open semantics
        var conflict = _bookings.FirstOrDefault(b => b.Slot.OverlapsWith(slot));
        if (conflict is not null)
            throw new SchedulingConflictException(
                $"ServiceBay '{BayCode}'", Id.Value.ToString(), conflict.Slot);

        var booking = BayBooking.Create(Id, appointmentId, slot);
        _bookings.Add(booking);
        return booking;
    }

    /// <summary>Releases reservation when the associated appointment is cancelled.</summary>
    public void Release(AppointmentId appointmentId)
    {
        var booking = _bookings.FirstOrDefault(b => b.AppointmentId == appointmentId)
            ?? throw new InvalidOperationException(
                $"No active booking for appointment {appointmentId} on bay '{BayCode}'.");

        _bookings.Remove(booking);
    }

    public void TakeOffline(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        IsUnderMaintenance = true;
    }

    public void BringOnline() => IsUnderMaintenance = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// BayBooking  (child entity of the ServiceBay aggregate)
//
// Identity is the (ServiceBayId, AppointmentId) pair — no surrogate DB key.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class BayBooking
{
    // Private constructor required by EF Core for materialization of owned type (TimeSlot).
    private BayBooking()
    {
        BayId         = null!;
        AppointmentId = null!;
        Slot          = null!;
    }

    private BayBooking(ServiceBayId bayId, AppointmentId appointmentId, TimeSlot slot)
    {
        BayId         = bayId;
        AppointmentId = appointmentId;
        Slot          = slot;
    }

    /// <summary>Natural domain identity — the pair (ServiceBayId, AppointmentId) is unique.</summary>
    public ServiceBayId  BayId         { get; }
    public AppointmentId AppointmentId { get; }
    public TimeSlot      Slot          { get; }

    internal static BayBooking Create(
        ServiceBayId  bayId,
        AppointmentId appointmentId,
        TimeSlot      slot)
        => new(bayId, appointmentId, slot);
}
