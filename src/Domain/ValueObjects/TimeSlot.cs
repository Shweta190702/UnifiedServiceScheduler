using UnifiedServiceScheduler.Domain.Exceptions;

namespace UnifiedServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Immutable half-open time interval <c>[Start, End)</c>.
/// Value Object: equality is purely structural (record semantics).
/// No parameterless constructor — no ORM can accidentally create an empty instance.
/// </summary>
public sealed record TimeSlot
{
    // ── Properties are init-only; set only via the factory ───────────────────

    /// <summary>Inclusive start of the interval.</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Exclusive end of the interval.</summary>
    public DateTimeOffset End { get; }

    /// <summary>Positive duration of the slot (derived, never stored).</summary>
    public TimeSpan Duration => End - Start;

    // ── Construction locked behind a factory ─────────────────────────────────

    // Private parameterless constructor for ORM materialization
    private TimeSlot() { }

    private TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End   = end;
    }

    /// <summary>
    /// Creates a <see cref="TimeSlot"/> from explicit start/end bounds.
    /// Throws <see cref="InvalidTimeSlotException"/> when <paramref name="end"/>
    /// is not strictly after <paramref name="start"/>.
    /// </summary>
    public static TimeSlot Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new InvalidTimeSlotException(
                $"End ({end:o}) must be strictly after Start ({start:o})");

        return new TimeSlot(start, end);
    }

    /// <summary>
    /// Creates a <see cref="TimeSlot"/> from a start point and a positive duration.
    /// </summary>
    public static TimeSlot From(DateTimeOffset start, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new InvalidTimeSlotException("Duration must be positive");

        return Create(start, start + duration);
    }

    // ── Overlap / containment logic ───────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when the two half-open intervals share at least
    /// one instant, i.e. <c>[A.Start, A.End) ∩ [B.Start, B.End) ≠ ∅</c>.
    ///
    /// <para>Formula: <c>Start &lt; other.End &amp;&amp; End &gt; other.Start</c></para>
    ///
    /// <para>Critically, two back-to-back slots do <em>not</em> overlap:
    /// <c>[09:00, 10:30)</c> and <c>[10:30, 12:00)</c> → <see langword="false"/>.</para>
    /// </summary>
    public bool OverlapsWith(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && End > other.Start;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="other"/> is fully
    /// contained within this slot (both bounds inclusive/exclusive respected).
    /// </summary>
    public bool Contains(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Start >= Start && other.End <= End;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="point"/> falls within
    /// this slot under half-open semantics: <c>Start &lt;= point &lt; End</c>.
    /// </summary>
    public bool Contains(DateTimeOffset point)
        => point >= Start && point < End;

    public override string ToString()
        => $"[{Start:yyyy-MM-dd HH:mm zzz}, {End:HH:mm zzz})";
}
