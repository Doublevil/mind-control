using MindControl.State;

namespace MindControl.Anchors;

/// <summary>
/// Event arguments used when a value observed by a <see cref="ValueWatcher{TValue,TReadFailure,TWriteFailure}"/>
/// changes.
/// </summary>
/// <param name="previousValue">Last known value before the change.</param>
/// <param name="newValue">New value freshly read after the change.</param>
/// <typeparam name="TValue">Type of the value that changed.</typeparam>
public class ValueChangedEventArgs<TValue>(TValue? previousValue, TValue newValue) : EventArgs
{
    /// <summary>Gets the last known value before the change.</summary>
    public TValue? PreviousValue => previousValue;
    
    /// <summary>Gets the new value freshly read after the change.</summary>
    public TValue NewValue => newValue;
}

/// <summary>
/// Event arguments used when a value observed by a <see cref="ValueWatcher{TValue,TReadFailure,TWriteFailure}"/>
/// becomes unreadable (causes a read failure). This may happen when the target process frees or rearranges its memory,
/// or when the related <see cref="ProcessMemory"/> instance is detached.
/// </summary>
/// <param name="lastKnownValue">Last value read before a read error occurred.</param>
public class ValueLostEventArgs<TValue>(TValue lastKnownValue) : EventArgs
{
    /// <summary>Gets the last value read before a read error occurred.</summary>
    public TValue LastKnownValue => lastKnownValue;
}

/// <summary>
/// Event arguments used when a value observed by a <see cref="ValueWatcher{TValue,TReadFailure,TWriteFailure}"/>
/// is successfully read after being lost.
/// </summary>
/// <param name="newValue">New value freshly read.</param>
/// <typeparam name="TValue">Type of the value that was reacquired.</typeparam>
public class ValueReacquiredEventArgs<TValue>(TValue newValue) : EventArgs
{
    /// <summary>Gets the new value freshly read.</summary>
    public TValue NewValue => newValue;
}

/// <summary>
/// Uses a timer to periodically read a value from a given anchor and raise events when the value changes.
/// </summary>
/// <typeparam name="TValue">Type of the value held by the anchor.</typeparam>
/// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
/// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
public class ValueWatcher<TValue, TReadFailure, TWriteFailure> : IDisposable
{
    private readonly ValueAnchor<TValue, TReadFailure, TWriteFailure> _anchor;
    private readonly PrecisionTimer _timer;
    private bool _isDisposed;
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    /// <summary>Event raised when the value observed by the watcher changes.</summary>
    public event EventHandler<ValueChangedEventArgs<TValue>>? ValueChanged;
    
    /// <summary>
    /// Event raised when the value observed by the watcher becomes unreadable (causes a read failure). This may happen
    /// when the target process frees or rearranges its memory, or when the related <see cref="ProcessMemory"/> instance
    /// is detached.
    /// </summary>
    public event EventHandler<ValueLostEventArgs<TValue>>? ValueLost;
    
    /// <summary>
    /// Event raised when the value observed by the watcher is successfully read after being lost.
    /// </summary>
    public event EventHandler<ValueReacquiredEventArgs<TValue>>? ValueReacquired;
    
    /// <summary>Gets a value indicating whether the last read operation was successful.</summary>
    public bool IsValueReadable { get; private set; }
    
    /// <summary>
    /// Gets the last value read by the watcher. This value is updated every time the watcher successfully reads a new
    /// value. Even if the value is lost, this property will still hold the last successfully read value. If the value
    /// was never read successfully, this property will hold the default value of <typeparamref name="TValue"/>.
    /// </summary>
    public TValue? LastKnownValue { get; private set; }
    
    /// <summary>Gets the last time the value either changed, was lost, or reacquired after a loss.</summary>
    public DateTime LastChangeTime { get; private set; }

    /// <summary>
    /// Uses a timer to periodically read a value from a given anchor and raise events when the value changes.
    /// </summary>
    /// <param name="anchor">The anchor holding the value to watch.</param>
    /// <param name="refreshInterval">Target time interval between each read operation.</param>
    /// <typeparam name="TValue">Type of the value held by the anchor.</typeparam>
    /// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
    /// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
    public ValueWatcher(ValueAnchor<TValue, TReadFailure, TWriteFailure> anchor, TimeSpan refreshInterval)
    {
        _anchor = anchor;
        LastChangeTime = DateTime.Now;
        LastKnownValue = default;
        _timer = new PrecisionTimer(refreshInterval);
        _timer.Tick += OnTimerTick;
        _timer.Start();
        
        // Build the initial state
        UpdateState(false);
    }

    /// <summary>
    /// Callback for the timer tick event. Reads the value from the anchor and raises events when the value changes.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e) => UpdateState();

    /// <summary>
    /// Reads the value from the anchor and raises events when the value changes.
    /// This method is called automatically by the timer, but can also be called manually to force a read.
    /// </summary>
    public void UpdateState() => UpdateState(true);

    /// <summary>
    /// Reads the value from the anchor and raises events when the value changes, unless the
    /// <paramref name="issueEvents"/> parameter is set to false.
    /// </summary>
    /// <param name="issueEvents">Whether to raise events when the value changes.</param>
    private void UpdateState(bool issueEvents)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_updateLock.Wait(0))
        {
            // An update is already in progress.
            // Wait for the update to finish, and return without updating (which would almost certainly be pointless).
            // This ensures no concurrent updates, and also that manual callers of UpdateState can expect that the
            // state is updated after the method returns.
            _updateLock.Wait();
            _updateLock.Release();
            return;
        }

        try
        {
            bool lastReadWasSuccessful = IsValueReadable && LastKnownValue != null;
            var previousValue = LastKnownValue;
            var result = _anchor.Read();
            IsValueReadable = result.IsSuccess;
            if (IsValueReadable)
            {
                var newValue = result.Value;
                bool valueChanged = newValue?.Equals(LastKnownValue) == false;
                if (lastReadWasSuccessful && !valueChanged)
                    return;
            
                LastChangeTime = DateTime.Now;
                LastKnownValue = newValue;
                if (!issueEvents)
                    return;
                
                if (!lastReadWasSuccessful)
                    ValueReacquired?.Invoke(this, new ValueReacquiredEventArgs<TValue>(newValue));
                if (valueChanged)
                    ValueChanged?.Invoke(this, new ValueChangedEventArgs<TValue>(previousValue, newValue));
            }
            else if (lastReadWasSuccessful)
            {
                LastChangeTime = DateTime.Now;
                if (issueEvents && previousValue != null)
                    ValueLost?.Invoke(this, new ValueLostEventArgs<TValue>(previousValue));
            }
        }
        finally
        {
            _updateLock.Release();
        }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _timer.Stop();
        _isDisposed = true;
        _updateLock.Dispose();
        ValueChanged = null;
        ValueLost = null;
        ValueReacquired = null;
        GC.SuppressFinalize(this);
    }
}