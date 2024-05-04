using MindControl.Internal;
using MindControl.Native;

namespace MindControl;

// This partial class implements methods related to memory allocation.
public partial class ProcessMemory
{
    private readonly List<AllocatedRange> _allocatedRanges = new();
    
    /// <summary>
    /// Gets the ranges allocated in this process.
    /// Dispose a range to free the memory and remove it from this list.
    /// </summary>
    public IReadOnlyList<AllocatedRange> AllocatedRanges => _allocatedRanges.AsReadOnly();

    /// <summary>
    /// Attempts to allocate a memory range of the given size within the process.
    /// </summary>
    /// <param name="size">Size of the memory range to allocate.</param>
    /// <param name="forExecutableCode">Determines if the memory range can be used to store executable code.</param>
    /// <param name="limitRange">Specify this parameter to limit the allocation to a specific range of memory.</param>
    /// <returns>The allocated memory range.</returns>
    public AllocatedRange Allocate(ulong size, bool forExecutableCode, MemoryRange? limitRange = null)
    {
        if (size == 0)
            throw new ArgumentException("The size of the memory range to allocate must be greater than zero.",
                nameof(size));
        
        // Find a free memory range that satisfies the size needed
        var range = FindAndAllocateFreeMemory(size, forExecutableCode, limitRange);
        if (range == null)
            throw new InvalidOperationException("No suitable free memory range was found.");
        
        // Add the range to the list of allocated ranges and return it
        var allocatedRange = new AllocatedRange(range.Value, forExecutableCode, this);
        _allocatedRanges.Add(allocatedRange);
        return allocatedRange;
    }
    
    /// <summary>
    /// Frees the memory allocated for the given range.
    /// </summary>
    /// <param name="range">Range to free.</param>
    /// <remarks>This method is internal because it is designed to be called by <see cref="AllocatedRange.Dispose"/>.
    /// Users would release memory by disposing ranges.</remarks>
    internal void Free(AllocatedRange range)
    {
        if (!_allocatedRanges.Contains(range))
            return;
        
        _allocatedRanges.Remove(range);
        _osService.ReleaseMemory(_processHandle, range.Range.Start);
    }

    /// <summary>
    /// Finds a free memory range that satisfies the size needed and performs the allocation.
    /// </summary>
    /// <param name="sizeNeeded">Size of the memory range needed.</param>
    /// <param name="forExecutableCode">Set to true to allocate a memory block with execute permissions.</param>
    /// <param name="limitRange">Specify this parameter to limit the search to a specific range of memory.
    /// If left null (default), the entire process memory will be searched.</param>
    /// <returns>The start address of the free memory range, or null if no suitable range was found.</returns>
    private MemoryRange? FindAndAllocateFreeMemory(ulong sizeNeeded, bool forExecutableCode,
        MemoryRange? limitRange = null)
    {
        var maxRange = _osService.GetFullMemoryRange();
        var actualRange = limitRange == null ? maxRange : maxRange.Intersect(limitRange.Value);

        // If the given range is not within the process applicative memory, return null
        if (actualRange == null)
            return null;

        // Compute the minimum multiple of the system page size that can fit the size needed
        // This will be the maximum size that we are going to allocate
        uint pageSize = _osService.GetPageSize();
        bool isDirectMultiple = sizeNeeded % pageSize == 0;
        uint minFittingPageSize = (uint)(sizeNeeded / pageSize + (isDirectMultiple ? (ulong)0 : 1)) * pageSize;
        
        // Browse through regions in the memory range to find the first one that satisfies the size needed
        var nextAddress = actualRange.Value.Start;
        MemoryRange? freeRange = null;
        MemoryRangeMetadata currentMetadata;
        while (nextAddress.ToUInt64() < actualRange.Value.End.ToUInt64()
            && (currentMetadata = _osService.GetRegionMetadata(_processHandle, nextAddress, _is64Bits))
            .Size.ToUInt64() > 0)
        {
            nextAddress = (UIntPtr)(nextAddress.ToUInt64() + currentMetadata.Size.ToUInt64());

            // If the current region cannot be used, reinitialize the current free range and keep iterating
            if (!currentMetadata.IsFree)
            {
                freeRange = null;
                continue;
            }

            // Build a range with the current region
            // Start from the start of the current free range if it's not null, so that we can have ranges that span
            // across multiple regions.
            freeRange = new MemoryRange(freeRange?.Start ?? currentMetadata.StartAddress,
                (UIntPtr)(currentMetadata.StartAddress.ToUInt64() + currentMetadata.Size.ToUInt64()));
            
            if (freeRange.Value.GetSize() >= sizeNeeded)
            {
                // The free range is large enough.
                // If the free range is larger than the size needed, we will allocate the minimum multiple of the
                // system page size that can fit the requested size.
                ulong neededSize = Math.Min(freeRange.Value.GetSize(), minFittingPageSize);
                var finalRange = MemoryRange.FromStartAndSize(freeRange.Value.Start, neededSize);
                
                // Even if they are free, some regions cannot be allocated.
                // The only way to know if a region can be allocated is to try to allocate it.
                try
                {
                    _osService.AllocateMemory(_processHandle, finalRange.Start, (int)finalRange.GetSize(),
                        MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
                        forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);
                    return finalRange;
                }
                catch
                {
                    // The allocation failed. Reset the current range and keep iterating.
                    freeRange = null;
                    continue;
                }
            }
        }

        // If we reached the end of the memory range and didn't find a suitable free range, return null
        return null;
    }
    
