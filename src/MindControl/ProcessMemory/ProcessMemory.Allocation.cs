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
    /// allocation management through the Store methods or <see cref="Reserve"/> method are not appropriate.
    /// </summary>
    /// <param name="size">Size of the memory range to allocate.</param>
    /// <param name="forExecutableCode">Determines if the memory range can be used to store executable code.</param>
    /// <param name="limitRange">Specify this parameter to limit the allocation to a specific range of memory.</param>
    /// <param name="nearAddress">If specified, try to allocate as close as possible to this address.</param>
    /// <returns>A result holding either the allocated memory range, or an allocation failure.</returns>
    public Result<MemoryAllocation, AllocationFailure> Allocate(ulong size, bool forExecutableCode,
        MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
    {
        if (size == 0)
            return new AllocationFailureOnInvalidArguments(
                "The size of the memory range to allocate must be greater than zero.");
        
        // Find a free memory range that satisfies the size needed
        var rangeResult = FindAndAllocateFreeMemory(size, forExecutableCode, limitRange, nearAddress);
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
        _osService.ReleaseMemory(ProcessHandle, allocation.Range.Start);
    }

    /// <summary>
    /// Finds a free memory range that satisfies the size needed and performs the allocation.
    /// </summary>
    /// <param name="sizeNeeded">Size of the memory range needed.</param>
    /// <param name="forExecutableCode">Set to true to allocate a memory block with execute permissions.</param>
    /// <param name="limitRange">Specify this parameter to limit the search to a specific range of memory.
    /// If left null (default), the entire process memory will be searched.</param>
    /// <param name="nearAddress">If specified, try to allocate as close as possible to this address.</param>
    /// <returns>A result holding either the memory range found, or an allocation failure.</returns>
    /// <remarks>The reason why the method performs the allocation itself is because we cannot know if the range can
    /// actually be allocated without performing the allocation.</remarks>
    private Result<MemoryRange, AllocationFailure> FindAndAllocateFreeMemory(ulong sizeNeeded,
        bool forExecutableCode, MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
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
        // If a near address is specified, start there. Otherwise, start at the beginning of the memory range.
        // For near address search, we are going to search back and forth around the address, which complicates the
        // process a bit. It means we have to keep track of both the next lowest address and next highest address.
        var nextAddress = nearAddress ?? actualRange.Value.Start;
        var nextAddressForward = nextAddress;
        var nextAddressBackwards = nextAddress;
        bool goingForward = true;
        
        MemoryRange? freeRange = null;
        MemoryRangeMetadata currentMetadata;
        while ((nextAddressForward.ToUInt64() < actualRange.Value.End.ToUInt64()
                || nextAddressBackwards.ToUInt64() > actualRange.Value.Start.ToUInt64())
            && (currentMetadata = _osService.GetRegionMetadata(ProcessHandle, nextAddress, Is64Bits)
                .GetValueOrDefault()).Size.ToUInt64() > 0)
        {
            nextAddressForward = (UIntPtr)Math.Max(nextAddressForward.ToUInt64(),
                nextAddress.ToUInt64() + currentMetadata.Size.ToUInt64());
            nextAddressBackwards = (UIntPtr)Math.Min(nextAddressBackwards.ToUInt64(),
                currentMetadata.StartAddress.ToUInt64() - 1);
            
            nextAddress = goingForward ? nextAddressForward : nextAddressBackwards;
            
            // If the current region cannot be used, reinitialize the current free range and keep iterating
            if (!currentMetadata.IsFree)
            {
                freeRange = null;

                // In a near address search, we may change direction there depending on which next address is closest.
                if (nearAddress != null)
                {
                    var forwardDistance = nearAddress.Value.DistanceTo(nextAddressForward);
                    var backwardDistance = nearAddress.Value.DistanceTo(nextAddressBackwards);
                    goingForward = forwardDistance <= backwardDistance;
                    nextAddress = goingForward ? nextAddressForward : nextAddressBackwards;
                }
                
                continue;
            }
            
            // Build a range with the current region
            // Extend the free range if it's not null, so that we can have ranges that span across multiple regions.
            if (goingForward)
            {
                freeRange = new MemoryRange(freeRange?.Start ?? currentMetadata.StartAddress,
                    (UIntPtr)(currentMetadata.StartAddress.ToUInt64() + currentMetadata.Size.ToUInt64()));
            }
            else
            {
                freeRange = new MemoryRange(currentMetadata.StartAddress,
                    (UIntPtr)(freeRange?.End.ToUInt64() ?? currentMetadata.StartAddress.ToUInt64()
                        + currentMetadata.Size.ToUInt64()));
            }

            if (freeRange.Value.GetSize() >= sizeNeeded)
            {
                // The free range is large enough.
                // If the free range is larger than the size needed, we will allocate the minimum multiple of the
                // system page size that can fit the requested size.
                ulong neededSize = Math.Min(freeRange.Value.GetSize(), minFittingPageSize);
                var finalRange = MemoryRange.FromStartAndSize(freeRange.Value.Start, neededSize);
                
                // Even if they are free, some regions cannot be allocated.
                // The only way to know if a region can be allocated is to try to allocate it.
                var allocateResult = _osService.AllocateMemory(ProcessHandle, finalRange.Start,
                    (int)finalRange.GetSize(), MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
                    forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);
                
                // If the allocation succeeded, return the range.
                if (allocateResult.IsSuccess)
                    return finalRange;
                
                // The allocation failed. Reset the current range and keep iterating.
                freeRange = null;
                
                // In a near address search, we may change direction there depending on which next address is closest.
                if (nearAddress != null)
                {
                    var forwardDistance = nearAddress.Value.DistanceTo(nextAddressForward);
                    var backwardDistance = nearAddress.Value.DistanceTo(nextAddressBackwards);
                    goingForward = forwardDistance <= backwardDistance;
                    nextAddress = goingForward ? nextAddressForward : nextAddressBackwards;
                }
                
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
    /// <param name="requireExecutable">Set to true if the memory range must be executable (to store code).</param>
    /// <param name="limitRange">Specify this parameter to limit the reservation to allocations within a specific range
    /// of memory. If left null (default), any allocation can be used. Otherwise, only allocations within the specified
    /// range will be considered, and if none are available, a new allocation will be attempted within that range.
    /// </param>
    /// <param name="nearAddress">If specified, prioritize allocations by their proximity to this address. If no
    /// matching allocation is found, a new allocation as close as possible to this address will be attempted.</param>
    /// <returns>A result holding either the resulting reservation, or an allocation failure.</returns>
    public Result<MemoryReservation, AllocationFailure> Reserve(ulong size, bool requireExecutable,
        MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
    {
        if (size == 0)
            return new AllocationFailureOnInvalidArguments(
                "The size of the memory range to reserve must be greater than zero.");
        
        uint alignment = Is64Bits ? (uint)8 : 4;
        var existingAllocations = _allocations
            .Where(a => (!requireExecutable || a.IsExecutable)
                && (limitRange == null || limitRange.Value.Contains(a.Range.Start)));
        
        // If we have a near address, sort the allocations by their distance to that address
        if (nearAddress != null)
            existingAllocations = existingAllocations.OrderBy(a => a.Range.DistanceTo(nearAddress.Value));
        
        // Pick the first allocation that works
        var reservationInExistingAllocation = existingAllocations
            .Select(r => r.ReserveRange(size, alignment))
            .FirstOrDefault(r => r.IsSuccess)
            ?.Value;
        
        // Reservation successful in existing allocation
        if (reservationInExistingAllocation != null)
            return reservationInExistingAllocation;
        
        // No allocation could satisfy the reservation: allocate a new range
        var allocationResult = Allocate(size, requireExecutable, limitRange, nearAddress);
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
        var reservedRangeResult = Reserve((ulong)data.Length, isCode);
        if (reservedRangeResult.IsFailure)
            return reservedRangeResult.Error;

        var reservedRange = reservedRangeResult.Value;
        WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        return reservedRange;
    }
    
    /// <summary>
    /// Stores the given data in the specified allocated range. Returns the reservation that holds the data.
    /// In most situations, you should use the <see cref="Store{T}(T)"/> or <see cref="Store(byte[],bool)"/> signatures
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically. Use this signature
    /// if you need to manage allocations and reservations manually.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="allocation">Allocated memory to store the data.</param>
    /// <returns>A result holding either the reservation storing the data, or a reservation failure.</returns>
    public Result<MemoryReservation, ReservationFailure> Store(byte[] data, MemoryAllocation allocation)
    {
        uint alignment = Is64Bits ? (uint)8 : 4;
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
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically. Use this signature
    /// if you need to manage allocations and reservations manually.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <param name="allocation">Range of memory to store the data in.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>The reservation holding the data.</returns>
    public Result<MemoryReservation, ReservationFailure> Store<T>(T value, MemoryAllocation allocation) where T: struct
        => Store(value.ToBytes(), allocation);
    
    /// <summary>
    /// Stores the given string in the process memory. If needed, memory is allocated to store the string.
    /// Returns the reservation that holds the string.
    /// </summary>
    /// <param name="value">String to store.</param>
    /// <param name="settings">String settings to use to write the string.</param>
    /// <returns>The reservation holding the string.</returns>
    public Result<MemoryReservation, AllocationFailure> StoreString(string value, StringSettings settings)
    {
        if (!settings.IsValid)
            return new AllocationFailureOnInvalidArguments(StringSettings.InvalidSettingsMessage);
        
        var bytes = settings.GetBytes(value);
        if (bytes == null)
            return new AllocationFailureOnInvalidArguments(StringSettings.GetBytesFailureMessage);
        
        return Store(bytes, isCode: false);
    }

    /// <summary>
    /// Stores the given string in the specified range of memory. Returns the reservation that holds the string.
    /// In most situations, you should use the <see cref="StoreString(string,StringSettings)"/> signature instead, to
    /// have the <see cref="ProcessMemory"/> instance handle allocations automatically. Use this signature if you need
    /// to manage allocations and reservations manually.
    /// </summary>
    /// <param name="value">String to store.</param>
    /// <param name="settings">String settings to use to write the string.</param>
    /// <param name="allocation">Range of memory to store the string in.</param>
    /// <returns>The reservation holding the string.</returns>
    public Result<MemoryReservation, ReservationFailure> StoreString(string value, StringSettings settings,
        MemoryAllocation allocation)
    {
        if (!settings.IsValid)
            return new ReservationFailureOnInvalidArguments(StringSettings.InvalidSettingsMessage);
        
        var bytes = settings.GetBytes(value);
        if (bytes == null)
            return new ReservationFailureOnInvalidArguments(StringSettings.GetBytesFailureMessage);
        
        return Store(bytes, allocation);
    }
    
    #endregion
}