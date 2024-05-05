namespace MindControl;

/// <summary>
/// Represents a range of memory that has been allocated in a process.
/// Can be used to safely manage data or code storage using reservations.
/// </summary>
public class MemoryAllocation
{
    /// <summary>
    /// Gets the memory range allocated.
    /// </summary>
    public MemoryRange Range { get; }
 
    private readonly List<MemoryReservation> _reservations = new();
    
    /// <summary>
    /// Gets the reservations managed by this allocation.
    /// </summary>
    public IReadOnlyList<MemoryReservation> Reservations => _reservations.AsReadOnly();
    
    /// <summary>
    /// Gets a value indicating if the memory range has been allocated with executable permissions.
    /// </summary>
    public bool IsExecutable { get; }
    
    /// <summary>
    /// Gets a boolean indicating if this instance has been disposed. If True, the instance is no longer usable.
    /// </summary>
    public bool IsDisposed { get; private set; }
    
    private readonly ProcessMemory? _parentProcessMemory;
    
    /// <summary>
    /// Builds a new <see cref="MemoryAllocation"/> instance.
    /// </summary>
    /// <param name="range">Memory range allocated.</param>
    /// <param name="isExecutable">Value indicating if the memory range has executable permissions.</param>
    /// <param name="parentProcessMemory">Instance of <see cref="ProcessMemory"/> handling this allocation.</param>
    internal MemoryAllocation(MemoryRange range, bool isExecutable, ProcessMemory parentProcessMemory)
    {
        Range = range;
        IsExecutable = isExecutable;
        _parentProcessMemory = parentProcessMemory;
    }
    
