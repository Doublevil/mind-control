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
        
        if (Start.ToUInt64() < End.ToUInt64())
            throw new ArgumentException($"The start address of the memory range cannot be greater than the end address. If you are trying to build a range from a start address and a size, use the {nameof(FromStartAndSize)} static method instead.");
    }

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
    /// <param name="alignStart">Indicates if the start of the range should be aligned. Defaults to true.</param>
    /// <param name="alignEnd">Indicates if the end of the range should be aligned. Defaults to true.</param>
    /// <returns>The aligned memory range. The returned range is always a subset of the range, or the range itself.
    /// </returns>
    public MemoryRange AlignedTo(uint alignment, bool alignStart = true, bool alignEnd = true)
    {
        if (!alignStart && !alignEnd)
            return this;
        
        var start = Start.ToUInt64();
        var end = End.ToUInt64();
        
        ulong alignedStart = alignStart ? start + (alignment - start % alignment) % alignment : start;
        ulong alignedEnd = alignEnd ? end - end % alignment : end;

        return new MemoryRange((UIntPtr)alignedStart, (UIntPtr)alignedEnd);
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
}