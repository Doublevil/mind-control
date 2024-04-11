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
    /// <param name="limitRange">Specify this parameter to limit the allocation to a specific range of memory.</param>
    /// <returns>The allocated memory range.</returns>
    public AllocatedRange Allocate(ulong size, MemoryRange? limitRange = null)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}