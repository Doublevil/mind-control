namespace MindControl;

/// <summary>
/// Represents a range of allocated memory in a process.
/// </summary>
public class AllocatedRange : IDisposable
{
    /// <summary>
    /// Gets the memory range of this allocated range.
    /// </summary>
    public MemoryRange Range { get; }
 
    private readonly List<AllocatedRange> _reservedRanges = new();
    
    /// <summary>
    /// Gets the reserved ranges within this range.
    /// They are owned by this instance. Do not dispose them manually.
    /// </summary>
    public IReadOnlyList<AllocatedRange> ReservedRanges => _reservedRanges.AsReadOnly();
    
    /// <summary>
    /// Gets a value indicating if the memory range has executable permissions.
    /// </summary>
    public bool IsExecutable { get; }
    
    /// <summary>
    /// Gets the range containing this range, if any.
    /// </summary>
    public AllocatedRange? ParentRange { get; }
    
    /// <summary>
    /// Gets a boolean indicating if the range is in use. If false, the range has been freed and is no longer usable.
    /// </summary>
    public bool IsReserved => !_hasBeenFreed;
    
    private readonly ProcessMemory? _parentProcessMemory;

    private bool _hasBeenFreed;
    
    /// <summary>
    /// Builds a new <see cref="AllocatedRange"/> instance with a parent range.
    /// </summary>
    /// <param name="range">Memory range of the allocated range.</param>
    /// <param name="isExecutable">Value indicating if the memory range has executable permissions.</param>
    /// <param name="parentRange">Parent range containing this range.</param>
    internal AllocatedRange(MemoryRange range, bool isExecutable, AllocatedRange parentRange)
    {
        Range = range;
        IsExecutable = isExecutable;
        ParentRange = parentRange;
    }
    
    /// <summary>
    /// Builds a new top-level <see cref="AllocatedRange"/> instance with a parent process memory.
    /// </summary>
    /// <param name="range">Memory range of the allocated range.</param>
    /// <param name="isExecutable">Value indicating if the memory range has executable permissions.</param>
    /// <param name="parentProcessMemory">Parent process memory instance handling this range.</param>
    internal AllocatedRange(MemoryRange range, bool isExecutable, ProcessMemory parentProcessMemory)
    {
        Range = range;
        IsExecutable = isExecutable;
        _parentProcessMemory = parentProcessMemory;
    }
    
    /// <summary>
    /// Gets the total space reserved in the range, in bytes.
    /// </summary>
    /// <returns>The total space reserved in the range.</returns>
    public ulong GetTotalReservedSpace()
    {
        if (_hasBeenFreed)
            throw new ObjectDisposedException(nameof(AllocatedRange));
        
        return ReservedRanges.Aggregate<AllocatedRange?, ulong>(0,
            (current, subRange) => current + subRange?.Range.GetSize() ?? 0);
    }

    /// <summary>
    /// Gets the total space remaining in the range, in bytes.
    /// Note that this does not mean the range is contiguous. Space might be fragmented and thus unable to be
    /// reserved entirely in a single allocation.
    /// </summary>
    /// <returns>The total space remaining in the range.</returns>
    public ulong GetRemainingSpace()
    {
        if (_hasBeenFreed)
            throw new ObjectDisposedException(nameof(AllocatedRange));
        
        return Range.GetSize() - GetTotalReservedSpace();
    }

    /// <summary>
    /// Gets the largest contiguous, unreserved space in the range. This is the largest space that can be reserved in a
    /// single allocation.
    /// </summary>
    /// <returns>The largest contiguous, unreserved space in the range.</returns>
    public MemoryRange? GetLargestReservableSpace()
    {
        if (_hasBeenFreed)
            throw new ObjectDisposedException(nameof(AllocatedRange));
        
        return GetFreeRanges().OrderByDescending(r => r.GetSize()).FirstOrDefault();
    }

    /// <summary>
    /// Gets a collection of all contiguous memory ranges that can be used for reservations.
    /// </summary>
    private IEnumerable<MemoryRange> GetFreeRanges()
    {
        // Take the whole range and exclude all reserved ranges from it.
        // This will split the range into all free ranges.
        var freeRanges = new List<MemoryRange> { Range };
        foreach (var reservedRange in _reservedRanges)
        {
            freeRanges = freeRanges.SelectMany(r => r.Exclude(reservedRange.Range)).ToList();
        }

        return freeRanges;
    }
    
