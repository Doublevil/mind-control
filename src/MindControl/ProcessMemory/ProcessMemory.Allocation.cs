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
        // Find a free memory range that satisfies the size needed
        var range = FindFreeMemory(size, limitRange);
        if (range == null)
            throw new InvalidOperationException("No suitable free memory range was found.");
        
        // Allocate the memory range
        _osService.AllocateMemory(_processHandle, range.Value.Start, (int)range.Value.GetSize(),
            MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
            forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);
        
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
    /// Finds a free memory range that satisfies the size needed.
    /// </summary>
    /// <param name="sizeNeeded">Size of the memory range needed.</param>
    /// <param name="limitRange">Specify this parameter to limit the search to a specific range of memory.
    /// If left null (default), the entire process memory will be searched.</param>
    /// <returns>The start address of the free memory range, or null if no suitable range was found.</returns>
    private MemoryRange? FindFreeMemory(ulong sizeNeeded, MemoryRange? limitRange = null)
    {
        var maxRange = _osService.GetFullMemoryRange();
        var actualRange = limitRange == null ? maxRange : maxRange.Intersect(limitRange.Value);

        // If the given range is not within the process applicative memory, return null
        if (actualRange == null)
            return null;

        // Compute the minimum multiple of the system page size that can fit the size needed
        // This will be the maximum size that we are going to allocate
        uint pageSize = _osService.GetPageSize();
        uint minFittingPageSize = (uint)(sizeNeeded / pageSize + 1) * pageSize;
        
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
                return MemoryRange.FromStartAndSize(freeRange.Value.Start, neededSize);
            }
        }

        // If we reached the end of the memory range and didn't find a suitable free range, return null
        return null;
    }
}