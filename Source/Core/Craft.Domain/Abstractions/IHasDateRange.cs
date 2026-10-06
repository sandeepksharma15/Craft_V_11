namespace Craft.Domain.Abstractions;

/// <summary>
/// Abstraction for models that represent a time-bounded assignment.
/// A record is considered "current" when it has a <see cref="StartDate"/> and no <see cref="EndDate"/>.
/// </summary>
public interface IHasDateRange
{
    /// <summary>The date the assignment began; <see langword="null"/> when not yet recorded.</summary>
    DateOnly? StartDate { get; set; }

    /// <summary>The date the assignment ended; null indicates an open-ended range.</summary>
    DateOnly? EndDate { get; set; }

    /// <summary>Returns <see langword="true"/> when this is the active (open-ended) assignment.</summary>
    bool IsCurrentAssignment => StartDate.HasValue && !EndDate.HasValue;

    /// <summary>
    /// Determines whether a given date falls within the date range.
    /// </summary>
    bool IsDateInRange(DateOnly date) => StartDate.HasValue && date >= StartDate.Value && (!EndDate.HasValue || date <= EndDate.Value);

    /// <summary>Checks overlap for closed ranges; an open-ended range returns false.</summary>
    bool OverlapsWith(DateOnly otherStart, DateOnly otherEnd) =>
        StartDate.HasValue && EndDate.HasValue &&
        otherStart <= EndDate.Value && otherEnd >= StartDate.Value;

    /// <summary>Checks overlap for closed ranges; either open-ended range returns false.</summary>
    bool OverlapsWith(IHasDateRange other) =>
        StartDate.HasValue && EndDate.HasValue &&
        other.StartDate.HasValue && other.EndDate.HasValue &&
        other.StartDate.Value <= EndDate.Value && other.EndDate.Value >= StartDate.Value;

    /// <summary>
    /// Determines whether the current date falls within the date range.
    /// </summary>
    bool IsTodayInRange() => IsDateInRange(DateOnly.FromDateTime(DateTime.Today));

    /// <summary>
    /// Determines whether the date range is in the past.
    /// </summary>
    bool IsInPast() => EndDate.HasValue && EndDate.Value < DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Determines whether the date range is in the future.
    /// </summary>
    bool IsInFuture() => StartDate.HasValue && StartDate.Value > DateOnly.FromDateTime(DateTime.Today);
}
