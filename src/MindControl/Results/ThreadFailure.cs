namespace MindControl.Results;

/// <summary>Represents a failure in a thread operation when the thread handle has already been disposed.</summary>
public record DisposedThreadFailure() : Failure("The thread handle has already been disposed.");

/// <summary>Represents a failure in a thread operation when the target function cannot be found.</summary>
/// <param name="Message">Message including details about the failure.</param>
public record FunctionNotFoundFailure(string Message) : Failure($"Could not find the target function. {Message}");

/// <summary>
/// Represents a failure in a thread operation when the thread did not finish execution within the specified timeout.
/// </summary>
public record ThreadWaitTimeoutFailure() : Failure("The thread did not terminate within the specified time frame.");

/// <summary>Represents a failure in a thread operation when a waiting operation was abandoned.</summary>
public record ThreadWaitAbandonedFailure() : Failure("The waiting operation was abandoned.");