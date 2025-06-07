namespace MindControl.Results;

/// <summary>
/// Represents a failure in a memory write operation when the value to write cannot be converted to an array of bytes.
/// </summary>
/// <param name="Type">Type that caused the failure.</param>
public record UnsupportedTypeWriteFailure(Type Type) : Failure($"The type {Type} is not supported for writing.")
{
    /// <summary>Type that caused the failure.</summary>
    public Type Type { get; init; } = Type;
}

/// <summary>
/// Represents a failure in a memory write operation when the target process is 32-bit, but the target memory address
/// is not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record IncompatibleBitnessWriteFailure(UIntPtr Address) : Failure($"The pointer to write, {Address}, is too large for a 32-bit process. If you want to write an 8-byte value and not a memory address, use a ulong instead.")
{
    /// <summary>Address that caused the failure.</summary>
    public UIntPtr Address { get; init; } = Address;
}

/// <summary>
/// Represents a failure in a memory write operation when the system API call to remove the protection properties of
/// the target memory space fails.
/// </summary>
/// <param name="Address">Address where the operation failed.</param>
/// <param name="Details">A description of the inner failure.</param>
public record MemoryProtectionRemovalFailure(UIntPtr Address, Failure Details)
    : Failure($"Failed to remove the memory protection at address {Address:X}: \"{Details}\".{Environment.NewLine}Change the memory protection strategy to {nameof(MemoryProtectionStrategy)}.{nameof(MemoryProtectionStrategy.Ignore)} to prevent memory protection removal. Because this is the first step when writing a value, this failure may also indicate that the target address is not within a valid memory range.")
{
    /// <summary>Address where the operation failed.</summary>
    public UIntPtr Address { get; init; } = Address;
    
    /// <summary>A description of the inner failure.</summary>
    public Failure Details { get; init; } = Details;
}

/// <summary>
/// Represents a failure in a memory write operation when the system API call to restore the protection properties of
/// the target memory space after writing fails.
/// </summary>
/// <param name="Address">Address where the operation failed.</param>
/// <param name="Details">Details about the failure.</param>
public record MemoryProtectionRestorationFailure(UIntPtr Address, Failure Details)
    : Failure($"The value was written successfully, but the memory protection at address {Address} could not be restored to its original value: {Details}.{Environment.NewLine}Change the memory protection strategy to {nameof(MemoryProtectionStrategy)}.{nameof(MemoryProtectionStrategy.Remove)} to skip memory protection restoration if you don't need it.")
{
    /// <summary>Address where the operation failed.</summary>
    public UIntPtr Address { get; init; } = Address;
    
    /// <summary>Details about the failure.</summary>
    public Failure Details { get; init; } = Details;
}

/// <summary>
/// Represents a failure in a memory write operation when trying to convert the value to write to an array of bytes to
/// write in memory.
/// </summary>
/// <param name="Type">Type that caused the failure.</param>
/// <param name="ConversionException">Exception that occurred during the conversion.</param>
public record ConversionWriteFailure(Type Type, Exception ConversionException)
    : Failure($"Failed to convert the value of type {Type} to an array of bytes. Make sure the type has a fixed length. See the ConversionException property for more details.")
{
    /// <summary>Type that caused the failure.</summary>
    public Type Type { get; init; } = Type;
}
