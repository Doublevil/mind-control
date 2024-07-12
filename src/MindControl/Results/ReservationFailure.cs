namespace MindControl.Results;

/// <summary>Represents a failure in a memory reservation operation.</summary>
public abstract record ReservationFailure;

/// <summary>
/// Represents a failure in a memory reservation operation when the target allocation has been disposed.
/// </summary>
public record ReservationFailureOnDisposedAllocation : ReservationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target allocation has been disposed.";
}

/// <summary>
/// Represents a failure in a memory reservation operation when the provided arguments are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record ReservationFailureOnInvalidArguments(string Message) : ReservationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure in a memory reservation operation when no space is available within the allocated memory range
/// to reserve the specified size.
/// </summary>
public record ReservationFailureOnNoSpaceAvailable : ReservationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "No space is available within the allocated memory range to reserve the specified size.";
}