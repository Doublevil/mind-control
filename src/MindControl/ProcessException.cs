namespace MindControl;

/// <summary>
/// Exception related to handling processes.
/// </summary>
public class ProcessException : Exception
{
    /// <summary>
    /// Gets the identifier of the process related to the exception, if any.
    /// </summary>
    public int? Pid { get; }
    
    /// <summary>
    /// Builds a <see cref="ProcessException"/> with the given details.
    /// </summary>
    /// <param name="message">Error message that explains the reason for the exception.</param>
    public ProcessException(string message) : this(null, message, null) {}

    /// <summary>
    /// Builds a <see cref="ProcessException"/> with the given details.
    /// </summary>
    /// <param name="pid">PID (process identifier) of the process related to the exception, if any.</param>
    /// <param name="message">Error message that explains the reason for the exception.</param>
    public ProcessException(int? pid, string message) : this(pid, message, null) {}
    
    /// <summary>
    /// Builds a <see cref="ProcessException"/> with the given details.
    /// </summary>
    /// <param name="pid">PID (process identifier) of the process related to the exception, if any.</param>
    /// <param name="message">Error message that explains the reason for the exception.</param>
    /// <param name="innerException">Exception that is the cause of this exception, if any.</param>
    public ProcessException(int? pid, string message, Exception? innerException) : base(message, innerException)
    {
        Pid = pid;
    }
}