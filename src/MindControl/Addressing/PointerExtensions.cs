namespace MindControl;

/// <summary>
/// Provides extension methods for pointers.
/// </summary>
public static class PointerExtensions
{
    /// <summary>
    /// Determines the distance between two pointers, without wrap-around.
    /// </summary>
    /// <param name="a">First pointer.</param>
    /// <param name="b">Second pointer.</param>
    /// <returns>The distance between the two pointers, without wrap-around.</returns>
    /// <remarks>
    /// As remarked by the documentation, the distance provided does not wrap around, meaning that, for example, the
    /// distance between 0 and <see cref="UIntPtr.MaxValue"/> is equal to <see cref="UIntPtr.MaxValue"/> and not 1.
    /// </remarks>
    public static ulong DistanceTo(this UIntPtr a, UIntPtr b)
    {
        ulong aValue = a.ToUInt64();
        ulong bValue = b.ToUInt64();
        return aValue < bValue ? bValue - aValue : aValue - bValue;
    }

    /// <summary>
    /// Gets a range of memory around the given address, with the specified size and without wrap-around.
    /// </summary>
    /// <param name="address">Target address.</param>
    /// <param name="size">Size of the range. Note that the resulting range may be smaller if the address is near the
    /// beginning or end of the address space.</param>
    /// <returns>A memory range around the address, with the specified size and without wrap-around.</returns>
    public static MemoryRange GetRangeAround(this UIntPtr address, ulong size)
    {
        if (size < 2)
            throw new ArgumentException("The size must be at least 2 bytes.", nameof(size));
        
        ulong addressValue = address.ToUInt64();
        ulong halfSize = size / 2;
        ulong start = addressValue <= halfSize ? 0 : addressValue - halfSize;
        ulong end = ulong.MaxValue - addressValue <= halfSize ? ulong.MaxValue : addressValue + halfSize;
        return new MemoryRange(new UIntPtr(start), new UIntPtr(end));
    }
}