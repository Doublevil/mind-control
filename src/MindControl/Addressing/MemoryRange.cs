namespace MindControl;

/// <summary>
/// Represents a range of memory addresses in a process.
/// </summary>
public readonly record struct MemoryRange
{
    /// <summary>Start address of the range.</summary>
    public UIntPtr Start { get; init; }

    /// <summary>End address of the range.</summary>
    public UIntPtr End { get; init; }
    
    /// <summary>
    /// Builds a <see cref="MemoryRange"/>.
    /// </summary>
    /// <param name="Start">Start address of the range.</param>
    /// <param name="End">End address of the range.</param>
    public MemoryRange(UIntPtr Start, UIntPtr End)
    {
        this.Start = Start;
        this.End = End;
        
        if (Start.ToUInt64() > End.ToUInt64())
            throw new ArgumentException($"The start address of the memory range cannot be greater than the end address. If you are trying to build a range from a start address and a size, use the {nameof(FromStartAndSize)} static method instead.");
    }

    /// <summary>
    /// Creates a new memory range from a start address and a size.
    /// </summary>
    /// <param name="start">Start address of the range.</param>
    /// <param name="size">Size of the range in bytes.</param>
    public static MemoryRange FromStartAndSize(UIntPtr start, ulong size)
    {
        if (size == 0)
            throw new ArgumentException("The size of the memory range cannot be zero.", nameof(size));
        
        return new MemoryRange(start, (UIntPtr)(start.ToUInt64() + size - 1));
    }

    /// <summary>
    /// Determines if the specified address is within the memory range.
    /// </summary>
    /// <param name="address">Address to check.</param>
    /// <returns>True if the address is within the memory range, false otherwise.</returns>
    public bool IsInRange(UIntPtr address)
        => address.ToUInt64() >= Start.ToUInt64() && address.ToUInt64() <= End.ToUInt64();

    /// <summary>
    /// Determines if the specified range is entirely contained within this range.
    /// </summary>
    /// <param name="range">Range to check.</param>
    /// <returns>True if the range is entirely contained within this range, false otherwise.</returns>
    public bool Contains(MemoryRange range)
        => range.Start.ToUInt64() >= Start.ToUInt64() && range.End.ToUInt64() <= End.ToUInt64();
    
    /// <summary>
    /// Determines if the specified range overlaps with this range.
    /// </summary>
    /// <param name="range">Range to check.</param>
    /// <returns>True if the range overlaps with this range, false otherwise.</returns>
    public bool Overlaps(MemoryRange range)
        => Start.ToUInt64() <= range.End.ToUInt64() && range.Start.ToUInt64() <= End.ToUInt64();

    /// <summary>
    /// Obtains the intersection of this range with the given range, which is the range that is common to both.
    /// </summary>
    /// <param name="otherRange">The range to intersect with.</param>
    /// <returns>The intersection of the two ranges, or null if there is no intersection.</returns>
    public MemoryRange? Intersect(MemoryRange otherRange)
    {
        ulong start = Math.Max(Start.ToUInt64(), otherRange.Start.ToUInt64());
        ulong end = Math.Min(End.ToUInt64(), otherRange.End.ToUInt64());
        return start <= end ? new MemoryRange((UIntPtr)start, (UIntPtr)end) : null;
    }
    
    /// <summary>
    /// Returns the result of excluding the given range from this range. This can be seen as an XOR operation between
    /// the two ranges.
    /// This will result in no range when this range is entirely contained within the given range, two ranges when
    /// the given range is in the middle of this range, or in other cases a single range that will be reduced by the
    /// overlapping part (if any).
    /// </summary>
    /// <param name="rangeToExclude">The range to subtract from this range.</param>
    /// <returns>The resulting ranges. The resulting collection may be empty.</returns>
    public IEnumerable<MemoryRange> Exclude(MemoryRange rangeToExclude)
    {
        // This range is entirely contained in the range to subtract: no range left
        if (rangeToExclude.Contains(this))
            return Array.Empty<MemoryRange>();

        // No overlap between the two ranges: the original range is returned, untouched
        if (!rangeToExclude.Overlaps(this))
            return new[] { this };
        
        // There is an overlap: either one or two ranges will be returned, depending on the overlap
        var results = new List<MemoryRange>();
        if (rangeToExclude.Start.ToUInt64() > Start.ToUInt64())
            results.Add(new MemoryRange(Start, rangeToExclude.Start - 1));
        
        if (rangeToExclude.End.ToUInt64() < End.ToUInt64())
            results.Add(new MemoryRange(rangeToExclude.End + 1, End));

        return results;
    }
    
    /// <summary>
    /// Returns a subset of this range, aligned to the specified byte alignment.
    /// For example, a range of [2,9] aligned to 4 bytes will result in [4,8].
    /// </summary>
    /// <param name="alignment">Alignment in bytes. Usually 4 for 32-bits processes, or 8 for 64-bits processes.</param>
    /// <param name="alignmentMode">Alignment mode. Defines how the range should be aligned. Defaults to
    /// <see cref="RangeAlignmentMode.AlignBlock"/>.</param>
    /// <returns>The aligned memory range. The returned range is always a subset of the range, or the range itself.
    /// If the aligned memory range cannot fit in the original range, returns null.</returns>
    public MemoryRange? AlignedTo(uint alignment, RangeAlignmentMode alignmentMode = RangeAlignmentMode.AlignBlock)
    {
        if (alignment == 0)
            throw new ArgumentException("The alignment value cannot be zero.", nameof(alignment));
        
        if (alignmentMode == RangeAlignmentMode.None || alignment == 1)
            return this;
        bool alignSize = alignmentMode == RangeAlignmentMode.AlignBlock;
        
        var start = Start.ToUInt64();
        ulong alignedStart = start + (alignment - start % alignment) % alignment;
        
        ulong size = End.ToUInt64() - alignedStart + 1;
        ulong alignedSize = alignSize ? size - size % alignment : size;
        
        ulong end = alignedStart + alignedSize - 1;
        if (alignedStart > End.ToUInt64())
            return null;
        
        return new MemoryRange((UIntPtr)alignedStart, (UIntPtr)end);
    }

    /// <summary>
    /// Returns the size of the memory range.
    /// </summary>
    public ulong GetSize() => End.ToUInt64() - Start.ToUInt64() + 1;

    /// <summary>
    /// Deconstructs the memory range into its start and end addresses.
    /// </summary>
    /// <param name="start">Start address of the range.</param>
    /// <param name="end">End address of the range.</param>
    public void Deconstruct(out UIntPtr start, out UIntPtr end)
    {
        start = Start;
        end = End;
    }

    /// <summary>Returns the fully qualified type name of this instance.</summary>
    /// <returns>The fully qualified type name.</returns>
    public override string ToString() => $"[{Start:X}, {End:X}]";
}

/// <summary>
/// Defines a byte alignment mode for memory ranges.
/// </summary>
public enum RangeAlignmentMode
{
    /// <summary>Do not align the range.</summary>
    None,
    
    /// <summary>Align the start of the range, but not the size.</summary>
    AlignStart,
    
    /// <summary>Align both the start and the size of the range.</summary>
    AlignBlock
}
