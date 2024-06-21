using MindControl.Native;

namespace MindControl;

/// <summary>
/// Settings for the FindBytes method.
/// </summary>
public class FindBytesSettings
{
    /// <summary>
    /// Gets or sets a value indicating if the search should scan writable memory.
    /// If null (default), the search will scan both writable and non-writable memory.
    /// Set this to true to search for data.
    /// </summary>
    public bool? SearchWritable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the search should scan executable memory.
    /// If null (default), the search will scan both executable and non-executable memory.
    /// Set this to true to search for code.
    /// </summary>
    public bool? SearchExecutable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the search should scan readable memory.
    /// If null (default), the search will scan both readable and non-readable memory.
    /// </summary>
    public bool? SearchReadable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the search should include mapped memory.
    /// If null (default), the search will scan both mapped and non-mapped memory.
    /// </summary>
    public bool? SearchMapped { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return. If null (default), all results will be returned.
    /// </summary>
    public int? MaxResultCount { get; set; } = null;

    /// <summary>
    /// Throws an exception if the settings are invalid.
    /// </summary>
    public void Validate()
    {
        if (MaxResultCount < 1)
        {
            throw new ArgumentException("The maximum result count must be either null or greater than zero.",
                nameof(MaxResultCount));
        }
    }
}

// This partial class implements array of byte scanning methods.
public partial class ProcessMemory
{
    /// <summary>
    /// Scans the memory of the target process for a byte pattern. Returns the address of each occurrence in the
    /// target range, or in the whole memory if no range is specified.
    /// Depending on the parameters, the scan may take a long time to complete. Use the asynchronous variant if you need
    /// to keep your program responsive while the scan is going on.
    /// Read the documentation to learn how to perform efficient scans.
    /// </summary>
    /// <param name="pattern">Byte pattern to look for. See <see cref="ByteSearchPattern.TryParse"/>. You can use a
    /// string instead and it will be converted implicitly. An example would be "1F ?? 4B 00 ?6".</param>
    /// <param name="range">Range of memory to scan. Leave this to null (the default) to scan the whole process
    /// memory. Restricting the memory range can dramatically improve the performance of the scan.</param>
    /// <param name="settings">Settings for the search. Leave this to null (the default) to use the default settings.
    /// Using more restrictive settings can dramatically improve the performance of the scan.</param>
    /// <returns>An enumerable of addresses where the pattern was found.</returns>
    public IEnumerable<UIntPtr> FindBytes(ByteSearchPattern pattern, MemoryRange? range = null,
        FindBytesSettings? settings = null)
    {
        var actualSettings = settings ?? new FindBytesSettings();
        var actualRange = GetClampedMemoryRange(range);
        actualSettings.Validate();
        return FindBytesInternal(pattern, actualRange, actualSettings);
    }
    
    /// <summary>
    /// Scans the memory of the target process for a byte pattern. Returns the address of each occurrence in the
    /// target range, or in the whole memory if no range is specified.
    /// This is the asynchronous variant of <see cref="FindBytes"/>. Use this variant if you need to keep your program
    /// responsive while the scan is going on.
    /// Read the documentation to learn how to perform efficient scans.
    /// </summary>
    /// <param name="pattern">Byte pattern to look for. See <see cref="ByteSearchPattern.TryParse"/>. You can use a
    /// string instead and it will be converted implicitly. An example would be "1F ?? 4B 00 ?6".</param>
    /// <param name="range">Range of memory to scan. Leave this to null (the default) to scan the whole process
    /// memory. Restricting the memory range can dramatically improve the performance of the scan.</param>
    /// <param name="settings">Settings for the search. Leave this to null (the default) to use the default settings.
    /// Using more restrictive settings can dramatically improve the performance of the scan.</param>
    /// <returns>An asynchronous enumerable of addresses where the pattern was found.</returns>
    public async IAsyncEnumerable<UIntPtr> FindBytesAsync(ByteSearchPattern pattern, MemoryRange? range = null,
        FindBytesSettings? settings = null)
    {
        var results = await Task.Run(() => FindBytes(pattern, range, settings));
        foreach (var result in results)
            yield return result;
    }
    
    #region Search implementation
    
    /// <summary>
    /// Scans the memory of the target process for a byte pattern. Returns the address of each occurrence in the
    /// target range.
    /// </summary>
    /// <param name="pattern">Byte pattern to search for.</param>
    /// <param name="range">Memory range to scan.</param>
    /// <param name="settings">Settings for the search.</param>
    private IEnumerable<UIntPtr> FindBytesInternal(ByteSearchPattern pattern, MemoryRange range,
        FindBytesSettings settings)
    {
        var regionRanges = GetAggregatedRegionRanges(range, settings);
        var resultCount = 0;
        foreach (var regionRange in regionRanges)
        {
            foreach (var address in ScanRangeForBytePattern(pattern.ByteArray, pattern.Mask, regionRange))
            {
                yield return address;

                resultCount++;
                if (settings.MaxResultCount != null && resultCount >= settings.MaxResultCount)
                    yield break;
            }
        }
    }
    
