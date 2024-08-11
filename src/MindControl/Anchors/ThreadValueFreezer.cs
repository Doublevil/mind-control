namespace MindControl.Anchors;

/// <summary>Provides methods to freeze a value in memory, using a thread that constantly writes the value.</summary>
/// <typeparam name="TValue">Type of the value to freeze.</typeparam>
/// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
/// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
public class ThreadValueFreezer<TValue, TReadFailure, TWriteFailure> : IDisposable
{
    private readonly ValueAnchor<TValue, TReadFailure, TWriteFailure> _anchor;
    private readonly TValue _value;
    private bool _disposed;

    /// <summary>Event raised when a freeze operation fails.</summary>
    public event EventHandler<FreezeFailureEventArgs<TWriteFailure>>? FreezeFailed;
    
    /// <summary>
    /// Freezes a value in memory, using a thread that constantly writes the target value.
    /// </summary>
    /// <param name="anchor">Anchor holding the memory value to freeze.</param>
    /// <param name="value">Value to freeze in memory.</param>
    public ThreadValueFreezer(ValueAnchor<TValue, TReadFailure, TWriteFailure> anchor, TValue value)
    {
        _anchor = anchor;
        _value = value;
        new Thread(WriteForever).Start();
    }

    /// <summary>
    /// Writes the value to the anchor until this instance is disposed, and raises the <see cref="FreezeFailed"/> event
    /// whenever the write operation fails.
    /// </summary>
    private void WriteForever()
    {
        while (!_disposed)
        {
            var result = _anchor.Write(_value);
            if (result.IsFailure)
                FreezeFailed?.Invoke(this, new FreezeFailureEventArgs<TWriteFailure>(result.Error));
        }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the timer and unsubscribes from the tick event.</summary>
    /// <param name="disposing">Whether the object is being disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            FreezeFailed = null;

        _disposed = true;
    }
}