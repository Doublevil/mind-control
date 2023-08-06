// Written with help from https://stackoverflow.com/a/41697139/548894.

using System.Diagnostics;

namespace MindControl.State;

/// <summary>
/// High precision non-overlapping timer.
/// </summary>
public class PrecisionTimer : IStateTimer
{
    /// <summary>
    /// Tick time length in [ms]
    /// </summary>
    private static readonly double TickLength = 1000f / Stopwatch.Frequency;
    
    /// <summary>
    /// Invoked when the timer is elapsed
    /// </summary>
    public event EventHandler? Tick;

    /// <summary>
    /// The interval between two timer ticks, in milliseconds.
    /// </summary>
    private volatile float _interval;

    /// <summary>
    /// The timer is running
    /// </summary>
    private volatile bool _isRunning;

    /// <summary>
    /// Gets or sets a value indicating if the timer is currently running.
    /// </summary>
    public bool IsEnabled
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value)
                return;

            // Start or stop depending on the value wanted.
            if (value)
                Start();
            else
                Stop();
        }
    }

    /// <summary>
    /// Gets or sets the max duration of a stopwatch before it is restarted.
    /// This setting helps prevent precision problems when the stopwatch has been running for too long.
    /// The default value is one hour.
    /// </summary>
    public TimeSpan? RestartThreshold { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Execution thread.
    /// </summary>
    private Thread? _thread;

    /// <summary>
    /// Builds a <see cref="PrecisionTimer"/> with the given properties.
    /// </summary>
    /// <param name="interval">Interval between two automatic ticks</param>
    public PrecisionTimer(TimeSpan interval)
    {
        Interval = interval;
    }

    /// <summary>
    /// Gets or sets the interval between two automatic ticks.
    /// </summary>
    public TimeSpan Interval
    {
        get => TimeSpan.FromMilliseconds(_interval);
        set => _interval = (float)value.TotalMilliseconds;
    }

    /// <summary>
    /// If true, sets the execution thread to ThreadPriority.Highest (applies at the next start).
    /// This setting might improve performance in some cases, but might cause issues in other cases.
    /// </summary>
    public bool UseHighPriorityThread { get; set; } = false;

    /// <summary>
    /// Starts the timer.
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _thread = new Thread(ExecuteTimer)
        {
            IsBackground = true,
        };

        if (UseHighPriorityThread)
            _thread.Priority = ThreadPriority.Highest;
        
        _thread.Start();
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void Stop(bool joinThread = true)
    {
        _isRunning = false;

        // Even if _thread.Join may take time it is guaranteed that 
        // Elapsed event is never called overlapped with different threads
        if (_thread != null && joinThread && Thread.CurrentThread != _thread)
            _thread.Join();
    }

    /// <summary>
    /// Method started in a specific thread, that runs until the timer is stopped.
    /// Triggers the timer ticks as precisely as possible.
    /// </summary>
    private void ExecuteTimer()
    {
        var nextTrigger = 0f;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (_isRunning)
        {
            nextTrigger += _interval;

            while (true)
            {
                var elapsed = ElapsedHiRes(stopwatch);
                var diff = nextTrigger - elapsed;
                if (diff <= 0f)
                    break;

                switch (diff)
                {
                    case < 1f:
                        Thread.SpinWait(10);
                        break;
                    case < 5f:
                        Thread.SpinWait(100);
                        break;
                    case < 15f:
                        Thread.Sleep(1);
                        break;
                    default:
                        Thread.Sleep(10);
                        break;
                }

                if (!_isRunning)
                    return;
            }
            
            Tick?.Invoke(this, EventArgs.Empty);
            
            if (!_isRunning)
                return;
            
            // Restart the timer if needed, to prevent precision problems
            if (RestartThreshold != null && stopwatch.Elapsed >= RestartThreshold.Value)
            {
                stopwatch.Restart();
                nextTrigger = 0f;
            }
        }

        stopwatch.Stop();
    }
    
    /// <summary>
    /// Manually triggers a tick.
    /// </summary>
    public void ForceTick()
    {
        // Just tick. No need to adjust anything else for the purpose of this timer.
        Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the elapsed duration of the given stopwatch in milliseconds.
    /// </summary>
    /// <param name="stopwatch">Target stopwatch.</param>
    private static double ElapsedHiRes(Stopwatch stopwatch) => stopwatch.ElapsedTicks * TickLength;
}