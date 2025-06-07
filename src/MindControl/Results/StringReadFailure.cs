namespace MindControl.Results;

/// <summary>Represents a failure in a string read operation when the settings provided are invalid.</summary>
public record InvalidStringSettingsFailure()
    : Failure("The provided string settings are invalid. They must specify either a length prefix or a null terminator.");

/// <summary>
/// Represents a failure in a string read operation when the string length prefix was evaluated to a value exceeding the
/// configured max length, or a null terminator was not found within the configured max length.
/// </summary>
/// <param name="LengthPrefixValue">Length read from the length prefix bytes, in case a length prefix was set.</param>
public record StringTooLongFailure(ulong? LengthPrefixValue) : Failure(LengthPrefixValue != null
        ? $"The string was found with a length prefix of {LengthPrefixValue}, which exceeds the configured max length."
        : "String reading was aborted because no null terminator was found within the configured max length.")
{
    /// <summary>Length read from the length prefix bytes, in case a length prefix was set.</summary>
    public ulong? LengthPrefixValue { get; init; } = LengthPrefixValue;
}

/// <summary>
/// Represents a failure in a string settings search operation when no adequate settings were found to read the given
/// string from the specified pointer.
/// </summary>
public record UndeterminedStringSettingsFailure()
    : Failure("No adequate settings were found to read the given string from the specified pointer.");
