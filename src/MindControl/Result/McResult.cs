namespace MindControl;

/// <summary>
/// Represents the result of an operation that can either succeed or fail. 
/// </summary>
/// <typeparam name="TError">Type of the error that can be returned in case of failure.</typeparam>
public class McResult<TError>
{
    /// <summary>
    /// Gets the error that caused the operation to fail, if any.
    /// </summary>
    public TError? Error { get; }
    
    /// <summary>
    /// Gets the error message that describes the error that caused the operation to fail, if any.
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Gets a boolean indicating if the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Initializes a new successful <see cref="McResult{TError}"/> instance.
    /// </summary>
    protected McResult() { IsSuccess = true; }
    
    /// <summary>
    /// Initializes a new failed <see cref="McResult{TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    /// <param name="errorMessage">Error message that describes the error that caused the operation to fail.</param>
    protected McResult(TError error, string? errorMessage)
    {
        Error = error;
        ErrorMessage = errorMessage;
        IsSuccess = false;
    }
    
    /// <summary>
    /// Creates a new successful <see cref="McResult{TError}"/> instance.
    /// </summary>
    public static McResult<TError> Success() => new();
    
    /// <summary>
    /// Creates a new failed <see cref="McResult{TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    /// <param name="errorMessage">Error message that describes the error that caused the operation to fail.</param>
    public static McResult<TError> Failure(TError error, string? errorMessage = null) => new(error, errorMessage);
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail, with a result value in case of success.
/// </summary>
/// <typeparam name="TResult">Type of the result that can be returned in case of success.</typeparam>
/// <typeparam name="TError">Type of the error that can be returned in case of failure.</typeparam>
public sealed class McResult<TResult, TError> : McResult<TError>
{
    /// <summary>
    /// Gets the result of the operation, if any.
    /// </summary>
    public TResult? Result { get; }
    
    /// <summary>
    /// Initializes a new successful <see cref="McResult{TResult, TError}"/> instance.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    private McResult(TResult result) { Result = result; }
    
    /// <summary>
    /// Initializes a new failed <see cref="McResult{TResult, TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    /// <param name="errorMessage">Error message that describes the error that caused the operation to fail.</param>
    private McResult(TError error, string? errorMessage) : base(error, errorMessage) { }
    
    /// <summary>
    /// Creates a new successful <see cref="McResult{TResult, TError}"/> instance.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    public static McResult<TResult, TError> Success(TResult result)
        => new(result);
    
    /// <summary>
    /// Creates a new failed <see cref="McResult{TResult, TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    /// <param name="errorMessage">Error message that describes the error that caused the operation to fail.</param>
    public new static McResult<TResult, TError> Failure(TError error, string? errorMessage = null)
        => new(error, errorMessage);
}
