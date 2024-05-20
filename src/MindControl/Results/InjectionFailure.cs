namespace MindControl.Results;

/// <summary>
/// Represents a reason for an injection operation to fail.
/// </summary>
public enum InjectionFailureReason
{
    /// <summary>
    /// The library file to inject was not found.
    /// </summary>
    LibraryFileNotFound,
    
    /// <summary>
    /// Failure when calling a system API function.
    /// </summary>
    SystemFailure,
    
    /// <summary>
    /// The injection timed out.
    /// </summary>
    Timeout,
    
    /// <summary>
    /// The library injection completed but the module cannot be found in the target process.
    /// </summary>
    ModuleNotFound
}

/// <summary>
/// Represents a failure in an injection operation.
/// </summary>
public abstract record InjectionFailure(InjectionFailureReason Reason);

/// <summary>
/// Represents a failure in an injection operation when the library file to inject was not found.
/// </summary>
/// <param name="LibraryPath">Path to the library file that was not found.</param>
public record InjectionFailureOnLibraryFileNotFound(string LibraryPath)
    : InjectionFailure(InjectionFailureReason.LibraryFileNotFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The library file to inject was not found at \"{LibraryPath}\".";
}

/// <summary>
/// Represents a failure in an injection operation when calling a system API function.
/// </summary>
/// <param name="Message">Message that explains the reason for the failure.</param>
/// <param name="Details">Details about the failure.</param>
public record InjectionFailureOnSystemFailure(string Message, SystemFailure Details)
    : InjectionFailure(InjectionFailureReason.SystemFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to inject the library due to a system API failure: {Details}";
}

/// <summary>
/// Represents a failure in an injection operation when the injection timed out.
/// </summary>
public record InjectionFailureOnTimeout()
    : InjectionFailure(InjectionFailureReason.Timeout)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() 
        => "The injection timed out. The thread did not return in time.";
}

/// <summary>
/// Represents a failure in an injection operation when the library injection completed but the module cannot be found
/// in the target process.
/// </summary>
public record InjectionFailureOnModuleNotFound()
    : InjectionFailure(InjectionFailureReason.ModuleNotFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The library injection completed, but the module cannot be found in the target process. The injection thread has probably failed. Check that the library is valid and compatible with the target process (32-bit vs 64-bit).";
}
