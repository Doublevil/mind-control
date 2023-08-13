namespace MindControl.State;

/// <summary>
/// Event arguments for events related to arbitrary process states.
/// </summary>
public class StateEventArgs<T> : EventArgs
{
    /// <summary>
    /// Gets the underlying state.
    /// </summary>
    public T State { get; }

    /// <summary>
    /// Builds a <see cref="StateEventArgs{T}"/> with the given properties.
    /// </summary>
    /// <param name="state">State related to the event.</param>
    public StateEventArgs(T state)
    {
        State = state;
    }
}