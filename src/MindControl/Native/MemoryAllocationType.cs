namespace MindControl.Native;

/// <summary>
/// Allocation types for memory allocation functions.
/// </summary>
[Flags]
public enum MemoryAllocationType : uint
{
    /// <summary>
    /// Allocates physical storage in memory or in the paging file on disk for the specified reserved memory pages.
    /// </summary>
    Commit = 0x1000,

    /// <summary>
    /// Reserves a range of the process's virtual address space without allocating any actual physical storage in memory
    /// or in the paging file on disk.
    /// </summary>
    Reserve = 0x2000,

    /// <summary>
    /// Indicates that data in the memory range is no longer of interest.
    /// </summary>
    Reset = 0x80000,

    /// <summary>
    /// Reverses the effects of <see cref="Reset"/>.
    /// </summary>
    ResetUndo = 0x1000000,
    
    /// <summary>
    /// Reserves an address range that can be used to map Address Windowing Extensions (AWE) pages.
    /// </summary>
    Physical = 0x400000,

    /// <summary>
    /// Allocates memory at the highest possible address. This can be slower than regular allocations, especially when
    /// there are many allocations.
    /// </summary>
    TopDown = 0x100000,
    
    /// <summary>
    /// Allocates memory using large page support.
    /// </summary>
    LargePages = 0x20000000
}
