namespace MindControl.Results;

/// <summary>Represents a failure when reading or writing a value anchor.</summary>
public abstract record ValueAnchorFailure;

/// <summary>Represents a failure when trying to read or write a value anchor on a disposed instance.</summary>
public record ValueAnchorFailureOnDisposedInstance : ValueAnchorFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    public override string ToString() => "The anchor is disposed and cannot be used anymore.";
}

/// <summary>Represents a failure when trying to read or write a value anchor with invalid arguments.</summary>
/// <param name="Failure">Underlying failure that occurred when trying to read or write the value.</param>
public record ValueAnchorFailure<TFailure>(TFailure Failure) : ValueAnchorFailure
{
    /// <summary>Returns a string that represents the current object.</summary>
    public override string ToString() => Failure?.ToString()
        ?? "An unspecified failure occurred when trying to read or write the value.";
}