using Iced.Intel;

namespace MindControl.Results;

/// <summary>
/// Represents a failure that occurred while writing code to a target process when a disassembling operation failed.
/// </summary>
/// <param name="Error">Error code that describes the failure.</param>
public record CodeDecodingFailure(DecoderError Error)
    : Failure($"Failed to decode the instruction with the following error code: {Error}. Check that the provided address points to the start of a valid instruction.")
{
    /// <summary>Error code that describes the failure.</summary>
    public DecoderError Error { get; init; } = Error;
}
