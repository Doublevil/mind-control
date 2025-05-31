namespace MindControl.Results;

/// <summary>
/// Represents a failure in a memory reservation operation when the target allocation has been disposed.
/// </summary>
public record DisposedAllocationFailure() : Failure("The target allocation has been disposed.");

/// <summary>
/// Represents a failure in a memory reservation operation when no space is available within the allocated memory range
/// to reserve the specified size.
/// </summary>
public record InsufficientSpaceFailure()
    : Failure("No space is available within the allocated memory range to reserve the specified size.");