    /// <summary>
    /// Scans the input memory range for the given byte pattern.
    /// </summary>
    /// <param name="bytePattern">Byte pattern to search for.</param>
    /// <param name="mask">Mask to use when comparing the pattern with the memory.</param>
    /// <param name="range">Memory range to scan.</param>
    /// <returns>An enumerable of addresses where the pattern was found.</returns>
    private IEnumerable<UIntPtr> ScanRangeForBytePattern(byte[] bytePattern, byte[] mask, MemoryRange range)
    {
        // Read the whole memory range and place it in a byte array.
        byte[] rangeMemory = _osService.ReadProcessMemory(ProcessHandle, range.Start, range.GetSize())
            .GetValueOrDefault() ?? Array.Empty<byte>();
        
        int maxIndex = rangeMemory.Length - bytePattern.Length;
        
        // Iterate through the memory range, checking for the pattern.
        for (var rangeIndex = 0; rangeIndex <= maxIndex; rangeIndex++)
        {
            if ((rangeMemory[rangeIndex] & mask[0]) != (bytePattern[0] & mask[0]))
                continue;
            
            // First byte matches. Check the rest of the pattern.
            var isMatching = true;
            for (int patternIndex = bytePattern.Length - 1; patternIndex >= 1; patternIndex--)
            {
                // Keep iterating until either the pattern is fully matched or a mismatch is found.
                if ((rangeMemory[rangeIndex + patternIndex] & mask[patternIndex]) ==
                    (bytePattern[patternIndex] & mask[patternIndex]))
                    continue;
                
                isMatching = false;
                break;
            }

            // If the pattern was fully matched (isMatching is still true), return the address.
            if (isMatching)
                yield return (UIntPtr)(range.Start.ToUInt64() + (ulong)rangeIndex);
        }
    }
    
    /// <summary>
    /// Finds subsets of the input memory range that are compatible with the given search settings.
    /// Aggregates contiguous compatible regions to prevent failures in cases of multi-region patterns.
    /// </summary>
    /// <param name="range">Memory range to browse for compatible regions.</param>
    /// <param name="settings">Search settings to check against.</param>
    /// <returns>An array of compatible memory ranges.</returns>
    private MemoryRange[] GetAggregatedRegionRanges(MemoryRange range, FindBytesSettings settings)
    {
        ulong rangeEnd = range.End.ToUInt64();
        List<MemoryRange> compatibleRanges = new();
        MemoryRange? currentRange = null;
        
        UIntPtr currentAddress = range.Start;
        while (currentAddress.ToUInt64() <= rangeEnd)
        {
            var getRegionResult = _osService.GetRegionMetadata(ProcessHandle, currentAddress);
            if (getRegionResult.IsFailure)
            {
                // If we failed to get the region metadata, we cannot continue because we don't know where the next
                // region starts.
                break;
            }
            
            var currentRangeMetadata = getRegionResult.Value;
            if (currentRangeMetadata.Size.ToUInt64() == 0)
            {
                // We cannot keep browsing if the size is 0 because we don't know where the next region starts.
                break;
            }
            
            ulong nextAddress = currentRangeMetadata.StartAddress.ToUInt64() + currentRangeMetadata.Size.ToUInt64();
            
            if (IsMemoryRangeCompatible(currentRangeMetadata, settings))
            {
                // The current range is compatible.
                UIntPtr endAddress = (UIntPtr)(nextAddress - 1);
                if (currentRange == null)
                {
                    // This is either the first range, or the previous range was not compatible. Start a new range.
                    currentRange = new MemoryRange(
                        Start: currentRangeMetadata.StartAddress,
                        End: endAddress);
                }
                else
                {
                    // The previous range was also compatible. This one is contiguous. Extend the previous range.
                    currentRange = currentRange.Value with { End = endAddress };
                }
            }
            else
            {
                // The current range is not compatible.
                if (currentRange != null)
                {
                    // Previous range was compatible. Add it to the list and reset the current range.
                    compatibleRanges.Add(currentRange.Value);
                    currentRange = null;
                }
            }
            
            currentAddress = (UIntPtr)nextAddress;
        }
        
        // If the last range was compatible, add it to the list.
        if (currentRange != null)
        {
            compatibleRanges.Add(currentRange.Value);
        }
        
        return compatibleRanges.ToArray();
    }

    /// <summary>
    /// Checks if the memory range properties are compatible with the search settings.
    /// </summary>
    /// <param name="rangeProperties">Memory range properties to check.</param>
    /// <param name="settings">Search settings to check against.</param>
    /// <returns>True if the memory range is compatible with the search settings, false otherwise.</returns>
    private bool IsMemoryRangeCompatible(MemoryRangeMetadata rangeProperties, FindBytesSettings settings)
    {
        if (!rangeProperties.IsCommitted || rangeProperties.IsProtected)
            return false;
        if (settings.SearchWritable.HasValue && settings.SearchWritable.Value != rangeProperties.IsWritable)
            return false;
        if (settings.SearchExecutable.HasValue && settings.SearchExecutable.Value != rangeProperties.IsExecutable)
            return false;
        if (settings.SearchReadable.HasValue && settings.SearchReadable.Value != rangeProperties.IsReadable)
            return false;
        if (settings.SearchMapped.HasValue && settings.SearchMapped.Value != rangeProperties.IsMapped)
            return false;
        return true;
    }
    
    #endregion
}