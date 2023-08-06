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
        _timer = timer;
    }
}