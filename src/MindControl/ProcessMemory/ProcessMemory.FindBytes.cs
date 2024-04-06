using System.Text.RegularExpressions;
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
    /// <param name="bytePattern">String representation of the byte pattern to find. This pattern should be a series of
    /// hexadecimal bytes, optionally separated by spaces. Each character, excluding spaces, can be a specific value
    /// (0-F) or a wildcard "?" character, indicating that the value to look for at this position could be any value.
    /// Read the documentation for more information.</param>
    /// <param name="range">Range of memory to scan. Leave this to null (the default) to scan the whole process
    /// memory. Restricting the memory range can dramatically improve the performance of the scan.</param>
    /// <param name="settings">Settings for the search. Leave this to null (the default) to use the default settings.
    /// Using more restrictive settings can dramatically improve the performance of the scan.</param>
    /// <returns>An enumerable of addresses where the pattern was found.</returns>
    public IEnumerable<UIntPtr> FindBytes(string bytePattern, MemoryRange? range = null,
        FindBytesSettings? settings = null)
    {
        var actualSettings = settings ?? new FindBytesSettings();
        var actualRange = GetClampedMemoryRange(range);
        actualRange.Validate();
        actualSettings.Validate();
        
        (byte[] bytePatternArray, byte[] maskArray) = ParseBytePattern(bytePattern);
        return FindBytesInternal(bytePatternArray, maskArray, actualRange, actualSettings);
    }
    
    /// <summary>
    /// Scans the memory of the target process for a byte pattern. Returns the address of each occurrence in the
    /// target range, or in the whole memory if no range is specified.
    /// This is the asynchronous variant of <see cref="FindBytes"/>. Use this variant if you need to keep your program
    /// responsive while the scan is going on.
    /// Read the documentation to learn how to perform efficient scans.
    /// </summary>
    /// <param name="bytePattern">String representation of the byte pattern to find. This pattern should be a series of
    /// hexadecimal bytes, optionally separated by spaces. Each character, excluding spaces, can be a specific value
    /// (0-F) or a wildcard "?" character, indicating that the value to look for at this position could be any value.
    /// Read the documentation for more information.</param>
    /// <param name="range">Range of memory to scan. Leave this to null (the default) to scan the whole process
    /// memory. Restricting the memory range can dramatically improve the performance of the scan.</param>
    /// <param name="settings">Settings for the search. Leave this to null (the default) to use the default settings.
    /// Using more restrictive settings can dramatically improve the performance of the scan.</param>
    /// <returns>An asynchronous enumerable of addresses where the pattern was found.</returns>
    public async IAsyncEnumerable<UIntPtr> FindBytesAsync(string bytePattern, MemoryRange? range = null,
        FindBytesSettings? settings = null)
    {
        var results = await Task.Run(() => FindBytes(bytePattern, range, settings));
        foreach (var result in results)
            yield return result;
    }

    #region Parsing and input processing
    
    private readonly Regex _bytePatternRegex = new("^([0-9A-F?]{2})*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    
    /// <summary>
    /// Parses the byte pattern string into a byte array and a mask array.
    /// These arrays are used to compare the pattern with the memory when scanning for matches.
    /// </summary>
    /// <param name="bytePattern">The byte pattern string to parse.</param>
    /// <returns>A tuple containing the byte array and the mask array.</returns>
    /// <exception cref="ArgumentException">Thrown if the byte pattern is invalid.</exception>
    private Tuple<byte[], byte[]> ParseBytePattern(string bytePattern)
    {
        if (string.IsNullOrWhiteSpace(bytePattern))
        {
            throw new ArgumentException("The byte pattern cannot be null or empty.", nameof(bytePattern));
        }
        
        var pattern = bytePattern.Replace(" ", "");
        if (pattern.Length % 2 != 0)
        {
            throw new ArgumentException("The byte pattern must contain an even number of non-space characters.",
                nameof(bytePattern));
        }
        if (!_bytePatternRegex.IsMatch(pattern))
        {
            throw new ArgumentException("The byte pattern must contain only hexadecimal characters and '?' wildcards.",
                nameof(bytePattern));
        }
        if (pattern == new string('?', pattern.Length))
        {
            throw new ArgumentException("The byte pattern cannot contain only '?' wildcards.", nameof(bytePattern));
        }

        var bytePatternArray = new byte[pattern.Length / 2];
        var maskArray = new byte[pattern.Length / 2];
        for (var i = 0; i < pattern.Length; i += 2)
        {
            var byteString = pattern.Substring(i, 2);
            if (byteString[0] == '?' || byteString[1] == '?')
            {
                // Both bytes are unknown. Set both the value and the mask to 0.
                bytePatternArray[i / 2] = 0;
                maskArray[i / 2] = 0;
            }
            else if (byteString[0] == '?')
            {
                // The first byte is unknown. Set the value to the second byte and the mask to 0xF.
                bytePatternArray[i / 2] = Convert.ToByte(byteString[1].ToString(), 16);
                maskArray[i / 2] = 0xF;
            }
            else if (byteString[1] == '?')
            {
                // The second byte is unknown. Set the value to the first byte multiplied by 16 and the mask to 0xF0.
                bytePatternArray[i / 2] = (byte)(Convert.ToByte(byteString[0].ToString(), 16) * 16);
                maskArray[i / 2] = 0xF0;
            }
            else
            {
                // Both bytes are known. Set the value to the byte and the mask to 0xFF.
                bytePatternArray[i / 2] = Convert.ToByte(byteString, 16);
                maskArray[i / 2] = 0xFF;
            }
        }

        return new Tuple<byte[], byte[]>(bytePatternArray, maskArray);
    }
    
    /// <summary>
    /// Returns the intersection of the input range with the full addressable memory range.
    /// If the input memory range is null, the full memory range is returned.
    /// </summary>
    /// <param name="input">Input memory range to clamp. If null, the full memory range is returned.</param>
    /// <returns>The clamped memory range, or the full memory range if the input is null.</returns>
    private MemoryRange GetClampedMemoryRange(MemoryRange? input)
    {
        var fullMemoryRange = _osService.GetFullMemoryRange();
        return input == null ? fullMemoryRange : new MemoryRange(
            Start: input.Value.Start.ToUInt64() < fullMemoryRange.Start.ToUInt64()
                ? fullMemoryRange.Start : input.Value.Start,
            End: input.Value.End.ToUInt64() > fullMemoryRange.End.ToUInt64()
                ? fullMemoryRange.End : input.Value.End
        );
    }
    
    #endregion
    
    #region Search implementation
    
    /// <summary>
    /// Scans the memory of the target process for a byte pattern. Returns the address of each occurrence in the
    /// target range.
    /// </summary>
    /// <param name="bytePattern">Byte pattern to search for.</param>
    /// <param name="mask">Mask to use when comparing the pattern with the memory.</param>
    /// <param name="range">Memory range to scan.</param>
    /// <param name="settings">Settings for the search.</param>
    private IEnumerable<UIntPtr> FindBytesInternal(byte[] bytePattern, byte[] mask, MemoryRange range,
        FindBytesSettings settings)
    {
        var regionRanges = GetAggregatedRegionRanges(range, settings);
        var resultCount = 0;
        foreach (var regionRange in regionRanges)
        {
            foreach (var address in ScanRangeForBytePattern(bytePattern, mask, regionRange))
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
        byte[] rangeMemory = _osService.ReadProcessMemory(_processHandle, range.Start, range.GetSize())
                             ?? Array.Empty<byte>();
        
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
            var currentRangeMetadata = _osService.GetRegionMetadata(_processHandle, currentAddress, _is64Bits);
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