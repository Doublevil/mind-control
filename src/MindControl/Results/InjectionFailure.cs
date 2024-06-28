namespace MindControl.Results;

/// <summary>
/// Represents a reason for an injection operation to fail.
/// </summary>
public enum InjectionFailureReason
{
    /// <summary>The library file to inject was not found.</summary>
    LibraryFileNotFound,
    /// <summary>The module to inject is already loaded in the target process.</summary>
    ModuleAlreadyLoaded,
    /// <summary>Failure when trying to reserve memory to store function parameters.</summary>
    ParameterAllocationFailure,
    /// <summary>Failure when running the library loading thread.</summary>
    ThreadFailure,
    /// <summary>The library failed to load.</summary>
    LoadLibraryFailure
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
/// Represents a failure in an injection operation when the module to inject is already loaded in the target process.
/// </summary>
public record InjectionFailureOnModuleAlreadyLoaded()
    : InjectionFailure(InjectionFailureReason.ModuleAlreadyLoaded)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The module to inject is already loaded in the target process.";
}

/// <summary>
/// Represents a failure in an injection operation when trying to reserve memory to store function parameters.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record InjectionFailureOnParameterAllocation(AllocationFailure Details)
    : InjectionFailure(InjectionFailureReason.ParameterAllocationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to allocate memory to store function parameters required to inject the library: {Details}";
}

/// <summary>
/// Represents a failure in an injection operation when running the library loading thread.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record InjectionFailureOnThreadFailure(ThreadFailure Details)
    : InjectionFailure(InjectionFailureReason.ThreadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to inject the library due to a remote thread failure: {Details}";
}

/// <summary>
/// Represents a failure in an injection operation when the library function call fails.
/// </summary>
public record InjectionFailureOnLoadLibraryFailure()
    : InjectionFailure(InjectionFailureReason.LoadLibraryFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The LoadLibraryW function returned a status code of 0, indicating failure. Check that the library is valid and compatible with the target process (32-bit vs 64-bit).";
}
