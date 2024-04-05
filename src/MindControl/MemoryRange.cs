namespace MindControl;

/// <summary>
/// Represents a range of memory addresses in a process.
/// </summary>
/// <param name="Start">Start address of the range.</param>
/// <param name="End">End address of the range.</param>
public readonly record struct MemoryRange(UIntPtr Start, UIntPtr End)
{
    /// <summary>
    /// Creates a new memory range from a start address and a size.
    /// </summary>
    /// <param name="start">Start address of the range.</param>
    /// <param name="size">Size of the range in bytes.</param>
    public static MemoryRange FromStartAndSize(UIntPtr start, ulong size)
        => new(start, (UIntPtr)(start.ToUInt64() + size));

    /// <summary>
    /// Determines if the specified address is within the memory range.
    /// </summary>
    /// <param name="address">Address to check.</param>
    /// <returns>True if the address is within the memory range, false otherwise.</returns>
    public bool IsInRange(UIntPtr address)
        => address.ToUInt64() >= Start.ToUInt64() && address.ToUInt64() <= End.ToUInt64();
    
    /// <summary>
    /// Throws an exception if the memory range is invalid.
    /// </summary>
    public void Validate()
    {
        if (Start.ToUInt64() >= End.ToUInt64())
            throw new ArgumentException("The start address of the memory range cannot be greater than the end address.");
    }

    /// <summary>
    /// Returns the size of the memory range.
    /// </summary>
    public ulong GetSize() => End.ToUInt64() - Start.ToUInt64() + 1;
}