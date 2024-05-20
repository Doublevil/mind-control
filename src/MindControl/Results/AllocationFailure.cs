namespace MindControl.Results;

/// <summary>
/// Represents a reason for a memory allocation operation to fail.
/// </summary>
public enum AllocationFailureReason
{
    /// <summary>
    /// The provided limit range is not within the bounds of the target process applicative memory range.
    /// </summary>
    LimitRangeOutOfBounds,
    
    /// <summary>
    /// No free memory was found in the target process that would be large enough to accomodate the specified size
    /// within the searched range.
    /// </summary>
    NoFreeMemoryFound
}

/// <summary>
/// Represents a failure in a memory allocation operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record AllocationFailure(AllocationFailureReason Reason);

/// <summary>
/// Represents a failure in a memory allocation operation when the provided limit range is not within the bounds of the
/// target process applicative memory range.
/// </summary>
/// <param name="ApplicativeMemoryRange">Applicative memory range of the target process.</param>
public record AllocationFailureOnLimitRangeOutOfBounds(MemoryRange ApplicativeMemoryRange)
    : AllocationFailure(AllocationFailureReason.LimitRangeOutOfBounds)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The provided limit range is not within the bounds of the target process applicative memory range ({ApplicativeMemoryRange}).";
}

/// <summary>
/// Represents a failure in a memory allocation operation when no free memory large enough to accomodate the specified
/// size was found in the target process within the searched range.
/// </summary>
/// <param name="SearchedRange">Searched range in the target process.</param>
/// <param name="LastRegionAddressSearched">Last memory region address searched in the target process.</param>
public record AllocationFailureOnNoFreeMemoryFound(MemoryRange SearchedRange, UIntPtr LastRegionAddressSearched)
    : AllocationFailure(AllocationFailureReason.NoFreeMemoryFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"No free memory range large enough to accomodate the specified size was found in the target process within the searched range ({SearchedRange}).";
}
