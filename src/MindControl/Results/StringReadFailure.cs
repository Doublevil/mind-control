namespace MindControl.Results;

/// <summary>
/// Represents a reason for a string read operation to fail.
/// </summary>
public enum StringReadFailureReason
{
    /// <summary>
    /// Failure when evaluating the pointer path to the target address.
    /// </summary>
    PointerPathEvaluationFailure,
    
    /// <summary>
    /// The string read operation failed because the settings provided are invalid.
    /// </summary>
    InvalidSettings,
    
    /// <summary>
    /// The string read operation failed because the target process is 32-bit, but the target memory address is not
    /// within the 32-bit address space.
    /// </summary>
    IncompatibleBitness,
    
    /// <summary>
    /// The string read operation failed because the target pointer is a zero pointer.
    /// </summary>
    ZeroPointer,
    
    /// <summary>
    /// The pointer read operation failed.
    /// </summary>
    PointerReadFailure,
    
    /// <summary>
    /// A read operation failed when attempting to read bytes from the actual string.
    /// </summary>
    StringBytesReadFailure,
    
    /// <summary>
    /// The length prefix of the string was evaluated to a value exceeding the configured max length, or a null
    /// terminator was not found within the configured max length.
    /// </summary>
    StringTooLong
}

/// <summary>
/// Represents a failure in a string read operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record StringReadFailure(StringReadFailureReason Reason);

/// <summary>
/// Represents a failure in a string read operation when the pointer path evaluation failed.
/// </summary>
/// <param name="Details">Details about the path evaluation failure.</param>
public record StringReadFailureOnPointerPathEvaluation(PathEvaluationFailure Details)
    : StringReadFailure(StringReadFailureReason.PointerPathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to evaluate the specified pointer path: {Details}";
}

/// <summary>
/// Represents a failure in a string read operation when the settings provided are invalid.
/// </summary>
public record StringReadFailureOnInvalidSettings()
    : StringReadFailure(StringReadFailureReason.InvalidSettings)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The provided string settings are invalid. They must specify either a length prefix or a null terminator.";
}

/// <summary>
/// Represents a failure in a string read operation when the target process is 32-bit, but the target memory address is
/// not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record StringReadFailureOnIncompatibleBitness(UIntPtr Address)
    : StringReadFailure(StringReadFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The target address {Address} is not within the 32-bit address space.";
}

/// <summary>
/// Represents a failure in a string read operation when the target pointer is a zero pointer.
/// </summary>
public record StringReadFailureOnZeroPointer()
    : StringReadFailure(StringReadFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target address is a zero pointer.";
}

/// <summary>
/// Represents a failure in a string read operation when the pointer read operation failed.
/// </summary>
/// <param name="Details">Details about the pointer read failure.</param>
public record StringReadFailureOnPointerReadFailure(ReadFailure Details)
    : StringReadFailure(StringReadFailureReason.PointerReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The pointer read operation failed: {Details}";
}

/// <summary>
/// Represents a failure in a string read operation when a read operation on the string bytes failed.
/// </summary>
/// <param name="Address">Address where the string read operation failed.</param>
/// <param name="Details">Details about the read failure.</param>
public record StringReadFailureOnStringBytesReadFailure(UIntPtr Address, ReadFailure Details)
    : StringReadFailure(StringReadFailureReason.StringBytesReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The string read operation failed at address {Address}: {Details}"; 
}

/// <summary>
/// Represents a failure in a string read operation when the string length prefix was evaluated to a value exceeding the
/// configured max length, or a null terminator was not found within the configured max length.
/// </summary>
/// <param name="LengthPrefixValue">Length read from the length prefix bytes, in case a length prefix was set.</param>
public record StringReadFailureOnStringTooLong(ulong? LengthPrefixValue)
    : StringReadFailure(StringReadFailureReason.StringTooLong)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => LengthPrefixValue != null
        ? $"The string was found with a length prefix of {LengthPrefixValue}, which exceeds the configured max length."
        : "String reading was aborted because no null terminator was found within the configured max length.";
}
