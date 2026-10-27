using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Tests.Fakes;
using Xunit;

namespace UnifiedServiceScheduler.Tests.Domain;

public sealed class TimeSlotAndSchedulingDomainTests
{
    // Deterministic constant timestamp: Monday, November 2, 2026 08:00:00 UTC (no DateTime.Now)
    private static readonly DateTimeOffset FixedMonday8AmUtc = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Boundary Assertion 1: Specifically verify the exact boundary edge case where
    /// Appointment A ends at 10:00:00.000 AM UTC and Appointment B starts at 10:00:00.000 AM UTC.
    /// Under half-open interval semantics [Start, End), End is exclusive and Start is inclusive.
    /// Confirm both succeed without overlap errors.
    /// </summary>
    [Fact]
    public void Schedule_BackToBackAppointments_Exact10AmBoundary_ShouldSucceed()
    {
        // Arrange: Explicit exact timestamps
        var startA = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        var boundary10AmUtc = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var endB = new DateTimeOffset(2026, 11, 2, 11, 0, 0, TimeSpan.Zero);

        var slotA = TimeSlot.Create(startA, boundary10AmUtc);             // [09:00:00.000Z, 10:00:00.000Z)
        var slotB = TimeSlot.Create(boundary10AmUtc, endB);               // [10:00:00.000Z, 11:00:00.000Z)

        // Act & Assert 1: Mathematical boundary separation
        Assert.False(slotA.OverlapsWith(slotB),
            "Slot A [09:00:00.000Z, 10:00:00.000Z) must NOT overlap with Slot B [10:00:00.000Z, 11:00:00.000Z).");
        Assert.False(slotB.OverlapsWith(slotA),
            "Slot B [10:00:00.000Z, 11:00:00.000Z) must NOT overlap with Slot A [09:00:00.000Z, 10:00:00.000Z).");

        // Act & Assert 2: Real domain aggregates reserve both without exceptions
        var dealership = TestDomainDataBuilder.CreateDealership();
        var bay = TestDomainDataBuilder.CreateBay(dealership.Id);
        var cert = Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1));
        var technician = TestDomainDataBuilder.CreateTechnician(dealership.Id, "TECH-01", certifications: [cert]);
        var serviceType = TestDomainDataBuilder.CreateStandardMaintenance();

        var apptIdA = AppointmentId.New();
        var apptIdB = AppointmentId.New();

        var bayBookingA = bay.Reserve(apptIdA, serviceType, slotA);
        var techBookingA = technician.Reserve(apptIdA, serviceType, slotA);

        var bayBookingB = bay.Reserve(apptIdB, serviceType, slotB);
        var techBookingB = technician.Reserve(apptIdB, serviceType, slotB);

        Assert.NotNull(bayBookingA);
        Assert.NotNull(techBookingA);
        Assert.NotNull(bayBookingB);
        Assert.NotNull(techBookingB);

        Assert.Equal(2, bay.Bookings.Count);
        Assert.Equal(2, technician.Bookings.Count);
    }

    /// <summary>
    /// Boundary Assertion 2: Sub-millisecond boundary precision.
    /// If Slot A spills into Slot B by even 1 millisecond [09:00:00.000Z, 10:00:00.001Z),
    /// the second reservation must be rejected with SchedulingConflictException.
    /// </summary>
    [Fact]
    public void Schedule_BoundaryCollisionBySingleMillisecond_ThrowsConflictException()
    {
        // Arrange: Slot A extends 1 millisecond into 10:00
        var startA = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        var endAWith1MsOverlap = new DateTimeOffset(2026, 11, 2, 10, 0, 0, 1, TimeSpan.Zero); // 10:00:00.001Z
        var startB = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var endB = new DateTimeOffset(2026, 11, 2, 11, 0, 0, TimeSpan.Zero);

        var slotA = TimeSlot.Create(startA, endAWith1MsOverlap);
        var slotB = TimeSlot.Create(startB, endB);

        // Act & Assert 1: Mathematical collision detection
        Assert.True(slotA.OverlapsWith(slotB));
        Assert.True(slotB.OverlapsWith(slotA));

        // Act & Assert 2: Domain aggregate conflict rejection
        var dealership = TestDomainDataBuilder.CreateDealership();
        var bay = TestDomainDataBuilder.CreateBay(dealership.Id);
        var serviceType = TestDomainDataBuilder.CreateStandardMaintenance();

        bay.Reserve(AppointmentId.New(), serviceType, slotA);

        var ex = Assert.Throws<SchedulingConflictException>(() =>
            bay.Reserve(AppointmentId.New(), serviceType, slotB));

        Assert.Contains("already booked", ex.Message);
    }

    /// <summary>
    /// Anti-Mocking Verification: Real Technician aggregate validates certifications
    /// using pure domain logic (no mock setup).
    /// </summary>
    [Fact]
    public void Schedule_TechnicianMissingRequiredSkill_ThrowsDomainExceptionOrFails()
    {
        // Arrange: Technician holds Apprentice level for Brakes
        var dealership = TestDomainDataBuilder.CreateDealership();
        var lowCert = Certification.Create("Brakes", SkillLevel.Apprentice, new DateOnly(2025, 1, 1));
        var technician = TestDomainDataBuilder.CreateTechnician(dealership.Id, "TECH-JUNIOR", certifications: [lowCert]);

        // Service requires Specialist level in EV Powertrain
        var evServiceType = TestDomainDataBuilder.CreateHighVoltageService();
        var slot = TimeSlot.From(FixedMonday8AmUtc.AddHours(2), evServiceType.BaseDuration);
        var appointmentId = AppointmentId.New();

        // Act & Assert: Pure domain exception
        var exception = Assert.Throws<InsufficientSkillException>(() =>
            technician.Reserve(appointmentId, evServiceType, slot));

        Assert.Contains(SkillLevel.Specialist.ToString(), exception.Message);
        Assert.Empty(technician.Bookings); // Unmodified schedule
    }

    /// <summary>
    /// Deterministic Working Hours: Fixed timestamp spills past 18:00 UTC closing.
    /// </summary>
    [Fact]
    public void Schedule_OutsideOperatingHours_SpillsPastClosing_Fails()
    {
        // Arrange: Monday 17:30 UTC for 1-hour service -> ends at 18:30 UTC
        var dealership = TestDomainDataBuilder.CreateDealership();
        var slotSpanningPastClosing = TimeSlot.From(
            new DateTimeOffset(2026, 11, 2, 17, 30, 0, TimeSpan.Zero),
            TimeSpan.FromHours(1));

        // Act & Assert
        var ex = Assert.Throws<OutsideWorkingHoursException>(() =>
            dealership.AssertWithinWorkingHours(slotSpanningPastClosing));

        Assert.Contains("falls outside dealership working hours", ex.Message);
    }

    /// <summary>
    /// Deterministic Working Hours: Fixed timestamp overlapping lunch break (12:00-13:00 UTC).
    /// </summary>
    [Fact]
    public void Schedule_OutsideOperatingHours_OverlapsLunchBreak_Fails()
    {
        // Arrange: Monday 11:30 - 12:30 UTC overlaps 12:00 - 13:00 break
        var dealership = TestDomainDataBuilder.CreateDealership();
        var slotOverlappingBreak = TimeSlot.From(
            new DateTimeOffset(2026, 11, 2, 11, 30, 0, TimeSpan.Zero),
            TimeSpan.FromHours(1));

        // Act & Assert
        var ex = Assert.Throws<OutsideWorkingHoursException>(() =>
            dealership.AssertWithinWorkingHours(slotOverlappingBreak));

        Assert.Contains("overlaps with scheduled break", ex.Message);
    }
}
