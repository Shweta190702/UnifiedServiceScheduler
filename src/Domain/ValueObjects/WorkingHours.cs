using UnifiedServiceScheduler.Domain.Exceptions;

namespace UnifiedServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Represents a non-schedulable interval within a working day (e.g. lunch break, shift transition).
/// </summary>
public sealed record BreakWindow(TimeOnly Start, TimeOnly End, string Description)
{
    public static BreakWindow Create(TimeOnly start, TimeOnly end, string description = "Break")
    {
        if (end <= start)
            throw new InvalidTimeSlotException($"Break end ({end}) must be after start ({start})");

        return new BreakWindow(start, end, description);
    }

    public bool Overlaps(TimeOnly slotStart, TimeOnly slotEnd)
        => slotStart < End && slotEnd > Start;
}

/// <summary>
/// Describes a dealership's operating window for a single day, including scheduled breaks.
/// Stored as <see cref="TimeOnly"/> to remain timezone-agnostic at the value level;
/// the application layer combines it with a <see cref="DateOnly"/> and the
/// dealership's <see cref="TimeZoneInfo"/> to produce concrete <see cref="DateTimeOffset"/> bounds.
/// </summary>
public sealed record WorkingHours
{
    private readonly List<BreakWindow> _breaks;

    public TimeOnly Opens  { get; }
    public TimeOnly Closes { get; }
    public IReadOnlyList<BreakWindow> Breaks => _breaks.AsReadOnly();

    private WorkingHours(TimeOnly opens, TimeOnly closes, List<BreakWindow> breaks)
    {
        Opens   = opens;
        Closes  = closes;
        _breaks = breaks;
    }

    public static WorkingHours Create(
        TimeOnly opens,
        TimeOnly closes,
        IEnumerable<BreakWindow>? breaks = null)
    {
        if (closes <= opens)
            throw new InvalidTimeSlotException(
                $"Closing time ({closes}) must be after opening time ({opens})");

        var breakList = breaks?.ToList() ?? [];
        foreach (var b in breakList)
        {
            if (b.Start < opens || b.End > closes)
                throw new InvalidTimeSlotException(
                    $"Break '{b.Description}' ({b.Start}–{b.End}) must fall within operating hours ({opens}–{closes})");
        }

        return new WorkingHours(opens, closes, breakList);
    }

    /// <summary>Convenience: 08:00 – 18:00 with a standard 12:00 – 13:00 lunch break.</summary>
    public static readonly WorkingHours Standard =
        Create(
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            [BreakWindow.Create(new TimeOnly(12, 0), new TimeOnly(13, 0), "Lunch Break")]);

    /// <summary>
    /// Returns true when the full <paramref name="slot"/> fits within today's working window,
    /// does not cross calendar days, and does NOT overlap any scheduled break window.
    /// </summary>
    public bool Encompasses(TimeSlot slot, TimeZoneInfo dealershipTimeZone)
    {
        var localStart = TimeZoneInfo.ConvertTime(slot.Start, dealershipTimeZone);
        var localEnd   = TimeZoneInfo.ConvertTime(slot.End,   dealershipTimeZone);

        // Appointment must not cross midnight into another calendar day
        if (localStart.Date != localEnd.Date)
            return false;

        // End is exclusive, so HH:mm of End must be <= Closes.
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime   = TimeOnly.FromDateTime(localEnd.DateTime);

        if (startTime < Opens || endTime > Closes)
            return false;

        // Invariant: Appointment cannot overlap any scheduled break
        if (_breaks.Any(b => b.Overlaps(startTime, endTime)))
            return false;

        return true;
    }

    public bool TryGetOverlappingBreak(
        TimeSlot slot,
        TimeZoneInfo dealershipTimeZone,
        out BreakWindow? overlappingBreak)
    {
        var localStart = TimeZoneInfo.ConvertTime(slot.Start, dealershipTimeZone);
        var localEnd   = TimeZoneInfo.ConvertTime(slot.End,   dealershipTimeZone);
        var startTime  = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime    = TimeOnly.FromDateTime(localEnd.DateTime);

        overlappingBreak = _breaks.FirstOrDefault(b => b.Overlaps(startTime, endTime));
        return overlappingBreak is not null;
    }

    public override string ToString()
    {
        var breaksDesc = _breaks.Count > 0
            ? $" (Breaks: {string.Join(", ", _breaks.Select(b => $"{b.Start}–{b.End}"))})"
            : string.Empty;
        return $"{Opens:HH:mm}–{Closes:HH:mm}{breaksDesc}";
    }
}
