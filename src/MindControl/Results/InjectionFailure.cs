namespace MindControl.Results;

/// <summary>
/// Represents a failure in a library injection operation when the library file to inject was not found.
/// </summary>
/// <param name="LibraryPath">Path to the library file that was not found.</param>
public record LibraryFileNotFoundFailure(string LibraryPath)
    : Failure($"The library file to inject was not found at \"{LibraryPath}\".")
{
    /// <summary>Path to the library file that was not found.</summary>
    public string LibraryPath { get; init; } = LibraryPath;
}

/// <summary>
/// Represents a failure in a library injection operation when the module to inject is already loaded in the target
/// process.
/// </summary>
public record ModuleAlreadyLoadedFailure() : Failure("The module to inject is already loaded in the target process.");

/// <summary>Represents a failure in a library injection operation when the library function call fails.</summary>
public record LibraryLoadFailure()
    : Failure("The LoadLibraryW function returned an exit code of 0, indicating failure. Check that the library is valid and compatible with the target process (32-bit vs 64-bit).");
