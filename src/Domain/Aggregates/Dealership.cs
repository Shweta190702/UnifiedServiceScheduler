using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Aggregates;

// ─────────────────────────────────────────────────────────────────────────────
// Dealership  (Aggregate Root)
//
// Owns the per-day working-hours schedule and is the single authority for
// "does this time slot fall within operating hours?"
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Dealership : AggregateRoot<DealershipId>
{
    // Private constructor required by EF Core for materialization.
    private Dealership() : base(DealershipId.New())
    {
        Name     = string.Empty;
        Address  = string.Empty;
        TimeZone = TimeZoneInfo.Utc;
        InitializeDefaultSchedule();
    }

    // Null value for a DayOfWeek key means "closed that day".
    private readonly Dictionary<DayOfWeek, WorkingHours?> _schedule = [];

    private void InitializeDefaultSchedule()
    {
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            _schedule[day] = day is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? null
                : WorkingHours.Standard;
        }
    }

    private Dealership(
        DealershipId dealershipId,
        string       name,
        string       address,
        TimeZoneInfo timeZone)
        : base(dealershipId)
    {
        Name     = name;
        Address  = address;
        TimeZone = timeZone;
        InitializeDefaultSchedule();
    }

    public string       Name     { get; }
    public string       Address  { get; }
    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Read-only view of the dealership's working schedule keyed by <see cref="DayOfWeek"/>.
    /// A <see langword="null"/> value means the dealership is closed that day.
    /// </summary>
    public IReadOnlyDictionary<DayOfWeek, WorkingHours?> Schedule => _schedule.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Dealership Register(
        DealershipId dealershipId,
        string       name,
        string       address,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentNullException.ThrowIfNull(timeZone);

        return new Dealership(dealershipId, name.Trim(), address.Trim(), timeZone);
    }

    // ── Schedule management ───────────────────────────────────────────────────

    public void SetWorkingHours(DayOfWeek day, WorkingHours hours)
    {
        ArgumentNullException.ThrowIfNull(hours);
        _schedule[day] = hours;
    }

    public void CloseDay(DayOfWeek day) => _schedule[day] = null;

    // ── Core domain service: working-hours guard ──────────────────────────────

    /// <summary>
    /// Asserts that <paramref name="slot"/> falls entirely within this dealership's
    /// operating hours for the relevant day (converted to the dealership's timezone).
    /// </summary>
    /// <exception cref="OutsideWorkingHoursException">
    /// Thrown when the dealership is closed on that day or the slot exceeds opening hours.
    /// </exception>
    public void AssertWithinWorkingHours(TimeSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var localStart = TimeZoneInfo.ConvertTime(slot.Start, TimeZone);
        var dayOfWeek  = localStart.DayOfWeek;

        if (!_schedule.TryGetValue(dayOfWeek, out var hours) || hours is null)
            throw new OutsideWorkingHoursException(
                slot, $"Dealership '{Name}' is closed on {dayOfWeek}");

        if (!hours.Encompasses(slot, TimeZone))
        {
            if (hours.TryGetOverlappingBreak(slot, TimeZone, out var overlappingBreak))
            {
                throw new OutsideWorkingHoursException(
                    slot, $"Appointment slot {slot} overlaps with scheduled break '{overlappingBreak!.Description}' ({overlappingBreak.Start}–{overlappingBreak.End}).");
            }

            throw new OutsideWorkingHoursException(slot, hours);
        }
    }

    public override string ToString() => $"Dealership({Id}) {Name}";
}
