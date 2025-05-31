namespace MindControl.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail. 
/// </summary>
public class Result
{
    private readonly Failure? _failure;
    
    /// <summary>Default string representing a success.</summary>
    protected const string SuccessString = "The operation was successful.";
    
    /// <summary>
    /// Gets a successful <see cref="Result"/> instance.
    /// </summary>
    public static readonly Result Success = new();
    
    /// <summary>
    /// Gets the failure from an unsuccessful result. Throws if the operation was successful.
    /// Use this after checking <see cref="Result.IsFailure"/> to ensure the operation was not successful.
    /// </summary>
    public Failure Failure => IsFailure ? _failure!
        : throw new InvalidOperationException("Cannot access the failure from a successful result.");
    
    /// <summary>
    /// Gets a boolean indicating if the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Gets a boolean indicating if the operation was a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// Initializes a new successful <see cref="Result"/> instance.
    /// </summary>
    protected Result() { IsSuccess = true; }
    
    /// <summary>
    /// Initializes a new failed <see cref="Result"/> instance.
    /// </summary>
    /// <param name="failure">A description of the failure.</param>
    protected Result(Failure failure)
    {
        _failure = failure;
        IsSuccess = false;
    }
    
    /// <summary>Throws a <see cref="ResultFailureException"/> if the operation was not successful.</summary>
    public void ThrowOnFailure()
    {
        if (IsFailure)
            throw ToException();
    }
    
    /// <summary>
    /// Converts the result to an exception if it represents a failure.
    /// </summary>
    /// <returns>A new <see cref="ResultFailureException"/> instance if the operation was a failure.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the operation was successful and thus cannot be
    /// converted to an exception.</exception>
    public ResultFailureException ToException() => IsFailure ? new ResultFailureException(Failure)
        : throw new InvalidOperationException("Cannot convert a successful result to an exception.");
    
    /// <summary>
    /// Creates a new failed <see cref="Result"/> instance.
    /// </summary>
    /// <param name="failure">A description of the failure.</param>
    public static Result FailWith(Failure failure) => new(failure);

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => IsSuccess ? SuccessString : Failure.ToString();
    
    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="Result"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator Result(Failure result) => FailWith(result);
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail, with a result value in case of success.
/// </summary>
/// <typeparam name="TResult">Type of the result that can be returned in case of success.</typeparam>
public class Result<TResult> : Result
{
    private readonly TResult? _value;

    /// <summary>
    /// Gets the resulting value of the operation. Throws if the operation was not successful.
    /// Use this after checking <see cref="Result.IsSuccess"/> to ensure the operation was successful.
    /// </summary>
    public TResult Value => IsSuccess ? _value!
        : throw new InvalidOperationException("Cannot access the value of an unsuccessful result.",
            new ResultFailureException(Failure));
    
    /// <summary>
    /// Initializes a new successful <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="value">Result of the operation.</param>
    protected Result(TResult value) { _value = value; }
    
    /// <summary>
    /// Initializes a new failed <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">A description of the failure.</param>
    protected Result(Failure failure) : base(failure) { }
    
    /// <summary>
    /// Creates a new successful <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    public static Result<TResult> FromResult(TResult result) => new(result);
    
    /// <summary>
    /// Creates a new failed <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">A description of the failure.</param>
    public new static Result<TResult> FailWith(Failure failure) => new(failure);
    
    /// <summary>
    /// Gets the resulting value of the operation, or the default value for the resulting type if the operation was not
    /// successful. 
    /// </summary>
    public TResult? ValueOrDefault() => IsSuccess ? Value : default;
    
    /// <summary>
    /// Gets the resulting value of the operation, or the provided default value if the operation was not successful.
    /// </summary>
    /// <param name="defaultValue">Default value to return if the operation was not successful.</param>
    public TResult ValueOr(TResult defaultValue) => IsSuccess ? Value : defaultValue;
    
    /// <summary>
    /// Builds a new <see cref="Result{TResult}"/> instance from a successful result with a different type.
    /// </summary>
    /// <param name="result">Result to convert.</param>
    /// <typeparam name="TOtherResult">Type of the value of the result to convert.</typeparam>
    /// <returns>A new <see cref="Result{TResult}"/> instance with the value of the input result.</returns>
    public static Result<TResult> CastValueFrom<TOtherResult>(Result<TOtherResult> result)
        where TOtherResult : TResult
        => result.IsSuccess ? new Result<TResult>(result.Value!) : result.Failure;
    
    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator Result<TResult>(TResult result) => FromResult(result);

    /// <summary>
    /// Implicitly converts a failure to an unsuccessful <see cref="Result{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">Failure to convert.</param>
    public static implicit operator Result<TResult>(Failure failure) => FailWith(failure);
    
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => IsSuccess ? Value?.ToString() ?? SuccessString : Failure.ToString();
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail, with a result value in case of success. This
/// variant is disposable and may hold a disposable result. When a successful instance is disposed, the result will be
/// too.
/// </summary>
/// <typeparam name="TResult">Type of the result that can be returned in case of success.</typeparam>
public class DisposableResult<TResult> : Result<TResult>, IDisposable where TResult : IDisposable
{
    /// <summary>
    /// Initializes a new successful <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="value">Result of the operation.</param>
    protected DisposableResult(TResult value) : base(value) { }
    
    /// <summary>
    /// Initializes a new failed <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">A representation of the failure.</param>
    protected DisposableResult(Failure failure) : base(failure) { }
    
    /// <summary>
    /// Implicitly converts a result value to a successful <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="result">Result value to convert.</param>
    public static implicit operator DisposableResult<TResult>(TResult result) => FromResult(result);

    /// <summary>
    /// Implicitly converts a failure to an unsuccessful <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">Failure to convert.</param>
    public static implicit operator DisposableResult<TResult>(Failure failure) => FailWith(failure);
    
    /// <summary>
    /// Creates a new successful <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="result">Result of the operation.</param>
    public new static DisposableResult<TResult> FromResult(TResult result) => new(result);
    
    /// <summary>
    /// Creates a new failed <see cref="DisposableResult{TResult}"/> instance.
    /// </summary>
    /// <param name="failure">A representation of the failure.</param>
    public new static DisposableResult<TResult> FailWith(Failure failure) => new(failure);
    
    /// <summary>Disposes the result value if the operation was successful.</summary>
    public void Dispose()
    {
        if (IsSuccess)
            Value.Dispose();
    }
} 
