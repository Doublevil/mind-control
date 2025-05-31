using MindControl.Results;
using MindControl.State;

namespace MindControl.Anchors;

/// <summary>Event arguments used when a freeze operation fails.</summary>
/// <param name="failure">Failure that occurred when trying to freeze the value.</param>
public class FreezeFailureEventArgs(Failure failure) : EventArgs
{
    /// <summary>Gets the failure that occurred when trying to freeze the value.</summary>
    public Failure Failure { get; } = failure;
}

/// <summary>
/// Provides methods to freeze a value in memory, using a timer to write the value at regular intervals.
/// </summary>
public class TimerValueFreezer<TValue> : IDisposable
{
    private readonly PrecisionTimer _timer;
    private readonly ValueAnchor<TValue> _anchor;
    private readonly TValue _value;
    private bool _isTicking;
    private bool _disposed;

    /// <summary>Event raised when a freeze operation fails.</summary>
    public event EventHandler<FreezeFailureEventArgs>? FreezeFailed;
    
    /// <summary>
    /// Freezes a value in memory, using a timer to write the value at regular intervals.
    /// </summary>
    /// <param name="anchor">Anchor holding the memory value to freeze.</param>
    /// <param name="value">Value to freeze in memory.</param>
    /// <param name="timerInterval">Interval at which the value should be written to memory.</param>
    public TimerValueFreezer(ValueAnchor<TValue> anchor, TValue value, TimeSpan timerInterval)
    {
        _anchor = anchor;
        _value = value;
        _timer = new PrecisionTimer(timerInterval);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <summary>
    /// Callback for the timer tick event. Writes the value to the anchor, and raises the <see cref="FreezeFailed"/>
    /// event if the write operation fails.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isTicking)
            return;

        _isTicking = true;
        try
        {
            var result = _anchor.Write(_value);
            if (result.IsFailure)
                FreezeFailed?.Invoke(this, new FreezeFailureEventArgs(result.Failure));
        }
        finally
        {
            _isTicking = false;
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
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            FreezeFailed = null;
        }

        _disposed = true;
    }
}