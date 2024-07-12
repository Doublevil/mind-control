using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>Provides methods to manipulate and track a specific value in memory.</summary>
/// <param name="memoryAdapter">Adapter that reads and writes the value from and to memory.</param>
/// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
/// <typeparam name="TValue">Type of the value to read and write.</typeparam>
/// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
/// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
public class ValueAnchor<TValue, TReadFailure, TWriteFailure>
    (IMemoryAdapter<TValue, TReadFailure, TWriteFailure> memoryAdapter, ProcessMemory processMemory)
    : IValueAnchor
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<TValue, TReadFailure> Read() => memoryAdapter.Read(processMemory);
    
    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result<TWriteFailure> Write(TValue value) => memoryAdapter.Write(processMemory, value);

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        processMemory.RemoveAnchor(this);
    }
}