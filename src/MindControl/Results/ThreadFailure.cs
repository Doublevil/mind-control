namespace MindControl.Results;

/// <summary>Represents a reason for a thread operation to fail.</summary>
public enum ThreadFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>Invalid arguments were provided to the thread operation.</summary>
    InvalidArguments,
    /// <summary>The thread handle has already been disposed.</summary>
    DisposedInstance,
    /// <summary>Failure when evaluating the pointer path to the target address.</summary>
    PointerPathEvaluationFailure,
    /// <summary>The target function cannot be found.</summary>
    FunctionNotFound,
    /// <summary>Failure when waiting for a thread to finish execution for too long.</summary>
    ThreadWaitTimeout,
    /// <summary>Failure when a waiting operation was abandoned.</summary>
    WaitAbandoned,
    /// <summary>Failure when calling a system API function.</summary>
    SystemFailure
}

/// <summary>
/// Represents a failure in a thread operation.
/// </summary>
public record ThreadFailure(ThreadFailureReason Reason);

/// <summary>
/// Represents a failure in a thread operation when the target process is not attached.
/// </summary>
public record ThreadFailureOnDetachedProcess()
    : ThreadFailure(ThreadFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure in a thread operation when the arguments provided are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record ThreadFailureOnInvalidArguments(string Message)
    : ThreadFailure(ThreadFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure in a thread operation when the thread handle has already been disposed.
/// </summary>
public record ThreadFailureOnDisposedInstance()
    : ThreadFailure(ThreadFailureReason.DisposedInstance)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The thread handle has already been disposed.";
}

/// <summary>
/// Represents a failure in a thread operation when evaluating the pointer path to the target address.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record ThreadFailureOnPointerPathEvaluation(PathEvaluationFailure Details)
    : ThreadFailure(ThreadFailureReason.PointerPathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failure when evaluating the pointer path to the target address: {Details}";
}

/// <summary>
/// Represents a failure in a thread operation when the target function cannot be found.
/// </summary>
/// <param name="Message">Message including details about the failure.</param>
public record ThreadFailureOnFunctionNotFound(string Message)
    : ThreadFailure(ThreadFailureReason.FunctionNotFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Could not find the target function. {Message}";
}

/// <summary>
/// Represents a failure in a thread operation when the thread did not finish execution within the specified timeout.
/// </summary>
public record ThreadFailureOnWaitTimeout()
    : ThreadFailure(ThreadFailureReason.ThreadWaitTimeout)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The thread did not finish execution within the specified timeout.";
}

/// <summary>
/// Represents a failure in a thread operation when a waiting operation was abandoned.
/// </summary>
public record ThreadFailureOnWaitAbandoned()
    : ThreadFailure(ThreadFailureReason.WaitAbandoned)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The waiting operation was abandoned.";
}

/// <summary>
/// Represents a failure in a thread operation when invoking a system API function.
/// </summary>
/// <param name="Message">Message that details what operation failed.</param>
/// <param name="Details">Details about the failure.</param>
public record ThreadFailureOnSystemFailure(string Message, SystemFailure Details)
    : ThreadFailure(ThreadFailureReason.SystemFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"A system API function call failed: {Message} / {Details}";
}