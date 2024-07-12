namespace MindControl.Results;

/// <summary>Represents a failure when an operation is not supported.</summary>
/// <param name="Message">Message that describes why the operation is not supported.</param>
public record NotSupportedFailure(string Message)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"This operation is not supported: {Message}";
    }
}