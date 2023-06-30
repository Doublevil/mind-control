namespace MindControl.Native;

/// <summary>
/// Contains properties of a memory range.
/// </summary>
public class MemoryRangeMetadata
{
    /// <summary>
    /// A pointer to the base address of the region of pages.
    /// </summary>
    public UIntPtr BaseAddress;
    
    /// <summary>
    /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
    /// </summary>
    public uint RegionSize;
}