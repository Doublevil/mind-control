using MindControl.Native;
using MindControl.Results;

namespace MindControl.Threading;

/// <summary>Represents an awaitable thread running an operation in another process.</summary>
public class RemoteThread : IDisposable
{
    private readonly IOperatingSystemService _osService;
    private readonly IntPtr _threadHandle;
    
    /// <summary>Gets a value indicating whether the thread has been disposed.</summary>
    protected bool IsDisposed { get; private set; }
    
    /// <summary>Initializes a new instance of the <see cref="RemoteThread"/> class.</summary>
    /// <param name="osService">Operating system service used to interact with the thread.</param>
    /// <param name="threadHandle">Handle to the thread.</param>
    internal RemoteThread(IOperatingSystemService osService, IntPtr threadHandle)
    {
        _osService = osService;
        _threadHandle = threadHandle;
    }
    
    /// <summary>
    /// Synchronously waits for the thread to finish execution and returns its exit code.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for the thread to finish execution. If the duration is exceeded, the
    /// result will hold a <see cref="ThreadWaitTimeoutFailure"/> instance.</param>
    /// <returns>A result holding either the exit code of the thread, or a failure.</returns>
    public Result<uint> WaitForCompletion(TimeSpan timeout)
    {
        if (IsDisposed)
            return new DisposedThreadFailure();
        
        var waitResult = _osService.WaitThread(_threadHandle, timeout);
        if (waitResult.IsFailure)
            return waitResult.Failure;
        Dispose();
        return waitResult.Value;
    }
    
    /// <summary>
    /// Asynchronously waits for the thread to finish execution and returns its exit code. This method is just an
    /// asynchronous wrapper around <see cref="WaitForCompletion"/>.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for the thread to finish execution. If the duration is exceeded, the
    /// result will contain a <see cref="ThreadWaitTimeoutFailure"/> error.</param>
    /// <returns>A result holding either the exit code of the thread, or a failure.</returns>
    public Task<Result<uint>> WaitForCompletionAsync(TimeSpan timeout) => Task.Run(() => WaitForCompletion(timeout));

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public virtual void Dispose()
    {
        if (IsDisposed)
            return;
        
        _osService.CloseHandle(_threadHandle);
        IsDisposed = true;
    }
}