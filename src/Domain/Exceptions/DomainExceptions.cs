using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Exceptions;

/// <summary>
/// Base for all domain rule violations. Never crosses domain boundary —
/// application layer maps these to appropriate HTTP / gRPC status codes.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public sealed class SchedulingConflictException : DomainException
{
    public SchedulingConflictException(string resourceName, string resourceId, TimeSlot conflictingSlot)
        : base($"Resource '{resourceName}' (Id={resourceId}) is already booked during {conflictingSlot}.") { }
}

public sealed class InsufficientSkillException : DomainException
{
    public InsufficientSkillException(string technicianName, SkillLevel required, SkillLevel actual)
        : base($"Technician '{technicianName}' holds level {actual} but service requires {required}.") { }
}

public sealed class OutsideWorkingHoursException : DomainException
{
    public OutsideWorkingHoursException(TimeSlot slot, WorkingHours hours)
        : base($"Appointment slot {slot} falls outside dealership working hours {hours}.") { }

    public OutsideWorkingHoursException(TimeSlot slot, string reason)
        : base($"Appointment slot {slot} is invalid: {reason}.") { }
}

public sealed class InvalidTimeSlotException : DomainException
{
    public InvalidTimeSlotException(string reason) : base($"Invalid time slot: {reason}.") { }
}

public sealed class AppointmentStatusTransitionException : DomainException
{
    public AppointmentStatusTransitionException(AppointmentStatus from, AppointmentStatus to)
        : base($"Cannot transition appointment from '{from}' to '{to}'.") { }
}
