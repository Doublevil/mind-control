namespace MindControl.Results;

/// <summary>Represents a failure occurring when the provided byte search pattern is invalid.</summary>
/// <param name="Message">Message that explains what makes the pattern invalid.</param>
public record InvalidBytePatternFailure(string Message)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The provided byte search pattern is invalid: {Message}";
}
