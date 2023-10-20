namespace MindControl.State;

/// <summary>
/// An abstract watcher that provides a periodically refreshed state.
/// Inherit this class to track the state of your target process in real time.
/// </summary>
/// <typeparam name="T">Type of the state.</typeparam>
public abstract class StateWatcher<T>
{
    /// <summary>
    /// Timer that triggers automatic updates.
    /// </summary>
    private readonly IStateTimer _timer;

    /// <summary>
    /// Semaphore that prevents multiple updates from occurring at the same time.
    /// </summary>
    private readonly SemaphoreSlim _updateSemaphore;

    /// <summary>
    /// Gets a value indicating if this watcher is automatically refreshing state.
    /// </summary>
    public bool IsRunning => _timer.IsRunning;

    /// <summary>
    /// Gets or sets the target interval between two automatic state updates. Note that the actual number of updates
    /// per second might differ because of timer precision and overlapping updates.
    /// </summary>
    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }
    
    /// <summary>
    /// Gets the state read from the last successful update.
    /// </summary>
    public T? LatestState { get; protected set; }
    
    /// <summary>
    /// Gets the time of the last successful update.
    /// </summary>
    public DateTime? LatestUpdateTime { get; protected set; }
    
    /// <summary>
    /// Gets the last exception raised while attempting to read the state.
    /// </summary>
    public Exception? LatestException { get; protected set; }
    
    /// <summary>
    /// Gets the time of the last exception raised while attempting to read the state.
    /// </summary>
    public DateTime? LatestExceptionTime { get; protected set; }

    /// <summary>
    /// Event triggered after the state is updated.
    /// </summary>
    public event EventHandler<StateEventArgs<T>>? StateUpdated;

    /// <summary>
    /// Event triggered when a state update fails.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? StateUpdateFailed;

    /// <summary>
    /// Event triggered when a state update is skipped because another update is still in progress.
    /// </summary>
    public event EventHandler? StateUpdateSkipped;

    /// <summary>
    /// Builds a state watcher that updates the state automatically as many times as specified per second.
    /// </summary>
    /// <param name="targetUpdatesPerSecond">Target number of times per second the watcher should update the state.
    /// Note that the actual number of updates per second might differ because of timer precision and overlapping
    /// updates.</param>
    protected StateWatcher(float targetUpdatesPerSecond) : this(TimeSpan.FromSeconds(1/targetUpdatesPerSecond)) {}

    /// <summary>
    /// Builds a state watcher that updates the state automatically with a given time interval.
    /// </summary>
    /// <param name="updateInterval">Target interval between two state updates. Note that the actual number of updates
    /// per second might differ because of timer precision and overlapping updates.</param>
    protected StateWatcher(TimeSpan updateInterval) : this(new PrecisionTimer(updateInterval)) {}

    /// <summary>
    /// Builds a state watcher that updates the state automatically at every tick of the given timer.
    /// </summary>
    /// <param name="timer">Timer to use to trigger state updates.</param>
    protected StateWatcher(IStateTimer timer)
    {
        _updateSemaphore = new SemaphoreSlim(1, 1);
        _timer = timer;
        _timer.Tick += OnTimerTick; 
    }

    /// <summary>
    /// Starts automatically refreshing state.
    /// </summary>
    public void Start() => _timer.Start();

    /// <summary>
    /// Stops automatically refreshing state.
    /// </summary>
    public void Stop() => _timer.Stop();
    
    /// <summary>
    /// Callback. Called when the timer ticks.
    /// Causes a state update.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e) => UpdateState();

    /// <summary>
    /// Performs a state update. This method is automatically called at regular intervals when the watcher is running.
    /// </summary>
    public void UpdateState()
    {
        // Prevent multiple updates from occurring at the same time.
        if (!_updateSemaphore.Wait(0))
        {
            StateUpdateSkipped?.Invoke(this, EventArgs.Empty);
            return;
        }

        var previousState = LatestState;
        bool isStateUpdated = true;
        
        try
        {
            LatestState = ReadState();
            LatestUpdateTime = DateTime.Now;
        }
        catch (Exception e)
        {
            isStateUpdated = false;
            LatestException = e;
            LatestExceptionTime = DateTime.Now;
            StateUpdateFailed?.Invoke(this, new ExceptionEventArgs(e));
        }

        if (isStateUpdated)
        {
            OnBeforeUpdate(previousState, LatestState!);
            StateUpdated?.Invoke(this, new StateEventArgs<T>(LatestState!));
            OnAfterUpdate(previousState, LatestState!);
        }

        _updateSemaphore.Release();
    }

    /// <summary>
    /// Reads the current state, as watched by this instance.
    /// </summary>
    protected abstract T ReadState();

    /// <summary>
    /// Performs actions in-between getting a new state and raising the <see cref="StateUpdated"/> event.
    /// </summary>
    /// <param name="previousState">Previous state, before the update.</param>
    /// <param name="newState">State that was just read. Provided for convenience, but should be the same as
    /// the latest state of this instance.</param>
    protected virtual void OnBeforeUpdate(T? previousState, T newState) {}
    
    /// <summary>
    /// Performs actions after the update.
    /// </summary>
    /// <param name="previousState">Previous state, before the update occurred.</param>
    /// <param name="newState">State that was just read. Provided for convenience, but should be the same as
    /// the latest state of this instance.</param>
    protected virtual void OnAfterUpdate(T? previousState, T newState) {}
}