using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>
/// Represents an adapter for reading and writing a value from and to memory.
/// </summary>
/// <typeparam name="TValue">Type of the value to read and write.</typeparam>
public interface IMemoryAdapter<TValue>
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    Result<TValue> Read(ProcessMemory processMemory);
    
    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result Write(ProcessMemory processMemory, TValue value);
}