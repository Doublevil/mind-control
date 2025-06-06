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
    /// <returns>A result holding either the allocated memory range, or a failure.</returns>
    public DisposableResult<MemoryAllocation> Allocate(ulong size, bool forExecutableCode,
        MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        if (size == 0)
            return new InvalidArgumentFailure(nameof(size),
                "The size of the memory range to allocate must be greater than zero.");
        
        // Find a free memory range that satisfies the size needed
        var rangeResult = FindAndAllocateFreeMemory(size, forExecutableCode, limitRange, nearAddress);
        if (rangeResult.IsFailure)
            return rangeResult.Failure;
        
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
    /// <returns>A result holding either the memory range found, or a failure.</returns>
    /// <remarks>The reason why the method performs the allocation itself is because we cannot know if the range can
    /// actually be allocated without performing the allocation.</remarks>
    private Result<MemoryRange> FindAndAllocateFreeMemory(ulong sizeNeeded, bool forExecutableCode,
        MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
    {
        // Calculate the required page-aligned size
        uint pageSize = _osService.GetPageSize();
        bool isDirectMultiple = sizeNeeded % pageSize == 0;
        uint requiredSize = (uint)(sizeNeeded / pageSize + (isDirectMultiple ? (ulong)0 : 1)) * pageSize;
        
        // With no constraints, let the OS decide where to allocate
        if (limitRange == null && nearAddress == null)
        {
            var allocResult = _osService.AllocateMemory(ProcessHandle,
                (int)requiredSize, MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
                forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);

            if (allocResult.IsFailure)
                return allocResult.Failure;

            return MemoryRange.FromStartAndSize(allocResult.Value, requiredSize);
        }

        // Define the search boundaries
        var maxRange = _osService.GetFullMemoryRange(Is64Bit);
        var searchRange = limitRange == null ? maxRange : maxRange.Intersect(limitRange.Value);
        
        if (searchRange == null)
            return new LimitRangeOutOfBoundsFailure(maxRange);

        // Determine the starting address for the search
        UIntPtr startAddress = nearAddress ?? searchRange.Value.Start;
        
        // For near address searches, we'll try both directions
        if (nearAddress != null)
        {
            // Try to allocate memory close to the requested address
            var result = TryAllocateNear(startAddress, requiredSize, pageSize, forExecutableCode, searchRange.Value);
            if (result.IsSuccess)
                return result;
        }
        else
        {
            // When no near address is specified, just scan forward from start to end
            return AllocateNextFreeMemoryRange(startAddress, searchRange.Value.End, requiredSize, pageSize, forExecutableCode);
        }

        return new NoFreeMemoryFailure(searchRange.Value, startAddress);
    }

    /// <summary>
    /// Attempts to allocate a memory range as close as possible to the specified target address.
    /// </summary>
    /// <param name="targetAddress">Target address to allocate memory near.</param>
    /// <param name="requiredSize">Size of the memory range required.</param>
    /// <param name="pageSize">Size of the memory pages to use for allocation. This is used to align the addresses.
    /// </param>
    /// <param name="forExecutableCode">Set to true if the memory range can be used to store executable code.</param>
    /// <param name="searchRange">Range that defines the boundaries of the free memory search.</param>
    private Result<MemoryRange> TryAllocateNear(UIntPtr targetAddress, ulong requiredSize, uint pageSize,
        bool forExecutableCode, MemoryRange searchRange)
    {
        // Try to allocate at the target address first, just in case
        var directResult = TryAllocateAt(targetAddress, requiredSize, forExecutableCode);
        if (directResult.IsSuccess)
            return directResult;
        
        var targetRegion = _osService.GetRegionMetadata(ProcessHandle, targetAddress).ValueOrDefault();
        
        UIntPtr highAddress = targetAddress.AlignToClosest(pageSize);
        MemoryRangeMetadata highRegion = highAddress < targetRegion.StartAddress + targetRegion.Size ?
            targetRegion : GetNextFreeRegion(targetRegion, searchRange, goForward: true).ValueOrDefault();
        UIntPtr lowAddress = highAddress < pageSize ? 0 : highAddress - pageSize;
        MemoryRangeMetadata lowRegion = lowAddress >= targetRegion.StartAddress ?
            targetRegion : GetNextFreeRegion(targetRegion, searchRange, goForward: false).ValueOrDefault();
        
        // Track whether we've exhausted either direction
        bool canGoHigher = highRegion.Size != 0 && highAddress < searchRange.End;
        bool canGoLower = lowRegion.Size != 0 && lowAddress >= searchRange.Start;
        
        while (canGoHigher || canGoLower)
        {
            // Try above the target (increasing addresses) if the high address is closer
            if (canGoHigher &&
                (!canGoLower || targetAddress.DistanceTo(highAddress) <= targetAddress.DistanceTo(lowAddress)))
            {
                var result = TryAllocateAt(highAddress, requiredSize, forExecutableCode);
                if (result.IsSuccess)
                    return result;
                
                highAddress += pageSize;

                if (highAddress >= highRegion.StartAddress + highRegion.Size)
                {
                    var nextRegion = GetNextFreeRegion(highRegion, searchRange, goForward: true);
                    if (nextRegion.IsFailure)
                        canGoHigher = false;
                    else
                    {
                        highRegion = nextRegion.Value;
                        highAddress = highRegion.StartAddress;
                    }
                }
                
                if (highAddress >= searchRange.End)
                    canGoHigher = false;
            }
            
            // Try below the target (decreasing addresses) if the low address is closer
            if (canGoLower &&
                (!canGoHigher || targetAddress.DistanceTo(lowAddress) <= targetAddress.DistanceTo(highAddress)))
            {
                var result = TryAllocateAt(lowAddress, requiredSize, forExecutableCode);
                if (result.IsSuccess)
                    return result;
                
                lowAddress -= pageSize;
                
                if (lowAddress < lowRegion.StartAddress)
                {
                    var nextRegion = GetNextFreeRegion(lowRegion, searchRange, goForward: false);
                    if (nextRegion.IsFailure)
                        canGoLower = false;
                    else
                    {
                        lowRegion = nextRegion.Value;
                        lowAddress = lowRegion.StartAddress + lowRegion.Size - pageSize;
                    }
                }
                
                if (lowAddress < searchRange.Start)
                    canGoLower = false;
            }
        }
        
        return new NoFreeMemoryFailure(searchRange, targetAddress);
    }
    
    /// <summary>
    /// Starting at the given region, searches for the next free memory region within the specified search range.
    /// </summary>
    /// <param name="startRegion">Starting region to search from.</param>
    /// <param name="searchRange">Range of memory to search for free regions.</param>
    /// <param name="goForward">Set to true to search forward from the start region, or false to search backward.
    /// </param>
    private Result<MemoryRangeMetadata> GetNextFreeRegion(MemoryRangeMetadata startRegion, MemoryRange searchRange,
        bool goForward)
    {
        MemoryRangeMetadata currentRegion = startRegion;
        while (currentRegion.Size != 0 && currentRegion.StartAddress.ToUInt64() < searchRange.End.ToUInt64())
        {
            var nextAddress = goForward
                ? currentRegion.StartAddress + currentRegion.Size
                : currentRegion.StartAddress - 1;
            
            currentRegion = _osService.GetRegionMetadata(ProcessHandle, nextAddress).ValueOrDefault();
            
            if (currentRegion.IsFree)
                return currentRegion;
        }
        
        return new NoFreeMemoryFailure(searchRange, searchRange.End);
    }
    
    /// <summary>
    /// Allocates the next free memory range within the specified start and end addresses that can hold the
    /// required size.
    /// </summary>
    /// <param name="startAddress">Address to start searching for a free memory range.</param>
    /// <param name="endAddress">Address to end searching for a free memory range.</param>
    /// <param name="requiredSize">Size of the memory range required.</param>
    /// <param name="pageSize">Size of the memory pages to use for allocation. This is used to align the addresses.
    /// </param>
    /// <param name="forExecutableCode">Set to true if the memory range can be used to store executable code.</param>
    private Result<MemoryRange> AllocateNextFreeMemoryRange(UIntPtr startAddress, UIntPtr endAddress, 
        ulong requiredSize, uint pageSize, bool forExecutableCode)
    {
        var currentAddress = startAddress;
        
        while (currentAddress.ToUInt64() <= endAddress.ToUInt64())
        {
            var metadata = _osService.GetRegionMetadata(ProcessHandle, currentAddress).ValueOrDefault();
            
            // If we got invalid metadata or reached the end
            if (metadata.Size == 0)
                break;
                
            // Check if the region is free
            if (metadata.IsFree && metadata.Size >= requiredSize)
            {
                var finalRange = MemoryRange.FromStartAndSize(metadata.StartAddress, requiredSize);
                var result = TryAllocateAt(finalRange.Start, requiredSize, forExecutableCode);
                if (result.IsSuccess)
                    return result;
                
                currentAddress += pageSize;
                continue;
            }
            
            // Move to the next region
            currentAddress = metadata.StartAddress + metadata.Size;
        }
        
        return new NoFreeMemoryFailure(new MemoryRange(startAddress, endAddress), startAddress);
    }

    /// <summary>
    /// Attempts to allocate a memory range at the specified address with the given size.
    /// </summary>
    /// <param name="address">The address to allocate memory at.</param>
    /// <param name="size">Size of the memory range to allocate.</param>
    /// <param name="forExecutableCode">Set to true if the memory range can be used to store executable code.</param>
    private Result<MemoryRange> TryAllocateAt(UIntPtr address, ulong size, bool forExecutableCode)
    {
        var allocResult = _osService.AllocateMemory(ProcessHandle, address,
            (int)size, MemoryAllocationType.Commit | MemoryAllocationType.Reserve,
            forExecutableCode ? MemoryProtection.ExecuteReadWrite : MemoryProtection.ReadWrite);
            
        if (allocResult.IsSuccess)
            return MemoryRange.FromStartAndSize(address, size);
            
        return allocResult.Failure;
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
    /// <returns>A result holding either the resulting reservation, or a failure.</returns>
    public DisposableResult<MemoryReservation> Reserve(ulong size, bool requireExecutable,
        MemoryRange? limitRange = null, UIntPtr? nearAddress = null)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        if (size == 0)
            return new InvalidArgumentFailure(nameof(size),
                "The size of the memory range to reserve must be greater than zero.");
        
        uint alignment = Is64Bit ? (uint)8 : 4;
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
            return allocationResult.Failure;
        
        // Make a reservation within that new allocation
        var newAllocation = allocationResult.Value;
        var reservationResult = newAllocation.ReserveRange(size, alignment);
        if (reservationResult.IsFailure)
        {
            // There is no reason for the reservation to fail here, as we just allocated memory of sufficient size.
            // Just in case, we free the memory and return the most appropriate failure.
            Free(allocationResult.Value);
            return new NoFreeMemoryFailure(newAllocation.Range, newAllocation.Range.Start);
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
    public DisposableResult<MemoryReservation> Store(byte[] data, bool isCode = false)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        if (data.Length == 0)
            return new InvalidArgumentFailure(nameof(data), "The data to store must not be empty.");
        
        var reservedRangeResult = Reserve((ulong)data.Length, isCode);
        if (reservedRangeResult.IsFailure)
            return reservedRangeResult.Failure;

        var reservedRange = reservedRangeResult.Value;
        var writeResult = WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        if (writeResult.IsFailure)
        {
            // If writing the data failed, free the reservation and return the write failure.
            reservedRange.Dispose();
            return writeResult.Failure;
        }
        
        return reservedRange;
    }
    
    /// <summary>
    /// Stores the given data in the specified allocated range. Returns the reservation that holds the data.
    /// In most situations, you can use the <see cref="Store{T}(T)"/> or <see cref="Store(byte[],bool)"/> signatures
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically. Use this signature
    /// if you need to manage allocations and reservations manually.
    /// </summary>
    /// <param name="data">Data to store.</param>
    /// <param name="allocation">Allocated memory to store the data.</param>
    /// <returns>A result holding either the reservation storing the data, or a failure.</returns>
    public DisposableResult<MemoryReservation> Store(byte[] data, MemoryAllocation allocation)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        
        uint alignment = Is64Bit ? (uint)8 : 4;
        var reservedRangeResult = allocation.ReserveRange((ulong)data.Length, alignment);
        if (reservedRangeResult.IsFailure)
            return reservedRangeResult.Failure;

        var reservedRange = reservedRangeResult.Value;
        var writeResult = WriteBytes(reservedRange.Range.Start, data, MemoryProtectionStrategy.Ignore);
        if (writeResult.IsFailure)
        {
            // If writing the data failed, free the reservation and return the write failure.
            reservedRange.Dispose();
            return writeResult.Failure;
        }
        
        return reservedRange;
    }

    /// <summary>
    /// Stores the given value or structure in the process memory. If needed, memory is allocated to store the data.
    /// Returns the reservation that holds the data.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>A result holding either the reservation where the data has been written, or a failure.</returns>
    public DisposableResult<MemoryReservation> Store<T>(T value)
        => Store(value.ToBytes(), false);

    /// <summary>
    /// Stores the given value or structure in the specified range of memory. Returns the reservation that holds the
    /// data.
    /// In most situations, you can use the <see cref="Store{T}(T)"/> or <see cref="Store(byte[],bool)"/> signatures
    /// instead, to have the <see cref="ProcessMemory"/> instance handle allocations automatically. Use this signature
    /// if you need to manage allocations and reservations manually.
    /// </summary>
    /// <param name="value">Value or structure to store.</param>
    /// <param name="allocation">Range of memory to store the data in.</param>
    /// <typeparam name="T">Type of the value or structure.</typeparam>
    /// <returns>A result holding either the reservation where the data has been written, or a failure.</returns>
    public DisposableResult<MemoryReservation> Store<T>(T value, MemoryAllocation allocation)
        where T: struct
        => Store(value.ToBytes(), allocation);
    
    /// <summary>
    /// Stores the given string in the process memory. If needed, memory is allocated to store the string.
    /// Returns the reservation that holds the string.
    /// </summary>
    /// <param name="value">String to store.</param>
    /// <param name="settings">String settings to use to write the string.</param>
    /// <returns>A result holding either the reservation where the string has been written, or a failure.</returns>
    public DisposableResult<MemoryReservation> StoreString(string value, StringSettings settings)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        if (!settings.IsValid)
            return new InvalidArgumentFailure(nameof(settings), StringSettings.InvalidSettingsMessage);
        
        var bytes = settings.GetBytes(value);
        if (bytes == null)
            return new InvalidArgumentFailure(nameof(settings), StringSettings.GetBytesFailureMessage);
        
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
    /// <returns>A result holding either the reservation where the string has been written, or a failure.</returns>
    public DisposableResult<MemoryReservation> StoreString(string value, StringSettings settings,
        MemoryAllocation allocation)
    {
        if (!IsAttached)
            return new DetachedProcessFailure();
        if (!settings.IsValid)
            return new InvalidArgumentFailure(nameof(settings), StringSettings.InvalidSettingsMessage);
        
        var bytes = settings.GetBytes(value);
        if (bytes == null)
            return new InvalidArgumentFailure(nameof(settings), StringSettings.GetBytesFailureMessage);
        
        return Store(bytes, allocation);
    }
    
    #endregion
}