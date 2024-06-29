namespace MindControl.Results;

/// <summary>Represents a reason for a store operation to fail.</summary>
public enum StoreFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>The arguments provided to the store operation are invalid.</summary>
    InvalidArguments,
    /// <summary>The allocation operation failed.</summary>
    AllocationFailure,
    /// <summary>The reservation operation failed.</summary>
    ReservationFailure,
    /// <summary>The write operation failed.</summary>
    WriteFailure
}

/// <summary>Represents a failure in a memory store operation.</summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record StoreFailure(StoreFailureReason Reason);

/// <summary>Represents a failure in a memory store operation when the target process is not attached.</summary>
public record StoreFailureOnDetachedProcess()
    : StoreFailure(StoreFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>Represents a failure in a memory store operation when the provided arguments are invalid.</summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record StoreFailureOnInvalidArguments(string Message)
    : StoreFailure(StoreFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>Represents a failure in a memory store operation when the allocation operation failed.</summary>
/// <param name="Details">The allocation failure that caused the store operation to fail.</param>
public record StoreFailureOnAllocation(AllocationFailure Details)
    : StoreFailure(StoreFailureReason.AllocationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The allocation operation failed: {Details}";
}

/// <summary>Represents a failure in a memory store operation when the reservation operation failed.</summary>
/// <param name="Details">The reservation failure that caused the store operation to fail.</param>
public record StoreFailureOnReservation(ReservationFailure Details)
    : StoreFailure(StoreFailureReason.ReservationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The reservation operation failed: {Details}";
}

/// <summary>Represents a failure in a memory store operation when the write operation failed.</summary>
/// <param name="Details">The write failure that caused the store operation to fail.</param>
public record StoreFailureOnWrite(WriteFailure Details)
    : StoreFailure(StoreFailureReason.WriteFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The write operation failed: {Details}";
}