using Iced.Intel;

namespace MindControl.Results;

/// <summary>
/// Enumerates the possible reasons for a code writing failure.
/// </summary>
public enum CodeWritingFailureReason
{
    /// <summary>The target process is not attached.</summary>
    DetachedProcess,
    /// <summary>The given pointer path could not be successfully evaluated.</summary>
    PathEvaluationFailure,
    /// <summary>The arguments provided to the code write operation are invalid.</summary>
    InvalidArguments,
    /// <summary>The target address is a zero pointer.</summary>
    ZeroPointer,
    /// <summary>A reading operation failed.</summary>
    ReadFailure,
    /// <summary>A code disassembling operation failed.</summary>
    DecodingFailure,
    /// <summary>A write operation failed.</summary>
    WriteFailure
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process.
/// </summary>
/// <param name="Reason">Reason for the failure.</param>
public abstract record CodeWritingFailure(CodeWritingFailureReason Reason);

/// <summary>
/// Represents a failure that occurred while writing code to a target process when the target process is not attached.
/// </summary>
public record CodeWritingFailureOnDetachedProcess()
    : CodeWritingFailure(CodeWritingFailureReason.DetachedProcess)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Failure.DetachedErrorMessage;
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when the pointer path failed to evaluate.
/// </summary>
/// <param name="Details">Details about the path evaluation failure.</param>
public record CodeWritingFailureOnPathEvaluation(PathEvaluationFailure Details)
    : CodeWritingFailure(CodeWritingFailureReason.PathEvaluationFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to evaluate the given pointer path: {Details}";
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when the arguments provided are invalid.
/// </summary>
/// <param name="Message">Message that describes how the arguments fail to meet expectations.</param>
public record CodeWritingFailureOnInvalidArguments(string Message)
    : CodeWritingFailure(CodeWritingFailureReason.InvalidArguments)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"The provided arguments are invalid: {Message}";
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when the target address is a zero pointer.
/// </summary>
public record CodeWritingFailureOnZeroPointer()
    : CodeWritingFailure(CodeWritingFailureReason.ZeroPointer)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "The target address is a zero pointer.";
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when a read operation failed.
/// </summary>
/// <param name="Details">Details about the read failure.</param>
public record CodeWritingFailureOnReadFailure(ReadFailure Details)
    : CodeWritingFailure(CodeWritingFailureReason.ReadFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to read code from the target address: {Details}";
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when a disassembling operation failed.
/// </summary>
/// <param name="Error">Error code that describes the failure.</param>
public record CodeWritingFailureOnDecoding(DecoderError Error)
    : CodeWritingFailure(CodeWritingFailureReason.DecodingFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to decode the instruction with the following error code: {Error}";
}

/// <summary>
/// Represents a failure that occurred while writing code to a target process when a write operation failed.
/// </summary>
/// <param name="Details">Details about the write failure.</param>
public record CodeWritingFailureOnWriteFailure(WriteFailure Details)
    : CodeWritingFailure(CodeWritingFailureReason.WriteFailure)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"Failed to write code to the target address: {Details}";
}