    #region Store

    /// <summary>
    /// Reserves a range of memory of the given size. If no suitable range is found, a new range is allocated, and
    /// a reservation is made on it.
    /// </summary>
    /// <param name="size">Size of the memory range to reserve.</param>
    /// <param name="requireExecutable">Set to true if the memory range must be executable.</param>
    /// <returns>The reserved memory range.</returns>
    private AllocatedRange ReserveOrAllocateRange(ulong size, bool requireExecutable)
    {
        uint alignment = _is64Bits ? (uint)8 : 4;
        return _allocatedRanges.Select(r => r.TryReserveRange(size, alignment))
            .FirstOrDefault(r => r != null)
            ?? Allocate(size, requireExecutable).ReserveRange(size, alignment);
    }
    
    /// <summary>
    /// Stores the given data in the process memory. If needed, memory is allocated to store the data. Returns the
    /// reserved range that you can utilize to use the data.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="isCode">Set to true if the data is executable code. Defaults to false.</param>
    /// <returns>The reserved memory range.</returns>
    public AllocatedRange Store(byte[] data, bool isCode = false)
    {
        var reservedRange = ReserveOrAllocateRange((ulong)data.Length, isCode);
        WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        return reservedRange;
    }
    
    /// <summary>
    /// Stores the given data in the specified range of memory. Returns the reserved range that you can utilize to use
    /// the data.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="range">Range of memory to store the data in.</param>
    /// <returns>The reserved memory range.</returns>
    public AllocatedRange Store(byte[] data, AllocatedRange range)
    {
        uint alignment = _is64Bits ? (uint)8 : 4;
        var reservedRange = range.ReserveRange((ulong)data.Length, alignment);
        WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        return reservedRange;
    }

    /// <summary>
    /// Stores the given value or structure in the process memory. If needed, memory is allocated to store the data.
    /// Returns the reserved range that you can utilize to use the data.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>The reserved memory range.</returns>
    public AllocatedRange Store<T>(T value)
        => Store(value.ToBytes(), false);

    /// <summary>
    /// Stores the given value or structure in the specified range of memory. Returns the reserved range that you can
    /// utilize to use the data.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <param name="range">Range of memory to store the data in.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>The reserved memory range.</returns>
    public AllocatedRange Store<T>(T value, AllocatedRange range) where T: struct
        => Store(value.ToBytes(), range);

    #endregion
}