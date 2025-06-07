namespace MindControl.Results;

/// <summary>
/// Represents a failure in a path evaluation operation when the target process is 32-bit, but the target memory
/// address is not within the 32-bit address space.
/// </summary>
/// <param name="PreviousAddress">Address where the value causing the issue was read. May be null if the first address
/// in the path caused the failure.</param>
public record IncompatiblePointerPathBitnessFailure(UIntPtr? PreviousAddress = null)
    : Failure("The specified pointer path contains 64-bit offsets, but the target process is 32-bit. Check the offsets in the path.");

/// <summary>
/// Represents a failure in a path evaluation operation when the base module specified in the pointer path was not
/// found.
/// </summary>
/// <param name="ModuleName">Name of the module that was not found.</param>
public record BaseModuleNotFoundFailure(string ModuleName)
    : Failure($"The module \"{ModuleName}\", referenced in the pointer path, was not found in the target process.")
{
    /// <summary>Name of the module that was not found.</summary>
    public string ModuleName { get; init; } = ModuleName;
}

/// <summary>
/// Represents a failure in a path evaluation operation when a pointer in the path is out of the target process
/// address space.
/// </summary>
/// <param name="PreviousAddress">Address that triggered the failure after the offset. May be null if the first address
/// in the path caused the failure.</param>
/// <param name="Offset">Offset that caused the failure.</param>
public record PointerOutOfRangeFailure(UIntPtr? PreviousAddress, PointerOffset Offset)
    : Failure("The pointer path evaluated a pointer to an address that is out of the target process address space range.");

/// <summary>
/// Represents a failure when a null pointer is accessed.
/// </summary>
public record ZeroPointerFailure()
    : Failure("One of the pointers accessed as part of the operation was a null pointer.");
