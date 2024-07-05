using Iced.Intel;

namespace MindControl.Results;

/// <summary>
/// Enumerates the possible reasons for a hook operation to fail.
/// </summary>
public enum HookFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>The given pointer path could not be successfully evaluated.</summary>
    PathEvaluationFailure,
    /// <summary>The target process is 32-bit, but the target memory address is not within the 32-bit address space.
    /// </summary>
    IncompatibleBitness,
    /// <summary>The target address is a zero pointer.</summary>
    ZeroPointer,
    /// <summary>The arguments provided to the hook operation are invalid.</summary>
    InvalidArguments,
    /// <summary>The memory allocation operation failed.</summary>
    AllocationFailure,
    /// <summary>A reading operation failed.</summary>
    ReadFailure,
    /// <summary>A code disassembling operation failed.</summary>
    DecodingFailure,
    /// <summary>Instructions could not be assembled into a code block.</summary>
    CodeAssemblyFailure,
    /// <summary>A write operation failed.</summary>
    WriteFailure
}

/// <summary>
/// Represents a failure that occurred in a hook operation.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record HookFailure(HookFailureReason Reason);

/// <summary>
/// Represents a failure that occurred in a hook operation when the target process is not attached.
/// </summary>
public record HookFailureOnDetachedProcess()
    : HookFailure(HookFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure that occurred in a hook operation when the pointer path failed to evaluate.
/// </summary>
/// <param name="Details">Details about the path evaluation failure.</param>
public record HookFailureOnPathEvaluation(PathEvaluationFailure Details)
    : HookFailure(HookFailureReason.PathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to evaluate the given pointer path: {Details}";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when the target process is 32-bit, but the target memory
/// address is not within the 32-bit address space.
/// </summary>
/// <param name="Address">Address that caused the failure.</param>
public record HookFailureOnIncompatibleBitness(UIntPtr Address)
    : HookFailure(HookFailureReason.IncompatibleBitness)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"The address to read, {Address}, is a 64-bit address, but the target process is 32-bit.";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when the target address is a zero pointer.
/// </summary>
public record HookFailureOnZeroPointer() : HookFailure(HookFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target address is a zero pointer.";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when the arguments provided are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record HookFailureOnInvalidArguments(string Message)
    : HookFailure(HookFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The arguments provided are invalid: {Message}";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when the memory allocation operation required to store the
/// injected code failed.
/// </summary>
/// <param name="Details">Details about the allocation failure.</param>
public record HookFailureOnAllocationFailure(AllocationFailure Details)
    : HookFailure(HookFailureReason.AllocationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to allocate memory for the injected code: {Details}";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when a reading operation failed.
/// </summary>
/// <param name="Details">Details about the read failure.</param>
public record HookFailureOnReadFailure(ReadFailure Details)
    : HookFailure(HookFailureReason.ReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"A reading operation failed: {Details}";
}

/// <summary>
/// Represents a failure that occurred in a hook operation when a code disassembling operation failed.
/// </summary>
/// <param name="Error">Error code that describes the failure.</param>
public record HookFailureOnDecodingFailure(DecoderError Error)
    : HookFailure(HookFailureReason.DecodingFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to decode instructions with the following error code: {Error}";
}

/// <summary>Enumerates potential blocks for a <see cref="HookFailureOnCodeAssembly"/>.</summary>
public enum HookCodeAssemblySource
{
    /// <summary>Default value used when the source is unknown.</summary>
    Unknown,
    /// <summary>Designates the jump instruction that forwards execution to the injected code.</summary>
    JumpToInjectedCode,
    /// <summary>Designates the code block that is prepended to the injected code.</summary>
    PrependedCode,
    /// <summary>Designates the injected code block itself.</summary>
    InjectedCode,
    /// <summary>Designates the code block that is appended to the injected code.</summary>
    AppendedCode
}

/// <summary>
/// Represents a failure that occurred in a hook operation when instructions could not be assembled into a code block.
/// </summary>
/// <param name="Source">Block where the code assembly failed.</param>
/// <param name="Message">Message that describes the failure.</param>
public record HookFailureOnCodeAssembly(HookCodeAssemblySource Source, string Message)
    : HookFailure(HookFailureReason.CodeAssemblyFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to assemble code in {GetSourceAsString(Source)}: {Message}";
    
    /// <summary>Returns a string representation of the given <see cref="HookCodeAssemblySource"/>.</summary>
    /// <param name="source">The source to convert to a string.</param>
    /// <returns>A string representation of the given <see cref="HookCodeAssemblySource"/>.</returns>
    public static string GetSourceAsString(HookCodeAssemblySource source) => source switch
    {
        HookCodeAssemblySource.JumpToInjectedCode => "the jump to the injected code",
        HookCodeAssemblySource.PrependedCode => "the code block generated before the injected code",
        HookCodeAssemblySource.InjectedCode => "the given code to inject",
        HookCodeAssemblySource.AppendedCode => "the code block generated after the injected code",
        _ => "an undetermined code block"
    };
}

/// <summary>
/// Represents a failure that occurred in a hook operation when a write operation failed.
/// </summary>
/// <param name="Details">Details about the write failure.</param>
public record HookFailureOnWriteFailure(WriteFailure Details)
    : HookFailure(HookFailureReason.WriteFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"A write operation failed: {Details}";
}
