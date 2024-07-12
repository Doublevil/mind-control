namespace MindControl.Results;

/// <summary>Represents a failure in a string settings search operation.</summary>
public abstract record FindStringSettingsFailure;

/// <summary>
/// Represents a failure in a string settings search operation when the target process is not attached.
/// </summary>
public record FindStringSettingsFailureOnDetachedProcess : FindStringSettingsFailure
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
    : FindStringSettingsFailure
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
public record FindStringSettingsFailureOnIncompatibleBitness(UIntPtr Address) : FindStringSettingsFailure
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
public record FindStringSettingsFailureOnPointerReadFailure(ReadFailure Details) : FindStringSettingsFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to read a pointer while searching for string settings: {Details}";
}

/// <summary>
/// Represents a failure in a string settings search operation when the given pointer is a zero pointer.
/// </summary>
public record FindStringSettingsFailureOnZeroPointer : FindStringSettingsFailure
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
public record FindStringSettingsFailureOnStringReadFailure(ReadFailure Details) : FindStringSettingsFailure
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
public record FindStringSettingsFailureOnNoSettingsFound : FindStringSettingsFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "No adequate settings were found to read the given string from the specified pointer.";
}
