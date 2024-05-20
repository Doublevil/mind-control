using MindControl.Internal;
using MindControl.Native;
using MindControl.Results;

namespace MindControl;

// This partial class implements methods related to memory allocation.
public partial class ProcessMemory
{
    private readonly List<MemoryAllocation> _allocations = new();
    
    /// <summary>
    /// Gets the ranges that have been allocated for this process. Dispose a range to free the memory and remove it
    /// from this list.
    /// </summary>
    public IReadOnlyList<MemoryAllocation> Allocations => _allocations.AsReadOnly();

    /// <summary>
    /// Attempts to allocate a memory range of the given size within the process. Use this method only when automatic
    /// allocation management through the Store methods is not appropriate.
    /// </summary>
    /// <param name="size">Size of the memory range to allocate.</param>
    /// <param name="forExecutableCode">Determines if the memory range can be used to store executable code.</param>
    /// <param name="limitRange">Specify this parameter to limit the allocation to a specific range of memory.</param>
    /// <returns>A result holding either the allocated memory range, or an allocation failure.</returns>
    public Result<MemoryAllocation, AllocationFailure> Allocate(ulong size, bool forExecutableCode,
        MemoryRange? limitRange = null)
    {
        if (size == 0)
            throw new ArgumentException("The size of the memory range to allocate must be greater than zero.",
                nameof(size));
        
        // Find a free memory range that satisfies the size needed
        var rangeResult = FindAndAllocateFreeMemory(size, forExecutableCode, limitRange);
        if (rangeResult.IsFailure)
            return rangeResult.Error;
        
        // Add the range to the list of allocated ranges and return it
        var allocatedRange = new MemoryAllocation(rangeResult.Value, forExecutableCode, this);
        _allocations.Add(allocatedRange);
        return allocatedRange;
    }
    
    /// <summary>
    /// Releases a range of allocated memory.
    /// </summary>
    /// <param name="allocation">Allocation to free.</param>
    /// <remarks>This method is internal because it is designed to be called by <see cref="MemoryAllocation.Dispose"/>.
    /// Users would release memory by disposing <see cref="MemoryAllocation"/> instances.</remarks>
    internal void Free(MemoryAllocation allocation)
    {
        if (!_allocations.Contains(allocation))
            return;
        
        _allocations.Remove(allocation);
        _osService.ReleaseMemory(_processHandle, allocation.Range.Start);
    }

    /// <summary>
    /// Finds a free memory range that satisfies the size needed and performs the allocation.
    /// </summary>
    /// <param name="sizeNeeded">Size of the memory range needed.</param>
    /// <param name="forExecutableCode">Set to true to allocate a memory block with execute permissions.</param>
    /// <param name="limitRange">Specify this parameter to limit the search to a specific range of memory.
    /// If left null (default), the entire process memory will be searched.</param>
    /// <returns>A result holding either the memory range found, or an allocation failure.</returns>
    /// <remarks>The reason why the method performs the allocation itself is because we cannot know if the range can
    /// actually be allocated without performing the allocation.</remarks>
    private Result<MemoryRange, AllocationFailure> FindAndAllocateFreeMemory(ulong sizeNeeded,
        bool forExecutableCode, MemoryRange? limitRange = null)
    {
        var maxRange = _osService.GetFullMemoryRange();
        var actualRange = limitRange == null ? maxRange : maxRange.Intersect(limitRange.Value);

        // If the given range is not within the process applicative memory, return null
        if (actualRange == null)
            return new AllocationFailureOnLimitRangeOutOfBounds(maxRange);

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
            && (currentMetadata = _osService.GetRegionMetadata(_processHandle, nextAddress, _is64Bits)
                .GetValueOrDefault()).Size.ToUInt64() > 0)
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
                var allocateResult = _osService.AllocateMemory(_processHandle, finalRange.Start,
                    (int)finalRange.GetSize(), MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
                    forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);
                
                // If the allocation succeeded, return the range.
                if (allocateResult.IsSuccess)
                    return finalRange;
                
