using MindControl.Native;

namespace MindControl.Threading;

/// <summary>
/// Represents an awaitable thread running an operation in another process, with parameters temporarily stored in the
/// target process memory.
/// </summary>
public class ParameterizedRemoteThread : RemoteThread
{
    private readonly MemoryReservation _parameterReservation;

    /// <summary>Initializes a new instance of the <see cref="ParameterizedRemoteThread"/> class.</summary>
    /// <param name="osService">Operating system service used to interact with the thread.</param>
    /// <param name="threadHandle">Handle to the thread.</param>
    /// <param name="parameterReservation">Memory reservation containing the parameters to pass to the thread. The
    /// reservation will automatically be freed when the thread is awaited to completion or when this instance is
    /// disposed.</param>
    internal ParameterizedRemoteThread(IOperatingSystemService osService, IntPtr threadHandle,
        MemoryReservation parameterReservation)
        : base(osService, threadHandle)
    {
        _parameterReservation = parameterReservation;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public override void Dispose()
    {
        if (IsDisposed)
            return;
        
        base.Dispose();
        _parameterReservation.Dispose();
    }
}