namespace MindControl.Results;

/// <summary>Represents a failure in an operating system operation.</summary>
public abstract record SystemFailure;

/// <summary>Represents a failure in an operating system operation when the provided arguments are invalid.</summary>
/// <param name="ArgumentName">Name of the argument that caused the failure.</param>
/// <param name="Message">Message that describes how the argument fails to meet expectations.</param>
public record SystemFailureOnInvalidArgument(string ArgumentName, string Message) : SystemFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The value provided for \"{ArgumentName}\" is invalid: {Message}";
}

/// <summary>Represents a failure in a system API call.</summary>
/// <param name="ErrorCode">Numeric code that identifies the error. Typically provided by the operating system.</param>
/// <param name="ErrorMessage">Message that describes the error. Typically provided by the operating system.</param>
public record OperatingSystemCallFailure(int ErrorCode, string ErrorMessage) : SystemFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"A system API call failed with error code {ErrorCode}: {ErrorMessage}";
}