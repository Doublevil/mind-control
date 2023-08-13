namespace MindControl.State;

/// <summary>
/// Event arguments for an exception.
/// </summary>
public class ExceptionEventArgs
{
    /// <summary>
    /// Gets the underlying exception.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Builds a <see cref="ExceptionEventArgs"/> with the given properties.
    /// </summary>
    /// <param name="exception">Exception related to the event.</param>
    public ExceptionEventArgs(Exception exception)
    {
        Exception = exception;
    }
}