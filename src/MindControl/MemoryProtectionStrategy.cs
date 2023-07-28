namespace MindControl;

/// <summary>
/// Enumerates potential strategies to deal with memory protection.
/// </summary>
public enum MemoryProtectionStrategy
{
    /// <summary>
    /// Attempt to perform an operation without dealing with memory protection.
    /// Use this strategy if you care about performance and know that the memory area you target is not protected.
    /// </summary>
    Ignore = 0,
    
    /// <summary>
    /// Remove any memory protection on the target memory region before attempting a memory operation.
    /// </summary>
    Remove = 1,
    
    /// <summary>
    /// Remove any memory protection on the target memory region before attempting a memory operation, and then
    /// restore it to its previous state after the operation is done.
    /// This strategy is the safest but slowest choice.
    /// </summary>
    RemoveAndRestore = 2
}
