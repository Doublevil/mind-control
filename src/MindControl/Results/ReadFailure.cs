namespace MindControl.Results;

/// <summary>
/// Represents a failure in a memory read operation when the target process is 32-bit, but the target memory address is
/// not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record IncompatibleBitnessPointerFailure(UIntPtr Address)
    : Failure($"The address to read, {Address:X}, is a 64-bit address, but the target process is 32-bit.")
{
    /// <summary>Address that caused the failure.</summary>
    public UIntPtr Address { get; init; } = Address;
}

/// <summary>
/// Represents a failure in a memory read operation when trying to convert the bytes read from memory to the target
/// type.
/// </summary>
public record ConversionFailure()
    : Failure("Failed to convert the bytes read from memory to the target type. Check that the target type does not contain reference types or pointers.");

/// <summary>Represents a failure in a memory read operation when the type to read is not supported.</summary>
/// <param name="ProvidedType">Type that caused the failure.</param>
public record UnsupportedTypeReadFailure(Type ProvidedType)
    : Failure($"The type {ProvidedType} is not supported. Reference types are not supported.")
{
    /// <summary>Type that caused the failure.</summary>
    public Type ProvidedType { get; init; } = ProvidedType;
}