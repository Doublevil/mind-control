namespace MindControl.Results;

/// <summary>Represents a reason for a string settings search operation to fail.</summary>
public enum FindStringSettingsFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>Failure when trying to evaluate the given pointer path.</summary>
    PointerPathEvaluation,
    /// <summary>The target process is 32-bit, but the target memory address is not within the 32-bit address space.
    /// </summary>
    IncompatibleBitness,
    /// <summary>Failure when trying to read the given pointer.</summary>
    PointerReadFailure,
    /// <summary>The given pointer is a zero pointer.</summary>
    ZeroPointer,
    /// <summary>Failure when trying to read bytes at the address pointed by the given pointer.</summary>
    StringReadFailure,
    /// <summary>No adequate settings were found to read the given string from the specified pointer.</summary>
    NoSettingsFound
}

/// <summary>
/// Represents a failure in a string settings search operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record FindStringSettingsFailure(FindStringSettingsFailureReason Reason);

/// <summary>
/// Represents a failure in a string settings search operation when the target process is not attached.
/// </summary>
public record FindStringSettingsFailureOnDetachedProcess()
    : FindStringSettingsFailure(FindStringSettingsFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure in a string settings search operation when failing to evaluate the specified pointer path.
/// </summary>
/// <param name="Details">Underlying path evaluation failure details.</param>
public record FindStringSettingsFailureOnPointerPathEvaluation(PathEvaluationFailure Details)
    : FindStringSettingsFailure(FindStringSettingsFailureReason.PointerPathEvaluation)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to evaluate the specified pointer path: {Details}";
}

/// <summary>
/// Represents a failure in a string settings search operation when the target process is 32-bit, but the target memory
/// address is not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record FindStringSettingsFailureOnIncompatibleBitness(UIntPtr Address)
    : FindStringSettingsFailure(FindStringSettingsFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The address to read, {Address}, is a 64-bit address, but the target process is 32-bit.";
}

/// <summary>
/// Represents a failure in a string settings search operation when failing to read the value of the given pointer.
/// </summary>
/// <param name="Details">Underlying read failure details.</param>
public record FindStringSettingsFailureOnPointerReadFailure(ReadFailure Details)
    : FindStringSettingsFailure(FindStringSettingsFailureReason.PointerReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to read a pointer while searching for string settings: {Details}";
}

/// <summary>
/// Represents a failure in a string settings search operation when the given pointer is a zero pointer.
/// </summary>
public record FindStringSettingsFailureOnZeroPointer()
    : FindStringSettingsFailure(FindStringSettingsFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The given pointer is a zero pointer.";
}

/// <summary>
/// Represents a failure in a string settings search operation when failing to read bytes at the address pointed by the
/// given pointer.
/// </summary>
/// <param name="Details">Underlying read failure details.</param>
public record FindStringSettingsFailureOnStringReadFailure(ReadFailure Details)
    : FindStringSettingsFailure(FindStringSettingsFailureReason.StringReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to read bytes at the address pointed by the given pointer while searching for string settings: {Details}";
}

/// <summary>
/// Represents a failure in a string settings search operation when no adequate settings were found to read the given
/// string from the specified pointer.
/// </summary>
public record FindStringSettingsFailureOnNoSettingsFound()
    : FindStringSettingsFailure(FindStringSettingsFailureReason.NoSettingsFound)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "No adequate settings were found to read the given string from the specified pointer.";
}