    /// <summary>
    /// Gets the total space reserved in the allocated range, in bytes.
    /// </summary>
    /// <returns>The total space reserved in the allocated range, in bytes.</returns>
    public ulong GetTotalReservedSpace()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(MemoryAllocation));
        
        return Reservations.Aggregate<MemoryReservation?, ulong>(0,
            (current, subRange) => current + subRange?.Range.GetSize() ?? 0);
    }

    /// <summary>
    /// Gets the total space available for reservation in the range, in bytes.
    /// Note that space might be fragmented and thus unavailable for a single reservation. Use
    /// <see cref="GetNextRangeFittingSize"/> if you want to make sure your data fits in the range.
    /// </summary>
    /// <returns>The total space available for reservation in the range.</returns>
    public ulong GetRemainingSpace()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(MemoryAllocation));
        
        return Range.GetSize() - GetTotalReservedSpace();
    }

    /// <summary>
    /// Gets the largest contiguous, unreserved space in the range. This is the largest space that can be reserved in a
    /// single allocation.
    /// </summary>
    /// <returns>The largest contiguous, unreserved space in the range.</returns>
    public MemoryRange? GetLargestReservableSpace()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(MemoryAllocation));
        
        return GetFreeRanges().OrderByDescending(r => r.GetSize())
            .Cast<MemoryRange?>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets a collection of all contiguous memory ranges that can be used for reservations.
    /// </summary>
    private IEnumerable<MemoryRange> GetFreeRanges()
    {
        // Take the whole range and exclude all reserved ranges from it.
        // This will split the range into all free ranges.
        var freeRanges = new List<MemoryRange> { Range };
        foreach (var reservedRange in _reservations)
        {
            freeRanges = freeRanges.SelectMany(r => r.Exclude(reservedRange.Range)).ToList();
        }

        return freeRanges;
    }
    
    /// <summary>
    /// Gets the first free range that can fit the specified size, with an optional alignment. Returns null if no range
    /// is large enough to fit the requested size with the specified alignment.
    /// Note that this method does not reserve the range. Most of the time, you should use <see cref="ReserveRange"/>
    /// instead of this method.
    /// </summary>
    /// <param name="size">Requested size of the range to find, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes. Alignment means the resulting range might be bigger than the <paramref name="size"/>,
    /// but will never make it smaller.</param>
    /// <returns>The first free range that can fit the specified size, or null if no range is large enough.</returns>
    public MemoryRange? GetNextRangeFittingSize(ulong size, uint? byteAlignment = 8)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(MemoryAllocation));
        
        // Adjust the size to fit the alignment
        if (byteAlignment != null && size % byteAlignment.Value > 0)
            size += byteAlignment.Value - size % byteAlignment.Value;
        
        // Get the first range that can fit the size, aligned if necessary
        var matchingRange = GetFreeRanges()
            .Select(r => byteAlignment == null ? r : r.AlignedTo(byteAlignment.Value))
            .Where(r => r.HasValue)
            .OrderBy(r => r!.Value.Start.ToUInt64())
            .FirstOrDefault(r => r?.GetSize() >= size);

        if (matchingRange == null)
            return null;
        
        // Adjust the range to fit the size exactly
        return new MemoryRange(matchingRange.Value.Start, (UIntPtr)(matchingRange.Value.Start.ToUInt64() + size - 1));
    }

    /// <summary>
    /// Reserves and returns the next available range with the specified size.
    /// Data stored in the returned range will not be overwritten by future reservations until the reservation is freed.
    /// This method throws if there is no contiguous memory large enough in the range. See <see cref="TryReserveRange"/>
    /// for a non-throwing alternative.
    /// </summary>
    /// <param name="size">Minimal size that the range to get must be able to accomodate, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes. Alignment means the actual reserved space might be bigger than the
    /// <paramref name="size"/>, but will never make it smaller.</param>
    /// <returns>The resulting reservation.</returns>
    public MemoryReservation ReserveRange(ulong size, uint? byteAlignment = 8)
        => TryReserveRange(size, byteAlignment) ?? throw new InsufficientAllocatedMemoryException(size, byteAlignment);
    
    /// <summary>
    /// Attempts to reserve and return the next available range with the specified size.
    /// Data stored in the returned range will not be overwritten by future reservations until the reservation is freed.
    /// This method returns null if there is no contiguous memory large enough in the range. See
    /// <see cref="ReserveRange"/> if you expect an exception to be thrown on a reservation failure.
    /// Note that this method will still throw if the range has been disposed.
    /// </summary>
    /// <param name="size">Minimal size that the range to get must be able to accomodate, in bytes.</param>
    /// <param name="byteAlignment">Optional byte alignment for the range. When null, values are not aligned. The
    /// default value is 8, meaning that for example a range of [0x15,0x3C] will be aligned to [0x18,0x38] and thus
    /// only accomodate 32 bytes. Alignment means the actual reserved space might be bigger than the
    /// <paramref name="size"/>, but will never make it smaller.</param>
    /// <returns>The resulting reservation if possible, null otherwise.</returns>
    public MemoryReservation? TryReserveRange(ulong size, uint? byteAlignment = 8)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(MemoryAllocation));
        
        var range = GetNextRangeFittingSize(size, byteAlignment);
        if (range == null)
            return null;
        
        var reservedRange = new MemoryReservation(range.Value, this);
        _reservations.Add(reservedRange);
        return reservedRange;
    }
    
    /// <summary>
    /// Makes reserved space overlapping the specified range available for future reservations. Affected reservations
    /// will be disposed, and may be either completely gone, reduced, or split into two ranges.
    /// Consider disposing reservations instead, unless you want more control over the range to free.
    /// </summary>
    /// <param name="rangeToFree">The range of memory to free from reservations in this instance.</param>
    public void FreeRange(MemoryRange rangeToFree)
    {
        for (int i = _reservations.Count - 1; i >= 0; i--)
        {
            var currentRange = _reservations[i];
            var resultingRanges = currentRange.Range.Exclude(rangeToFree).ToArray();
            
            // Reservation is completely gone
            if (resultingRanges.Length == 0)
                currentRange.Dispose();
            
            // Reservation is reduced
            else if (resultingRanges.Length == 1 && resultingRanges[0] != currentRange.Range)
            {
                _reservations[i] = new MemoryReservation(resultingRanges[0], this);
                currentRange.Dispose();
            }
            
            // Reservation is split into two ranges
            else if (resultingRanges.Length == 2)
            {
                _reservations[i] = new MemoryReservation(resultingRanges[0], this);
                _reservations.Insert(i + 1, new MemoryReservation(resultingRanges[1], this));
                currentRange.Dispose();
            }
        }
    }

    /// <summary>
    /// Frees up the given reservation.
    /// </summary>
    /// <param name="reservation">Reservation to free up.</param>
    /// <remarks>This method is internal, as it is intended to be called by the <see cref="MemoryReservation"/> itself
    /// when disposing.</remarks>
    internal void FreeReservation(MemoryReservation reservation) => _reservations.Remove(reservation);

    /// <summary>
    /// Removes all reservations from the range, meaning the whole allocated range will be available for reservations.
    /// </summary>
    public void ClearReservations()
    {
        if (IsDisposed)
            return;
        
        for (int i = _reservations.Count - 1; i >= 0; i--)
            _reservations[i].Dispose();

        _reservations.Clear();
    }

    /// <summary>
    /// Releases all reservations, and frees up the allocated space. After this method is called, this instance will be
    /// made unusable, and you may no longer be able to read or write memory in this range.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        ClearReservations();
        
        _parentProcessMemory?.Free(this);
    }
}