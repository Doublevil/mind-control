namespace MindControl.Results;

/// <summary>
/// Base class for failures. Can also be used directly when no specific failure type is needed.
/// </summary>
/// <param name="Message">Message describing the failure.</param>
public record Failure(string Message)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Message;
}

/// <summary>Failure that occurs when the process is not attached.</summary>
public record DetachedProcessFailure()
    : Failure("The process is not attached. It may have exited or the process memory instance may have been disposed.");

/// <summary>Represents a failure in an operating system operation when the provided arguments are invalid.</summary>
/// <param name="ArgumentName">Name of the argument that caused the failure.</param>
/// <param name="Message">Message that describes how the argument fails to meet expectations.</param>
public record InvalidArgumentFailure(string ArgumentName, string Message)
    : Failure($"The value provided for \"{ArgumentName}\" is invalid: {Message}")
{
    /// <summary>Name of the argument that caused the failure.</summary>
    public string ArgumentName { get; init; } = ArgumentName;
}

/// <summary>Represents a failure when an operation is not supported.</summary>
/// <param name="Message">Message that describes why the operation is not supported.</param>
public record NotSupportedFailure(string Message) : Failure($"This operation is not supported: {Message}");
