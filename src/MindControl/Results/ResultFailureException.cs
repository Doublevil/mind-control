namespace MindControl.Results;

/// <summary>
/// Exception that may be thrown by failed <see cref="Result{TError}"/>.
/// </summary>
public class ResultFailureException<T> : Exception
{
    /// <summary>Default error message.</summary>
    private const string DefaultMessage = "The operation failed.";
    
    /// <summary>
    /// Gets the error object describing the failure.
    /// </summary>
    public T Error { get; }

    /// <summary>
    /// Builds a <see cref="ResultFailureException{T}"/> with the given details.
    /// </summary>
    /// <param name="error">Error object describing the failure.</param>
    public ResultFailureException(T error) : this(error, null) {}

    /// <summary>
    /// Builds a <see cref="ResultFailureException{T}"/> with the given details.
    /// </summary>
    /// <param name="error">Error object describing the failure.</param>
    /// <param name="innerException">Exception that is the cause of this exception, if any.</param>
    public ResultFailureException(T error, Exception? innerException)
        : base(error?.ToString() ?? DefaultMessage, innerException)
    {
        Error = error;
    }
}