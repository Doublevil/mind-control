namespace MindControl.Results;

/// <summary>Represents a failure in a thread operation.</summary>
public record ThreadFailure;

/// <summary>Represents a failure in a thread operation when the target process is not attached.</summary>
public record ThreadFailureOnDetachedProcess : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>Represents a failure in a thread operation when the arguments provided are invalid.</summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record ThreadFailureOnInvalidArguments(string Message) : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>Represents a failure in a thread operation when the thread handle has already been disposed.</summary>
public record ThreadFailureOnDisposedInstance : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The thread handle has already been disposed.";
}

/// <summary>Represents a failure in a thread operation when evaluating the pointer path to the target address.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record ThreadFailureOnPointerPathEvaluation(PathEvaluationFailure Details) : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failure when evaluating the pointer path to the target address: {Details}";
}

/// <summary>Represents a failure in a thread operation when the target function cannot be found.</summary>
/// <param name="Message">Message including details about the failure.</param>
public record ThreadFailureOnFunctionNotFound(string Message) : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Could not find the target function. {Message}";
}

/// <summary>
/// Represents a failure in a thread operation when the thread did not finish execution within the specified timeout.
/// </summary>
public record ThreadFailureOnWaitTimeout : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The thread did not finish execution within the specified timeout.";
}

/// <summary>Represents a failure in a thread operation when a waiting operation was abandoned.</summary>
public record ThreadFailureOnWaitAbandoned : ThreadFailure
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
public record ThreadFailureOnSystemFailure(string Message, SystemFailure Details) : ThreadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"A system API function call failed: {Message} / {Details}";
}