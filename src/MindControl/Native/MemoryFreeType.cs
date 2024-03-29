namespace MindControl.Native;

/// <summary>
/// Allocation types for memory freeing functions.
/// </summary>
[Flags]
public enum MemoryFreeType : uint
{
    /// <summary>
    /// Decommits the specified region of committed pages. After the operation, the pages are in the reserved state.
    /// </summary>
    Decommit = 0x4000,

    /// <summary>
    /// Releases the specified region of pages. After this operation, the pages are in the free state.
    /// </summary>
    Release = 0x8000
}
