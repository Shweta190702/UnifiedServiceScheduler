using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Events;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Aggregates;

// ─────────────────────────────────────────────────────────────────────────────
// Technician  (Aggregate Root)
//
// Enforced invariants:
//   1. A technician's bookings are strictly conflict-free for the full [Start, End).
//   2. The technician must hold an active, valid certification for the
//      ServiceType's category and minimum SkillLevel on the booking date.
//   3. A suspended technician cannot accept new bookings.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Technician : AggregateRoot<TechnicianId>
{
    // Private constructor required by EF Core for materialization of owned types (ContactInfo).
    private Technician() : base(TechnicianId.New())
    {
        DealershipId   = null!;
        Contact        = null!;
        EmployeeNumber = string.Empty;
    }

    private readonly List<Certification>     _certifications = [];
    private readonly List<TechnicianBooking> _bookings       = [];

    private Technician(
        TechnicianId technicianId,
        DealershipId dealershipId,
        ContactInfo  contact,
        string       employeeNumber)
        : base(technicianId)
    {
        DealershipId   = dealershipId;
        Contact        = contact;
        EmployeeNumber = employeeNumber;
        IsActive       = true;
    }

    public DealershipId DealershipId   { get; }
    public ContactInfo  Contact        { get; private set; }   // updatable
    public string       EmployeeNumber { get; }
    public bool         IsActive       { get; private set; }

    public IReadOnlyList<Certification>     Certifications => _certifications.AsReadOnly();
    public IReadOnlyList<TechnicianBooking> Bookings       => _bookings.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Technician Hire(
        TechnicianId technicianId,
        DealershipId dealershipId,
        ContactInfo  contact,
        string       employeeNumber)
    {
        ArgumentNullException.ThrowIfNull(technicianId);
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

        return new Technician(technicianId, dealershipId, contact, employeeNumber.Trim());
    }

    // ── Certification management ──────────────────────────────────────────────

    public void AddCertification(Certification certification)
    {
        ArgumentNullException.ThrowIfNull(certification);

        if (!_certifications.Contains(certification))
        {
            _certifications.Add(certification);
            Raise(new TechnicianCertificationAdded(Id, certification));
        }
    }

    // ── Scheduling ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when the technician holds an active, non-expired
    /// certification satisfying the service type's category and minimum skill level.
    /// </summary>
    public bool CanPerform(Entities.ServiceType serviceType, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return _certifications.Any(c =>
            c.SatisfiesRequirement(
                serviceType.ServiceCategory,
                serviceType.RequiredSkillLevel,
                asOf));
    }

    /// <summary>
    /// Reserves the technician for <paramref name="slot"/> and returns the booking evidence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Technician is suspended.</exception>
    /// <exception cref="InsufficientSkillException">Certification check fails.</exception>
    /// <exception cref="SchedulingConflictException">Slot overlaps an existing booking.</exception>
    public TechnicianBooking Reserve(
        AppointmentId           appointmentId,
        Entities.ServiceType    serviceType,
        TimeSlot                slot)
    {
        ArgumentNullException.ThrowIfNull(appointmentId);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(slot);

        if (!IsActive)
            throw new InvalidOperationException(
                $"Technician {EmployeeNumber} is suspended and cannot accept bookings.");

        // Invariant 2 — skill/certification check
        var asOf = DateOnly.FromDateTime(slot.Start.UtcDateTime);

        if (!CanPerform(serviceType, asOf))
        {
            var bestHeld = _certifications
                .Where(c => c.ServiceCategory.Equals(
                    serviceType.ServiceCategory, StringComparison.OrdinalIgnoreCase))
                .Select(c => (SkillLevel?)c.Level)
                .Max() ?? SkillLevel.Apprentice;

            throw new InsufficientSkillException(
                Contact.FullName, serviceType.RequiredSkillLevel, bestHeld);
        }

        // Invariant 1 — conflict check using half-open semantics
        var conflict = _bookings.FirstOrDefault(b => b.Slot.OverlapsWith(slot));
        if (conflict is not null)
            throw new SchedulingConflictException(
                "Technician", Id.Value.ToString(), conflict.Slot);

        var booking = TechnicianBooking.Create(Id, appointmentId, slot);
        _bookings.Add(booking);
        return booking;
    }

    /// <summary>Releases the booking when an appointment is cancelled.</summary>
    public void Release(AppointmentId appointmentId)
    {
        var booking = _bookings.FirstOrDefault(b => b.AppointmentId == appointmentId)
            ?? throw new InvalidOperationException(
                $"No active booking for appointment {appointmentId} on technician {EmployeeNumber}.");

        _bookings.Remove(booking);
    }

    public void Suspend()   => IsActive = false;
    public void Reinstate() => IsActive = true;
}

// ─────────────────────────────────────────────────────────────────────────────
// TechnicianBooking  (child entity of the Technician aggregate)
//
// Identity is the (TechnicianId, AppointmentId) pair — no surrogate DB key.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class TechnicianBooking
{
    // Private constructor required by EF Core for materialization of owned type (TimeSlot).
    private TechnicianBooking()
    {
        TechnicianId  = null!;
        AppointmentId = null!;
        Slot          = null!;
    }

    private TechnicianBooking(TechnicianId technicianId, AppointmentId appointmentId, TimeSlot slot)
    {
        TechnicianId  = technicianId;
        AppointmentId = appointmentId;
        Slot          = slot;
    }

    /// <summary>Natural domain identity — the pair (TechnicianId, AppointmentId) is unique.</summary>
    public TechnicianId  TechnicianId  { get; }
    public AppointmentId AppointmentId { get; }
    public TimeSlot      Slot          { get; }

    internal static TechnicianBooking Create(
        TechnicianId  technicianId,
        AppointmentId appointmentId,
        TimeSlot      slot)
        => new(technicianId, appointmentId, slot);
}
