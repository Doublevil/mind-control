using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>Provides methods to manipulate and track a specific value in memory.</summary>
public interface IValueAnchor<TValue, TReadFailure, TWriteFailure> : IDisposable
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    Result<TValue, ValueAnchorFailure> Read();

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result<ValueAnchorFailure> Write(TValue value);
}