    /// <summary>
    /// Gets the first free range that can fit the specified size, with an optional alignment, or null if no range is
    /// large enough to fit the requested size with the specified alignment.
    /// Note that this method does not reserve the range. Most of the time, you should use <see cref="ReserveRange"/>
    /// instead of this method.
    /// </summary>
    /// <param name="size">Requested size of the range to find, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes.</param>
    /// <returns>The first free range that can fit the specified size, or null if no range is large enough.</returns>
    public MemoryRange? GetNextRangeFittingSize(ulong size, uint? byteAlignment = 8)
    {
        if (_hasBeenFreed)
            throw new ObjectDisposedException(nameof(AllocatedRange));
        
        var matchingRange = GetFreeRanges()
            .Select(r => byteAlignment == null ? r : r.AlignedTo(byteAlignment.Value))
            .Where(r => r.HasValue)
            .OrderBy(r => r!.Value.Start.ToUInt64())
            .FirstOrDefault(r => r?.GetSize() >= size);

        if (matchingRange == null)
            return null;
        
        // Adjust the end address to fit no more than the requested size
        return matchingRange.Value with { End = (UIntPtr)(matchingRange.Value.Start.ToUInt64() + size - 1) };
    }

    /// <summary>
    /// Reserves and returns the next available range with the specified size.
    /// Data stored in the returned range will not be overwritten by future reservations until the range is freed.
    /// This method throws if there is no contiguous memory large enough in the range. See <see cref="TryReserveRange"/>
    /// for a non-throwing alternative. 
    /// </summary>
    /// <param name="size">Minimal size that the range to get must be able to accomodate, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes.</param>
    /// <returns>The reserved range.</returns>
    public AllocatedRange ReserveRange(ulong size, uint? byteAlignment = 8)
        => TryReserveRange(size, byteAlignment) ?? throw new InsufficientAllocatedMemoryException(size, byteAlignment);
    
    /// <summary>
    /// Attempts to reserve and return the next available range with the specified size.
    /// Data stored in the returned range will not be overwritten by future reservations until the range is freed.
    /// This method returns null if there is no contiguous memory large enough in the range. See
    /// <see cref="ReserveRange"/> if you expect an exception to be thrown on a reservation failure.
    /// Note that this method will still throw if the range has been freed.
    /// </summary>
    /// <param name="size">Minimal size that the range to get must be able to accomodate, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes.</param>
    /// <returns>The reserved range.</returns>
    public AllocatedRange? TryReserveRange(ulong size, uint? byteAlignment = 8)
    {
        if (_hasBeenFreed)
            throw new ObjectDisposedException(nameof(AllocatedRange));
        
        var range = GetNextRangeFittingSize(size, byteAlignment);
        if (range == null)
            return null;
        
        var reservedRange = new AllocatedRange(range.Value, IsExecutable, this);
        _reservedRanges.Add(reservedRange);
        return reservedRange;
    }
    
    /// <summary>
    /// Makes reserved space matching the specified range available for future reservations. Affected reserved ranges
    /// will be disposed, and may be either completely gone, reduced, or split into two ranges.
    /// Consider disposing reservations instead, unless you want more control over the range to free.
    /// </summary>
    /// <param name="rangeToFree">The range of memory to free from reservations in this instance.</param>
    public void FreeRange(MemoryRange rangeToFree)
    {
        for (int i = _reservedRanges.Count - 1; i >= 0; i--)
        {
            var currentRange = _reservedRanges[i];
            var resultingRanges = currentRange.Range.Exclude(rangeToFree).ToArray();
            
            // Reservation is completely gone
            if (resultingRanges.Length == 0)
                currentRange.Dispose();
            
            // Reservation is reduced
            else if (resultingRanges.Length == 1 && resultingRanges[0] != currentRange.Range)
            {
                currentRange.Dispose();
                _reservedRanges[i] = new AllocatedRange(resultingRanges[0], IsExecutable, this);
            }
            
            // Reservation is split into two ranges
            else if (resultingRanges.Length == 2)
            {
                currentRange.Dispose();
                _reservedRanges[i] = new AllocatedRange(resultingRanges[0], IsExecutable, this);
                _reservedRanges.Insert(i + 1, new AllocatedRange(resultingRanges[1], IsExecutable, this));
            }
        }
    }

    /// <summary>
    /// Removes all reserved ranges from the range, meaning the whole range will be available for reservations.
    /// After using this method, data stored in this range may be overwritten by future reservations.
    /// </summary>
    public void Clear()
    {
        if (_hasBeenFreed)
            return;
        
        foreach (var subRange in _reservedRanges)
            subRange.Dispose();
        
        _reservedRanges.Clear();
    }

    /// <summary>
    /// Releases all reservations used by the <see cref="AllocatedRange"/>, and makes this instance unusable. 
    /// If this instance is a top-level range, the process memory will be freed as well.
    /// </summary>
    public void Dispose()
    {
        if (_hasBeenFreed)
            return;

        _hasBeenFreed = true;
        Clear();
        
        if (ParentRange != null)
            ParentRange._reservedRanges.Remove(this);
        else
            _parentProcessMemory?.Free(this);
    }
}