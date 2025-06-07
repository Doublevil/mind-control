using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>
/// Represents an adapter for reading and writing a value from and to memory.
/// This implementation reads and writes any value type from and to memory using an address resolver.
/// </summary>
/// <param name="addressResolver">Resolver that provides the address of the value in memory.</param>
/// <typeparam name="TValue">Type of the value to read and write.</typeparam>
public class GenericMemoryAdapter<TValue>(IAddressResolver addressResolver)
    : IMemoryAdapter<TValue> where TValue : struct
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<TValue> Read(ProcessMemory processMemory)
    {
        var addressResult = addressResolver.ResolveFor(processMemory);
        if (addressResult.IsFailure)
            return addressResult.Failure;
        
        return processMemory.Read<TValue>(addressResult.Value);
    }

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Write(ProcessMemory processMemory, TValue value)
    {
        var addressResult = addressResolver.ResolveFor(processMemory);
        if (addressResult.IsFailure)
            return addressResult.Failure;
        
        return processMemory.Write(addressResult.Value, value, MemoryProtectionStrategy.Ignore);
    }
}