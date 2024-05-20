namespace MindControl.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail. 
/// </summary>
/// <typeparam name="TError">Type of the error that can be returned in case of failure.</typeparam>
public class Result<TError>
{
    private readonly TError? _error;
    
    /// <summary>Default string representing a success.</summary>
    protected const string SuccessString = "Success";
    
    /// <summary>Default string representing a failure.</summary>
    protected const string FailureString = "An unspecified failure occurred";
    
    /// <summary>
    /// Gets a successful <see cref="Result{TError}"/> instance.
    /// </summary>
    public static readonly Result<TError> Success = new();
    
    /// <summary>
    /// Gets the error that caused the operation to fail. Throws if the operation was successful.
    /// Use this after checking <see cref="Result{TResult,TError}.IsFailure"/> to ensure the operation was not
    /// successful.
    /// </summary>
    public TError Error => IsFailure ? _error!
        : throw new InvalidOperationException("Cannot access the error of a successful result.");
    
    /// <summary>
    /// Gets a boolean indicating if the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Gets a boolean indicating if the operation was a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// Initializes a new successful <see cref="Result{TError}"/> instance.
    /// </summary>
    protected Result() { IsSuccess = true; }
    
    /// <summary>
    /// Initializes a new failed <see cref="Result{TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    protected Result(TError error)
    {
        _error = error;
        IsSuccess = false;
    }
    
    /// <summary>
    /// Throws a <see cref="ResultFailureException{T}"/> if the operation was not successful.
    /// </summary>
    public void ThrowOnError()
    {
        if (IsFailure)
            throw new ResultFailureException<TError>(Error);
    }
    
    /// <summary>
    /// Creates a new failed <see cref="Result{TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    public static Result<TError> Failure(TError error) => new(error);

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => IsSuccess ? SuccessString : Error!.ToString() ?? FailureString;
    
    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="Result{TError}"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator Result<TError>(TError result) => Failure(result);
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail, with a result value in case of success.
/// </summary>
/// <typeparam name="TResult">Type of the result that can be returned in case of success.</typeparam>
/// <typeparam name="TError">Type of the error that can be returned in case of failure.</typeparam>
public sealed class Result<TResult, TError> : Result<TError>
{
    private readonly TResult? _value;

    /// <summary>
    /// Gets the resulting value of the operation. Throws if the operation was not successful.
    /// Use this after checking <see cref="Result{TResult,TError}.IsSuccess"/> to ensure the operation was successful.
    /// </summary>
    public TResult Value => IsSuccess ? _value!
        : throw new InvalidOperationException("Cannot access the value of an unsuccessful result.",
            new ResultFailureException<TError>(Error));
    
    /// <summary>
    /// Initializes a new successful <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="value">Result of the operation.</param>
    private Result(TResult value) { _value = value; }
    
    /// <summary>
    /// Initializes a new failed <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    private Result(TError error) : base(error) { }
    
    /// <summary>
    /// Creates a new successful <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    public static Result<TResult, TError> FromResult(TResult result)
        => new(result);
    
    /// <summary>
    /// Creates a new failed <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="error">Error that caused the operation to fail.</param>
    public new static Result<TResult, TError> Failure(TError error) => new(error);
    
    /// <summary>
    /// Gets the resulting value of the operation, or the default value if the operation was not successful.
    /// You can optionally provide a specific default value to return if the operation was not successful. 
    /// </summary>
    /// <paramref name="defaultValue">Default value to return if the operation was not successful. If not specified,
    /// defaults to the default value for the result type.</paramref>
    public TResult? GetValueOrDefault(TResult? defaultValue = default) => IsSuccess ? Value : defaultValue;
    
    /// <summary>
    /// Builds a new <see cref="Result{TResult,TError}"/> instance from a successful result with a different type.
    /// </summary>
    /// <param name="result">Result to convert.</param>
    /// <typeparam name="TOtherResult">Type of the value of the result to convert.</typeparam>
    /// <returns>A new <see cref="Result{TResult,TError}"/> instance with the value of the input result.</returns>
    public static Result<TResult, TError> CastValueFrom<TOtherResult>(Result<TOtherResult, TError> result)
        where TOtherResult : TResult
        => result.IsSuccess ? new Result<TResult, TError>(result.Value!) : result.Error;
    
    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator Result<TResult, TError>(TResult result)
        => FromResult(result);

    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="Result{TResult,TError}"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator Result<TResult, TError>(TError result)
        => Failure(result);
    
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => IsSuccess ? Value?.ToString() ?? SuccessString : Error?.ToString() ?? FailureString;
}
