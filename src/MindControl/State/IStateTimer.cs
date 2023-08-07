namespace MindControl.State;

/// <summary>
/// Provides a common interface for timers that can tick on their own but also be forced to tick manually.
/// </summary>
public interface IStateTimer
{
    /// <summary>
    /// Gets a value indicating if the timer is currently running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Gets or sets the interval between two automatic ticks.
    /// </summary>
    TimeSpan Interval { get; set; }
    
    /// <summary>
    /// Event triggered when the timer ticks, either automatically or manually.
    /// </summary>
    event EventHandler? Tick;

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Manually triggers a tick.
    /// </summary>
    void ForceTick();
}