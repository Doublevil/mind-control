namespace MindControl.Results;

/// <summary>
/// Represents a reason for a memory write operation to fail.
/// </summary>
public enum WriteFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>Failure when evaluating the pointer path to the target memory.</summary>
    PointerPathEvaluationFailure,
    /// <summary>The arguments provided to the memory read operation are invalid.</summary>
    InvalidArguments,
    /// <summary>The type to write is not supported.</summary>
    UnsupportedType,
    /// <summary>The target process is 32-bit, but the target memory address is not within the 32-bit address space.
    /// </summary>
    IncompatibleBitness,
    /// <summary>The target address is a zero pointer.</summary>
    ZeroPointer,
    /// <summary>Failure when invoking the system API to remove the protection properties of the target memory space.
    /// </summary>
    SystemProtectionRemovalFailure,
    /// <summary>Failure when invoking the system API to restore the protection properties of the target memory space
    /// after writing.</summary>
    SystemProtectionRestorationFailure,
    /// <summary>Failure when invoking the system API to write bytes in memory.</summary>
    SystemWriteFailure,
    /// <summary>Failure when trying to convert the value to write to an array of bytes to write in memory.</summary>
    ConversionFailure
}

/// <summary>
/// Represents a failure in a memory write operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record WriteFailure(WriteFailureReason Reason);

/// <summary>
/// Represents a failure in a memory write operation when the target process is not attached.
/// </summary>
public record WriteFailureOnDetachedProcess()
    : WriteFailure(WriteFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure in a memory write operation when evaluating the pointer path to the target memory.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record WriteFailureOnPointerPathEvaluation(PathEvaluationFailure Details)
    : WriteFailure(WriteFailureReason.PointerPathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to evaluate the specified pointer path: {Details}";
}

/// <summary>
/// Represents a failure in a memory write operation when the arguments provided are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record WriteFailureOnInvalidArguments(string Message)
    : WriteFailure(WriteFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure in a memory write operation when the value to write cannot be converted to an array of bytes.
/// </summary>
/// <param name="Type">Type that caused the failure.</param>
public record WriteFailureOnUnsupportedType(Type Type)
    : WriteFailure(WriteFailureReason.UnsupportedType)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The type {Type} is not supported for writing.";
}

/// <summary>
/// Represents a failure in a memory write operation when the target process is 32-bit, but the target memory address
/// is not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record WriteFailureOnIncompatibleBitness(UIntPtr Address)
    : WriteFailure(WriteFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The pointer to write, {Address}, is too large for a 32-bit process. If you want to write an 8-byte value and not a memory address, use a ulong instead.";
}

/// <summary>
/// Represents a failure in a memory write operation when the address to write is a zero pointer.
/// </summary>
public record WriteFailureOnZeroPointer()
    : WriteFailure(WriteFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The address to write is a zero pointer.";
}

/// <summary>
/// Represents a failure in a memory write operation when the system API call to remove the protection properties of
/// the target memory space fails.
/// </summary>
/// <param name="Address">Address where the operation failed.</param>
/// <param name="Details">Details about the failure.</param>
public record WriteFailureOnSystemProtectionRemoval(UIntPtr Address, SystemFailure Details)
    : WriteFailure(WriteFailureReason.SystemProtectionRemovalFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to remove the protection of the memory at address {Address:X}: \"{Details}\".{Environment.NewLine}Change the memory protection strategy to {nameof(MemoryProtectionStrategy)}.{nameof(MemoryProtectionStrategy.Ignore)} to prevent memory protection removal. As protection removal is the first step when writing a value, it may simply be that the provided target address does not point to valid memory.";
}

/// <summary>
/// Represents a failure in a memory write operation when the system API call to restore the protection properties of
/// the target memory space after writing fails.
/// </summary>
/// <param name="Address">Address where the operation failed.</param>
/// <param name="Details">Details about the failure.</param>
public record WriteFailureOnSystemProtectionRestoration(UIntPtr Address, SystemFailure Details)
    : WriteFailure(WriteFailureReason.SystemProtectionRestorationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The value was written successfully, but the protection of the memory at address {Address} could not be restored to its original value: {Details}.{Environment.NewLine}Change the memory protection strategy to {nameof(MemoryProtectionStrategy)}.{nameof(MemoryProtectionStrategy.Remove)} to prevent memory protection restoration.";
}

/// <summary>
/// Represents a failure in a memory write operation when the system API call to write bytes in memory fails.
/// </summary>
/// <param name="Address">Address where the write operation failed.</param>
/// <param name="Details">Details about the failure.</param>
public record WriteFailureOnSystemWrite(UIntPtr Address, SystemFailure Details)
    : WriteFailure(WriteFailureReason.SystemWriteFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to write at the address {Address}: {Details}";
}

/// <summary>
/// Represents a failure in a memory write operation when trying to convert the value to write to an array of bytes to
/// write in memory.
/// </summary>
/// <param name="Type">Type that caused the failure.</param>
/// <param name="ConversionException">Exception that occurred during the conversion.</param>
public record WriteFailureOnConversion(Type Type, Exception ConversionException)
    : WriteFailure(WriteFailureReason.ConversionFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"Failed to convert the value of type {Type} to an array of bytes. Make sure the type has a fixed length. See the ConversionException property for more details.";
}
