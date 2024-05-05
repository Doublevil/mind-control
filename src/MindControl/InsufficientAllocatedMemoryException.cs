namespace MindControl;

/// <summary>
/// Exception thrown by a <see cref="MemoryAllocation"/> when trying to reserve a memory range with a size exceeding
/// the largest contiguous unreserved space.
/// </summary>
public class InsufficientAllocatedMemoryException : Exception
{
    /// <summary>
    /// Gets the requested size for the reservation attempt that caused the exception.
    /// </summary>
    public ulong RequestedSize { get; }
    
    /// <summary>
    /// Gets the requested byte alignment for the reservation attempt that caused the exception.
    /// </summary>
    public uint? RequestedAlignment { get; }
    
    /// <summary>
    /// Builds a new <see cref="InsufficientAllocatedMemoryException"/> with the given parameters.
    /// </summary>
    /// <param name="requestedSize">Requested size for the reservation attempt.</param>
    /// <param name="requestedAlignment">Requested byte alignment for the reservation attempt.</param>
    public InsufficientAllocatedMemoryException(ulong requestedSize, uint? requestedAlignment)
        : base($"The requested size of {requestedSize} bytes with {(requestedAlignment == null ? "no alignment" : $"an alignment of {requestedAlignment.Value}")} bytes exceeds the largest contiguous unreserved space of the {nameof(MemoryAllocation)} instance. Consider allocating more space, reserving multiple smaller blocks, or letting the {nameof(ProcessMemory)} instance handle allocations by using addressless Write method signatures. Read the \"Allocating memory\" section in the documentation for more information.")
    {
        RequestedSize = requestedSize;
        RequestedAlignment = requestedAlignment;
    }
}