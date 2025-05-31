namespace MindControl.Results;

/// <summary>
/// Represents a failure in a memory allocation operation when the provided limit range is not within the bounds of the
/// target process applicative memory range.
/// </summary>
/// <param name="ApplicativeMemoryRange">Applicative memory range of the target process.</param>
public record LimitRangeOutOfBoundsFailure(MemoryRange ApplicativeMemoryRange)
    : Failure($"The provided limit range is not within the bounds of the target process applicative memory range ({ApplicativeMemoryRange}).")
{
    /// <summary>Applicative memory range of the target process.</summary>
    public MemoryRange ApplicativeMemoryRange { get; init; } = ApplicativeMemoryRange;
}

/// <summary>
/// Represents a failure in a memory allocation operation when no free memory large enough to accomodate the specified
/// size was found in the target process within the searched range.
/// </summary>
/// <param name="SearchedRange">Searched range in the target process.</param>
/// <param name="LastRegionAddressSearched">Last memory region address searched in the target process.</param>
public record NoFreeMemoryFailure(MemoryRange SearchedRange, UIntPtr LastRegionAddressSearched)
    : Failure($"No free memory range large enough to accomodate the specified size was found in the target process within the searched range ({SearchedRange}).")
{
    /// <summary>Searched range in the target process.</summary>
    public MemoryRange SearchedRange { get; init; } = SearchedRange;
}
