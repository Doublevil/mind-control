namespace MindControl.Results;

/// <summary>
/// Represents a reason for a memory reservation operation to fail.
/// </summary>
public enum ReservationFailureReason
{
    /// <summary>
    /// No space is available within the allocated memory range to reserve the specified size.
    /// </summary>
    NoSpaceAvailable
}

/// <summary>
/// Represents a failure in a memory reservation operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record ReservationFailure(ReservationFailureReason Reason);

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