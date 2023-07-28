namespace MindControl;

/// <summary>
/// Exception related to a memory manipulation operation.
/// </summary>
public class MemoryException : Exception
{
    /// <summary>
    /// Builds a <see cref="MemoryException"/> with the given details.
    /// </summary>
    /// <param name="message">Error message that explains the reason for the exception.</param>
    public MemoryException(string message) : base(message) {}
    
    /// <summary>
    /// Builds a <see cref="MemoryException"/> with the given details.
    /// </summary>
    /// <param name="message">Error message that explains the reason for the exception.</param>
    /// <param name="innerException">Exception that is the cause of this exception, if any.</param>
    public MemoryException(string message, Exception? innerException) : base(message, innerException) {}
}