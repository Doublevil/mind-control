using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>
/// Represents an adapter for reading and writing a value from and to memory.
/// This implementation reads and writes a byte array from and to memory using an address resolver and a size.
/// </summary>
public class ByteArrayMemoryAdapter : IMemoryAdapter<byte[]>
{
    private readonly IAddressResolver _addressResolver;
    private readonly int _size;

    /// <summary>
    /// Represents an adapter for reading and writing a value from and to memory.
    /// This implementation reads and writes a byte array from and to memory using an address resolver and a size.
    /// </summary>
    /// <param name="addressResolver">Resolver that provides the address of the array in memory.</param>
    /// <param name="size">Size of the byte array to read and write.</param>
    public ByteArrayMemoryAdapter(IAddressResolver addressResolver, int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "The size must be greater than zero.");
        
        _addressResolver = addressResolver;
        _size = size;
    }

    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<byte[]> Read(ProcessMemory processMemory)
    {
        var addressResult = _addressResolver.ResolveFor(processMemory);
        if (addressResult.IsFailure)
            return addressResult.Failure;

        return processMemory.ReadBytes(addressResult.Value, _size);
    }

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Write(ProcessMemory processMemory, byte[] value)
    {
        if (value.Length != _size)
            return new InvalidArgumentFailure(nameof(value),
                $"The size of the byte array does not match the expected size of {_size}.");

        var addressResult = _addressResolver.ResolveFor(processMemory);
        if (addressResult.IsFailure)
            return addressResult.Failure;

        return processMemory.WriteBytes(addressResult.Value, value);
    }
}