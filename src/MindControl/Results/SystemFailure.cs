namespace MindControl.Results;

/// <summary>Represents a failure in a system API call.</summary>
/// <param name="SystemApiName">Name of the system API function that failed.</param>
/// <param name="TopLevelOperationName">Friendly name of the operation that was attempted. As multiple system API calls
/// may be made to perform a single manipulation, this name is used to identify the broader operation that failed.
/// </param>
/// <param name="ErrorCode">Numeric code that identifies the error. Typically provided by the operating system.</param>
/// <param name="SystemMessage">Message that describes the error, provided by the operating system.</param>
public record OperatingSystemCallFailure(string SystemApiName, string TopLevelOperationName, int ErrorCode,
    string SystemMessage)
    : Failure($"A system API call to {SystemApiName} as part of a {TopLevelOperationName} operation failed with error code {ErrorCode}: {SystemMessage}")
{
    /// <summary>Name of the system API function that failed.</summary>
    public string SystemApiName { get; init; } = SystemApiName;

    /// <summary>Friendly name of the operation that was attempted. As multiple system API calls
    /// may be made to perform a single manipulation, this name is used to identify the broader operation that failed.
    /// </summary>
    public string TopLevelOperationName { get; init; } = TopLevelOperationName;

    /// <summary>Numeric code that identifies the error. Typically provided by the operating system.</summary>
    public int ErrorCode { get; init; } = ErrorCode;

    /// <summary>Message that describes the error, provided by the operating system.</summary>
    public string SystemMessage { get; init; } = SystemMessage;
}