namespace MindControl.Results;

/// <summary>
/// Represents a reason for a memory read operation to fail.
/// </summary>
public enum ReadFailureReason
{
    /// <summary>
    /// Failure when evaluating the pointer path to the target memory.
    /// </summary>
    PointerPathEvaluationFailure,
    
    /// <summary>
    /// The target process is 32-bits, but the target memory address is not within the 32-bit address space.
    /// </summary>
    IncompatibleBitness,
    
    /// <summary>
    /// The target pointer is a zero pointer.
    /// </summary>
    ZeroPointer,
    
    /// <summary>
    /// Failure when invoking the system API to read the target memory.
    /// </summary>
    SystemReadFailure,
    
    /// <summary>
    /// Failure when trying to convert the bytes read from memory to the target type.
    /// </summary>
    ConversionFailure
}

/// <summary>
/// Represents a failure in a memory read operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record ReadFailure(ReadFailureReason Reason);

/// <summary>
/// Represents a failure in a memory read operation when evaluating the pointer path to the target memory.
/// </summary>
/// <param name="PathEvaluationFailure">Details about the failure.</param>
public record ReadFailureOnPointerPathEvaluation(PathEvaluationFailure PathEvaluationFailure)
    : ReadFailure(ReadFailureReason.PointerPathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to evaluate the specified pointer path: {PathEvaluationFailure}";
}

/// <summary>
/// Represents a failure in a memory read operation when the target process is 32-bits, but the target memory address is
/// not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record ReadFailureOnIncompatibleBitness(UIntPtr Address)
    : ReadFailure(ReadFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The address to read, {Address}, is a 64-bit address, but the target process is 32-bits.";
}

/// <summary>
/// Represents a failure in a memory read operation when the target pointer is a zero pointer.
/// </summary>
public record ReadFailureOnZeroPointer()
    : ReadFailure(ReadFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "The address to read is a zero pointer.";
}

/// <summary>
/// Represents a failure in a memory read operation when invoking the system API to read the target memory.
/// </summary>
/// <param name="SystemReadFailure">Details about the failure.</param>
public record ReadFailureOnSystemRead(SystemFailure SystemReadFailure)
    : ReadFailure(ReadFailureReason.SystemReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to read at the target address: {SystemReadFailure}";
}

/// <summary>
/// Represents a failure in a memory read operation when trying to convert the bytes read from memory to the target
/// type.
/// </summary>
public record ReadFailureOnConversionFailure()
    : ReadFailure(ReadFailureReason.ConversionFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "Failed to convert the bytes read from memory to the target type. Try using primitive types or structure types.";
}