namespace MindControl.Results;

/// <summary>Represents a failure in a memory store operation.</summary>
public abstract record StoreFailure;

/// <summary>Represents a failure in a memory store operation when the target process is not attached.</summary>
public record StoreFailureOnDetachedProcess : StoreFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>Represents a failure in a memory store operation when the provided arguments are invalid.</summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record StoreFailureOnInvalidArguments(string Message) : StoreFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>Represents a failure in a memory store operation when the allocation operation failed.</summary>
/// <param name="Details">The allocation failure that caused the store operation to fail.</param>
public record StoreFailureOnAllocation(AllocationFailure Details) : StoreFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The allocation operation failed: {Details}";
}

/// <summary>Represents a failure in a memory store operation when the reservation operation failed.</summary>
/// <param name="Details">The reservation failure that caused the store operation to fail.</param>
public record StoreFailureOnReservation(ReservationFailure Details) : StoreFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The reservation operation failed: {Details}";
}

/// <summary>Represents a failure in a memory store operation when the write operation failed.</summary>
/// <param name="Details">The write failure that caused the store operation to fail.</param>
public record StoreFailureOnWrite(WriteFailure Details) : StoreFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The write operation failed: {Details}";
}