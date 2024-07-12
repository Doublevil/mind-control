namespace MindControl.Results;

/// <summary>Represents a failure in a memory allocation operation.</summary>
public abstract record AllocationFailure;

/// <summary>Represents a failure in a memory allocation operation when the target process is not attached.</summary>
public record AllocationFailureOnDetachedProcess : AllocationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>Represents a failure in a memory allocation operation when the provided arguments are invalid.</summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record AllocationFailureOnInvalidArguments(string Message) : AllocationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure in a memory allocation operation when the provided limit range is not within the bounds of the
/// target process applicative memory range.
/// </summary>
/// <param name="ApplicativeMemoryRange">Applicative memory range of the target process.</param>
public record AllocationFailureOnLimitRangeOutOfBounds(MemoryRange ApplicativeMemoryRange) : AllocationFailure
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
    : AllocationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"No free memory range large enough to accomodate the specified size was found in the target process within the searched range ({SearchedRange}).";
}
