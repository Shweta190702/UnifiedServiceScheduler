using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Events;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Aggregates;

// ─────────────────────────────────────────────────────────────────────────────
// Appointment  (Aggregate Root — central aggregate of this bounded context)
//
// Enforced invariants:
//   1. All four participants must be present and belong to the same Dealership.
//   2. The time slot must fit within Dealership working hours
//      (caller must invoke Dealership.AssertWithinWorkingHours before scheduling).
//   3. Technician must hold the required certification
//      (enforced inside Technician.Reserve).
//   4. Both Technician and ServiceBay must be conflict-free
//      (enforced inside their respective Reserve methods).
//   5. Only valid status transitions are allowed.
//
// Cross-aggregate coordination contract (Application Service responsibility):
//   a) Call Dealership.AssertWithinWorkingHours(slot)       → Rule 2
//   b) Call technician.Reserve(apptId, serviceType, slot)   → Rules 3 + 4a
//   c) Call serviceBay.Reserve(apptId, serviceType, slot)   → Rule 4b
//   d) Pass the returned TechnicianBooking + BayBooking into Appointment.Schedule()
//   All three steps must succeed within a single Unit of Work.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Appointment : AggregateRoot<AppointmentId>
{
    // Private constructor required by EF Core for reflection-based materialization
    // when entity contains references to owned types (Slot, VehicleSnapshot).
    // Inaccessible to external callers; domain code MUST use Appointment.Schedule().
    private Appointment() : base(AppointmentId.New())
    {
        DealershipId    = null!;
        CustomerId      = null!;
        VehicleSnapshot = null!;
        TechnicianId    = null!;
        ServiceBayId    = null!;
        ServiceTypeId   = null!;
        Slot            = null!;
        Notes           = string.Empty;
    }

    private Appointment(
        AppointmentId   id,
        DealershipId    dealershipId,
        CustomerId      customerId,
        VehicleSnapshot vehicleSnapshot,
        TechnicianId    technicianId,
        ServiceBayId    serviceBayId,
        ServiceTypeId   serviceTypeId,
        TimeSlot        slot)
        : base(id)
    {
        DealershipId    = dealershipId;
        CustomerId      = customerId;
        VehicleSnapshot = vehicleSnapshot;
        TechnicianId    = technicianId;
        ServiceBayId    = serviceBayId;
        ServiceTypeId   = serviceTypeId;
        Slot            = slot;
        Status          = AppointmentStatus.Requested;
        Notes           = string.Empty;
    }

    // ── Participant references (IDs only — no navigation properties) ──────────

    public DealershipId    DealershipId    { get; }
    public CustomerId      CustomerId      { get; }

    /// <summary>
    /// Immutable point-in-time snapshot of the vehicle captured at booking.
    /// Protects historical accuracy against subsequent Vehicle entity mutations.
    /// </summary>
    public VehicleSnapshot VehicleSnapshot { get; }

    public TechnicianId    TechnicianId    { get; }
    public ServiceBayId    ServiceBayId    { get; }
    public ServiceTypeId   ServiceTypeId   { get; }

    // ── Scheduling state ──────────────────────────────────────────────────────

    public TimeSlot          Slot        { get; }
    public AppointmentStatus Status      { get; private set; }
    public string            Notes       { get; private set; }
    public DateTimeOffset?   ActualStart { get; private set; }
    public DateTimeOffset?   ActualEnd   { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="Appointment"/> in <see cref="AppointmentStatus.Requested"/> state.
    ///
    /// <para>
    /// Requires <paramref name="technicianBookingEvidence"/> and
    /// <paramref name="bayBookingEvidence"/> as compile-time proof that both
    /// resources have already been reserved. The factory validates that each
    /// evidence object references this same <paramref name="id"/> and <paramref name="slot"/>,
    /// preventing any accidental bypass of the resource-reservation rules.
    /// </para>
    /// </summary>
    public static Appointment Schedule(
        AppointmentId     id,
        DealershipId      dealershipId,
        CustomerId        customerId,
        VehicleSnapshot   vehicleSnapshot,
        TechnicianBooking technicianBookingEvidence,
        BayBooking        bayBookingEvidence,
        ServiceTypeId     serviceTypeId,
        TimeSlot          slot,
        string?           notes = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(vehicleSnapshot);
        ArgumentNullException.ThrowIfNull(technicianBookingEvidence);
        ArgumentNullException.ThrowIfNull(bayBookingEvidence);
        ArgumentNullException.ThrowIfNull(serviceTypeId);
        ArgumentNullException.ThrowIfNull(slot);

        // ── Invariant: evidence must reference this exact appointment and slot ─
        if (technicianBookingEvidence.AppointmentId != id)
            throw new InvalidOperationException(
                "TechnicianBooking evidence belongs to a different appointment.");

        if (bayBookingEvidence.AppointmentId != id)
            throw new InvalidOperationException(
                "BayBooking evidence belongs to a different appointment.");

        if (technicianBookingEvidence.Slot != slot)
            throw new InvalidOperationException(
                "TechnicianBooking slot does not match the requested appointment slot.");

        if (bayBookingEvidence.Slot != slot)
            throw new InvalidOperationException(
                "BayBooking slot does not match the requested appointment slot.");

        var appointment = new Appointment(
            id, dealershipId, customerId, vehicleSnapshot,
            technicianBookingEvidence.TechnicianId,
            bayBookingEvidence.BayId,
            serviceTypeId, slot);

        if (!string.IsNullOrWhiteSpace(notes))
            appointment.Notes = notes.Trim();

        appointment.Raise(new AppointmentScheduled(
            id, customerId, vehicleSnapshot.Id,
            technicianBookingEvidence.TechnicianId,
            bayBookingEvidence.BayId,
            dealershipId, serviceTypeId, slot));

        return appointment;
    }

    // ── Status transitions ────────────────────────────────────────────────────

    /// <summary>Confirms the appointment — all resources are now locked.</summary>
    public void Confirm()
    {
        RequireStatus(AppointmentStatus.Requested, AppointmentStatus.Confirmed);
        Status = AppointmentStatus.Confirmed;
        Raise(new AppointmentConfirmed(Id, Slot));
    }

    /// <summary>Marks the vehicle as checked-in and work as underway.</summary>
    public void StartService(DateTimeOffset actualStart)
    {
        RequireStatus(AppointmentStatus.Confirmed, AppointmentStatus.InProgress);

        if (actualStart < Slot.Start)
            throw new InvalidOperationException(
                "Service cannot start before the scheduled slot begins.");

        Status      = AppointmentStatus.InProgress;
        ActualStart = actualStart;
        Raise(new ServiceStarted(Id, actualStart));
    }

    /// <summary>Marks service as finished; caller must release technician and bay bookings.</summary>
    public void CompleteService(DateTimeOffset actualEnd)
    {
        RequireStatus(AppointmentStatus.InProgress, AppointmentStatus.Completed);

        if (actualEnd <= ActualStart)
            throw new InvalidOperationException(
                "Actual end time must be strictly after actual start time.");

        Status    = AppointmentStatus.Completed;
        ActualEnd = actualEnd;
        Raise(new ServiceCompleted(Id, actualEnd));
    }

    /// <summary>
    /// Cancels the appointment. The Application Service must release
    /// Technician and ServiceBay bookings after this call.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.InProgress)
            throw new AppointmentStatusTransitionException(Status, AppointmentStatus.Cancelled);

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = AppointmentStatus.Cancelled;
        Raise(new AppointmentCancelled(Id, reason.Trim()));
    }

    public void MarkNoShow()
    {
        RequireStatus(AppointmentStatus.Confirmed, AppointmentStatus.NoShow);
        Status = AppointmentStatus.NoShow;
    }

    /// <summary>Updates free-text notes (forbidden on terminal statuses).</summary>
    public void UpdateNotes(string notes)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot update notes on a {Status} appointment.");

        Notes = notes?.Trim() ?? string.Empty;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RequireStatus(AppointmentStatus required, AppointmentStatus targetTransition)
    {
        if (Status != required)
            throw new AppointmentStatusTransitionException(Status, targetTransition);
    }

    public override string ToString()
        => $"Appointment({Id}) [{Status}] {Slot} Technician:{TechnicianId} Bay:{ServiceBayId}";
}
