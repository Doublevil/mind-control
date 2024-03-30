using System.Diagnostics;

namespace MindControl;

/// <summary>
/// Provides a <see cref="ProcessMemory"/> for a process identified by its name.
/// The tracker is able to re-attach to a process with the same name after it has been closed and reopened.
/// </summary>
public class ProcessTracker : IDisposable
{
    private readonly string _processName;
    private readonly SemaphoreSlim _instanceSemaphore = new(1, 1);
    private ProcessMemory? _processMemory;
    
    /// <summary>
    /// Gets the name of the process tracked by this instance.
    /// </summary>
    public string ProcessName => _processName;
    
    /// <summary>
    /// Gets a value indicating if the process is currently attached.
    /// </summary>
    public bool IsAttached => _processMemory?.IsAttached == true;

    /// <summary>
    /// Event raised when attaching to the target process.
    /// </summary>
    public event EventHandler? Attached;
    
    /// <summary>
    /// Event raised when detached from the target process.
    /// </summary>
    public event EventHandler? Detached;
    
    /// <summary>
    /// Builds a tracker for the process with the given name.
    /// </summary>
    /// <param name="processName">Name of the process to track.</param>
    public ProcessTracker(string processName)
    {
        _processName = processName;
    }
    
    /// <summary>
    /// Gets the <see cref="ProcessMemory"/> instance for the currently attached process.
    /// If no process is attached yet, make an attempt to attach to the target process by its name.
    /// Returns null if no process with the target name can be found.
    /// </summary>
    public ProcessMemory? GetProcessMemory()
    {
        // Use a semaphore here to make sure we never attach twice or attach while detaching.
        _instanceSemaphore.Wait();
        try
        {
            if (_processMemory?.IsAttached != true)
            {
                _processMemory = AttemptToAttachProcess();

                if (_processMemory != null)
                {
                    _processMemory.ProcessDetached += OnProcessDetached;
                    Attached?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        finally
        {
            _instanceSemaphore.Release();
        }
        return _processMemory;
    }

    /// <summary>
    /// Callback. Called when the process memory detaches.
    /// </summary>
    private void OnProcessDetached(object? sender, EventArgs e) => Detach();

    /// <summary>
    /// Makes sure the memory instance is detached.
    /// </summary>
    private void Detach()
    {
        // Reserve the semaphore to prevent simultaneous detach/attach operations.
        _instanceSemaphore.Wait();

        try
        {
            if (_processMemory != null)
            {
                _processMemory.ProcessDetached -= OnProcessDetached;
                _processMemory?.Dispose();
                Detached?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            // Swallow the exception - we don't care about something happening while detaching. 
        }
        
        _processMemory = null;
        _instanceSemaphore.Release();
    }

    /// <summary>
    /// Attempts to locate and attach the target process, and returns the resulting process memory instance.
    /// </summary>
    private ProcessMemory? AttemptToAttachProcess()
    {
        var process = GetTargetProcess();
        return process == null ? null : new ProcessMemory(process, ownsProcessInstance: true);
    }
    
    /// <summary>
    /// Gets the first process running with the target name. Returns null if no process with the given name is found. 
    /// </summary>
    private Process? GetTargetProcess()
    {
        var processes = Process.GetProcessesByName(_processName);

        switch (processes.Length)
        {
            case 0:
                return null;
            case 1:
                return processes.Single();
            default:
                // If we found multiple processes, take one (arbitrarily, the lowest by ID) and dispose the others.
                var target = processes.MinBy(p => p.Id);
                foreach (var process in processes.Except(new [] { target }))
                {
                    process?.Dispose();
                }
                return target;
        }
    }

    /// <summary>
    /// Releases the underlying process memory if required.
    /// </summary>
    public void Dispose()
    {
        Detach();
        _instanceSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}