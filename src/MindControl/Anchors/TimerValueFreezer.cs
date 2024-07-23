using MindControl.Results;
using MindControl.State;

namespace MindControl.Anchors;

/// <summary>
/// Provides methods to freeze a value in memory for a specific duration.
/// This implementation uses a timer to write the value to memory at regular intervals.
/// </summary>
public class TimerValueFreezer<TValue, TReadFailure, TWriteFailure>(TValue value, TimeSpan timerInterval)
    : IValueFreezer<TValue, TReadFailure, TWriteFailure>
{
    private IValueAnchor<TValue, TReadFailure, TWriteFailure>? _anchor;
    private PrecisionTimer? _timer;
    
    /// <summary>Event raised when a freeze operation fails.</summary>
    public event EventHandler<FreezeFailureEventArgs<ValueAnchorFailure>>? FreezeFailed;

    /// <summary>Gets a boolean value indicating if a value is currently being frozen.</summary>
    public bool IsFreezing => _timer != null;
    
    /// <summary>Freezes the memory area that the anchor is attached to, preventing its value from changing, until
    /// either this instance is disposed, or <see cref="Unfreeze"/> is called.</summary>
    /// <param name="anchor">Anchor holding the memory value to freeze.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result<string> StartFreezing(IValueAnchor<TValue, TReadFailure, TWriteFailure> anchor)
    {
        if (IsFreezing)
            return "This instance is already freezing a value.";
        
        _anchor = anchor;
        _timer = new PrecisionTimer(timerInterval);
        _timer.Tick += OnTimerTick;
        _timer.Start();
        
        return Result<string>.Success;
    }

    /// <summary>
    /// Callback for the timer tick event. Writes the value to the anchor, and raises the <see cref="FreezeFailed"/>
    /// event if the write operation fails.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_anchor == null)
            return;
        
        var result = _anchor.Write(value);
        if (result.IsFailure)
            FreezeFailed?.Invoke(this, new FreezeFailureEventArgs<ValueAnchorFailure>(result.Error));
    }

    /// <summary>Interrupts freezing if <see cref="StartFreezing"/> had been previously called.</summary>
    public void Unfreeze()
    {
        if (!IsFreezing)
            return;

        _timer?.Stop();
        _timer = null;
        _anchor = null;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public void Dispose()
    {
        Unfreeze();
    }
}