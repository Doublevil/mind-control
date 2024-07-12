namespace MindControl.Results;

/// <summary>Represents a failure in a memory read operation.</summary>
public abstract record ReadFailure;

/// <summary>Represents a failure in a memory read operation when the target process is not attached.</summary>
public record ReadFailureOnDetachedProcess : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>Represents a failure in a memory read operation when the arguments provided are invalid.</summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record ReadFailureOnInvalidArguments(string Message) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>Represents a failure in a memory read operation when resolving the address in the target process.</summary>
/// <param name="Details">Details about the failure.</param>
/// <typeparam name="T">Type of the underlying failure.</typeparam>
public record ReadFailureOnAddressResolution<T>(T Details) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to resolve the address: {Details}";
}

/// <summary>
/// Represents a failure in a memory read operation when evaluating the pointer path to the target memory.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record ReadFailureOnPointerPathEvaluation(PathEvaluationFailure Details) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to evaluate the specified pointer path: {Details}";
}

/// <summary>
/// Represents a failure in a memory read operation when the target process is 32-bit, but the target memory address is
/// not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record ReadFailureOnIncompatibleBitness(UIntPtr Address) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The address to read, {Address}, is a 64-bit address, but the target process is 32-bit.";
}

/// <summary>Represents a failure in a memory read operation when the target pointer is a zero pointer.</summary>
public record ReadFailureOnZeroPointer : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The address to read is a zero pointer.";
}

/// <summary>
/// Represents a failure in a memory read operation when invoking the system API to read the target memory.
/// </summary>
/// <param name="Details">Details about the failure.</param>
public record ReadFailureOnSystemRead(SystemFailure Details) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to read at the target address: {Details}";
}

/// <summary>
/// Represents a failure in a memory read operation when trying to convert the bytes read from memory to the target
/// type.
/// </summary>
public record ReadFailureOnConversionFailure : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => "Failed to convert the bytes read from memory to the target type. Check that the type does not contain references or pointers.";
}

/// <summary>Represents a failure in a memory read operation when the type to read is not supported.</summary>
/// <param name="ProvidedType">Type that caused the failure.</param>
public record ReadFailureOnUnsupportedType(Type ProvidedType) : ReadFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The type {ProvidedType} is not supported. Reference types are not supported.";
}