                // The allocation failed. Reset the current range and keep iterating.
                freeRange = null;
                continue;
            }
        }

        // If we reached the end of the memory range and didn't find a suitable free range.
        return new AllocationFailureOnNoFreeMemoryFound(actualRange.Value, nextAddress);
    }
    
    #region Store

    /// <summary>
    /// Reserves a range of memory of the given size. If no suitable range is found within the current allocations,
    /// a new range is allocated, and a reservation is made on it.
    /// </summary>
    /// <param name="size">Size of the memory range to reserve.</param>
    /// <param name="requireExecutable">Set to true if the memory range must be executable.</param>
    /// <returns>A result holding either the resulting reservation, or an allocation failure.</returns>
    private Result<MemoryReservation, AllocationFailure> FindOrMakeReservation(ulong size, bool requireExecutable)
    {
        uint alignment = _is64Bits ? (uint)8 : 4;
        var reservationInExistingAllocation = _allocations
            .Where(a => !requireExecutable || a.IsExecutable)
            .Select(r => r.ReserveRange(size, alignment))
            .FirstOrDefault(r => r.IsSuccess)
            ?.Value;
        
        // Reservation successful in existing allocation
        if (reservationInExistingAllocation != null)
            return reservationInExistingAllocation;
        
        // No allocation could satisfy the reservation: allocate a new range
        var allocationResult = Allocate(size, requireExecutable);
        if (allocationResult.IsFailure)
            return allocationResult.Error;
        
        // Make a reservation within that new allocation
        var newAllocation = allocationResult.Value;
        var reservationResult = newAllocation.ReserveRange(size, alignment);
        if (reservationResult.IsFailure)
        {
            // There is no reason for the reservation to fail here, as we just allocated memory of sufficient size.
            // Just in case, we free the memory and return the most appropriate failure.
            Free(allocationResult.Value);
            return new AllocationFailureOnNoFreeMemoryFound(newAllocation.Range, newAllocation.Range.Start);
        }
        
        return reservationResult.Value;
    }
    
    /// <summary>
    /// Stores the given data in the process memory. If needed, memory is allocated to store the data. Returns the
    /// reserved range that you can utilize to use the data.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="isCode">Set to true if the data is executable code. Defaults to false.</param>
    /// <returns>A result holding either the reserved memory range, or an allocation failure.</returns>
    public Result<MemoryReservation, AllocationFailure> Store(byte[] data, bool isCode = false)
    {
        var reservedRangeResult = FindOrMakeReservation((ulong)data.Length, isCode);
        if (reservedRangeResult.IsFailure)
            return reservedRangeResult.Error;

        var reservedRange = reservedRangeResult.Value;
        WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        return reservedRange;
    }
    
    /// <summary>
    /// Stores the given data in the specified allocated range. Returns the reservation that holds the data.
    /// In most situations, you should use the <see cref="Store{T}(T)"/> or <see cref="Store(byte[],bool)"/> signatures
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically, unless you need to
    /// manage them manually.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="allocation">Allocated memory to store the data.</param>
    /// <returns>A result holding either the reservation storing the data, or a reservation failure.</returns>
    public Result<MemoryReservation, ReservationFailure> Store(byte[] data, MemoryAllocation allocation)
    {
        uint alignment = _is64Bits ? (uint)8 : 4;
        var reservedRangeResult = allocation.ReserveRange((ulong)data.Length, alignment);
        if (reservedRangeResult.IsFailure)
            return reservedRangeResult.Error;

        var reservedRange = reservedRangeResult.Value;
        WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        return reservedRange;
    }

    /// <summary>
    /// Stores the given value or structure in the process memory. If needed, memory is allocated to store the data.
    /// Returns the reservation that holds the data.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>The reservation holding the data.</returns>
    public Result<MemoryReservation, AllocationFailure> Store<T>(T value)
        => Store(value.ToBytes(), false);

    /// <summary>
    /// Stores the given value or structure in the specified range of memory. Returns the reservation that holds the
    /// data.
    /// In most situations, you should use the <see cref="Store{T}(T)"/> or <see cref="Store(byte[],bool)"/> signatures
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically, unless you need to
    /// manage them manually.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <param name="allocation">Range of memory to store the data in.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>The reservation holding the data.</returns>
    public Result<MemoryReservation, ReservationFailure> Store<T>(T value, MemoryAllocation allocation) where T: struct
        => Store(value.ToBytes(), allocation);

    #endregion
}