namespace MindControl.State;

/// <summary>
/// Timer based on <see cref="System.Threading.Timer"/>.
/// </summary>
public class ThreadingTimer : IStateTimer
{
    private bool _isRunning;
    private readonly Timer _timer;

    /// <summary>
    /// Gets or sets a value indicating if the timer is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    private TimeSpan _interval;

    /// <summary>
    /// Gets or sets the interval between two automatic ticks.
    /// </summary>
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            UpdateTimer();
        }
    }

    /// <summary>
    /// Event triggered when the timer ticks, either automatically or manually.
    /// </summary>
    public event EventHandler? Tick;
    
    /// <summary>
    /// Builds a <see cref="ThreadingTimer"/> with the given properties.
    /// </summary>
    /// <param name="interval">Interval between two automatic ticks.</param>
    public ThreadingTimer(TimeSpan interval)
    {
        _interval = interval;
        _timer = new Timer(OnElapsed, null, Timeout.InfiniteTimeSpan, interval);
    }

    /// <summary>
    /// Starts the timer, if it is not already running.
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        UpdateTimer();
    }

    /// <summary>
    /// Stops the timer, if it was running.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        UpdateTimer();
    }

    /// <summary>
    /// Updates the timer to match the current state.
    /// </summary>
    private void UpdateTimer()
    {
        _timer.Change(_isRunning ? Interval : Timeout.InfiniteTimeSpan, Interval);
    }

    /// <summary>
    /// Callback. Called when the timer ticks.
    /// </summary>
    private void OnElapsed(object? _)
    {
        if (_isRunning)
            Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Manually triggers a tick.
    /// </summary>
    public void ForceTick()
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}