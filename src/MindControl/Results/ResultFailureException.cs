namespace MindControl.Results;

/// <summary>
/// Exception that may be thrown by failed <see cref="Result"/>.
/// </summary>
public class ResultFailureException : Exception
{
    /// <summary>
    /// Gets a representation of the failure at the origin of the exception.
    /// </summary>
    public Failure Failure { get; }

    /// <summary>
    /// Builds a <see cref="ResultFailureException"/> with the given details.
    /// </summary>
    /// <param name="failure">Representation of the failure at the origin of the exception.</param>
    /// <param name="innerException">Exception that is the cause of this exception, if any.</param>
    public ResultFailureException(Failure failure, Exception? innerException = null)
        : base(failure.ToString(), innerException)
    {
        Failure = failure;
    }
}