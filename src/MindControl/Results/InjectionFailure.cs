namespace MindControl.Results;

/// <summary>Represents a failure in a library injection operation.</summary>
public abstract record InjectionFailure;

/// <summary>Represents a failure in a library injection operation when the target process is not attached.</summary>
public record InjectionFailureOnDetachedProcess : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure in a library injection operation when the library file to inject was not found.
/// </summary>
/// <param name="LibraryPath">Path to the library file that was not found.</param>
public record InjectionFailureOnLibraryFileNotFound(string LibraryPath) : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The library file to inject was not found at \"{LibraryPath}\".";
}

/// <summary>
/// Represents a failure in a library injection operation when the module to inject is already loaded in the target
/// process.
/// </summary>
public record InjectionFailureOnModuleAlreadyLoaded : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The module to inject is already loaded in the target process.";
}

/// <summary>Represents a failure in a library injection operation when trying to store function parameters.</summary>
/// <param name="Details">Details about the failure.</param>
public record InjectionFailureOnParameterStorage(StoreFailure Details) : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to store function parameters required to inject the library: {Details}";
}

/// <summary>Represents a failure in a library injection operation when running the library loading thread.</summary>
/// <param name="Details">Details about the failure.</param>
public record InjectionFailureOnThreadFailure(ThreadFailure Details) : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to inject the library due to a remote thread failure: {Details}";
}

/// <summary>Represents a failure in a library injection operation when the library function call fails.</summary>
public record InjectionFailureOnLoadLibraryFailure : InjectionFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The LoadLibraryW function returned an exit code of 0, indicating failure. Check that the library is valid and compatible with the target process (32-bit vs 64-bit).";
}
