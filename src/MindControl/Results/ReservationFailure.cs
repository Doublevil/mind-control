namespace MindControl.Results;

/// <summary>Represents a reason for a memory reservation operation to fail.</summary>
public enum ReservationFailureReason
{
    /// <summary>The target allocation has been disposed.</summary>
    DisposedAllocation,
    /// <summary>The arguments provided to the reservation operation are invalid.</summary>
    InvalidArguments,
    /// <summary>No space is available within the allocated memory range to reserve the specified size.</summary>
    NoSpaceAvailable
}

/// <summary>
/// Represents a failure in a memory reservation operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record ReservationFailure(ReservationFailureReason Reason);

/// <summary>
/// Represents a failure in a memory reservation operation when the target allocation has been disposed.
/// </summary>
public record ReservationFailureOnDisposedAllocation()
    : ReservationFailure(ReservationFailureReason.DisposedAllocation)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target allocation has been disposed.";
}

/// <summary>
/// Represents a failure in a memory reservation operation when the provided arguments are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record ReservationFailureOnInvalidArguments(string Message)
    : ReservationFailure(ReservationFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure in a memory reservation operation when no space is available within the allocated memory range
/// to reserve the specified size.
/// </summary>
public record ReservationFailureOnNoSpaceAvailable()
    : ReservationFailure(ReservationFailureReason.NoSpaceAvailable)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "No space is available within the allocated memory range to reserve the specified size.";
}