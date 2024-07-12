namespace MindControl.Results;

/// <summary>Represents a failure in a path evaluation operation.</summary>
public abstract record PathEvaluationFailure;

/// <summary>Represents a failure in a path evaluation operation when the target process is not attached.</summary>
public record PathEvaluationFailureOnDetachedProcess : PathEvaluationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure in a path evaluation operation when the target process is 32-bit, but the target memory
/// address is not within the 32-bit address space.
/// </summary>
/// <param name="PreviousAddress">Address where the value causing the issue was read. May be null if the first address
/// in the path caused the failure.</param>
public record PathEvaluationFailureOnIncompatibleBitness(UIntPtr? PreviousAddress = null) : PathEvaluationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The specified pointer path contains 64-bit offsets, but the target process is 32-bit.";
}

/// <summary>
/// Represents a failure in a path evaluation operation when the base module specified in the pointer path was not
/// found.
/// </summary>
/// <param name="ModuleName">Name of the module that was not found.</param>
public record PathEvaluationFailureOnBaseModuleNotFound(string ModuleName) : PathEvaluationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The module \"{ModuleName}\", referenced in the pointer path, was not found in the target process.";
}

/// <summary>
/// Represents a failure in a path evaluation operation when a pointer in the path is out of the target process
/// address space.
/// </summary>
/// <param name="PreviousAddress">Address that triggered the failure after the offset. May be null if the first address
/// in the path caused the failure.</param>
/// <param name="Offset">Offset that caused the failure.</param>
public record PathEvaluationFailureOnPointerOutOfRange(UIntPtr? PreviousAddress, PointerOffset Offset)
    : PathEvaluationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The pointer path evaluated a pointer to an address that is out of the target process address space range.";
}

/// <summary>
/// Represents a failure in a path evaluation operation when invoking the system API to read an address.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
/// <param name="Details">Details about the failure.</param>
public record PathEvaluationFailureOnPointerReadFailure(UIntPtr Address, ReadFailure Details) : PathEvaluationFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to read a pointer at the address {Address}: {Details}";
}