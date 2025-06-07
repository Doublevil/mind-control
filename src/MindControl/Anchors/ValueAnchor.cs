using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>Provides methods to manipulate and track a specific value in memory.</summary>
/// <param name="memoryAdapter">Adapter that reads and writes the value from and to memory.</param>
/// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
/// <typeparam name="TValue">Type of the value to read and write.</typeparam>
public class ValueAnchor<TValue>(IMemoryAdapter<TValue> memoryAdapter, ProcessMemory processMemory)
{
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<TValue> Read() => memoryAdapter.Read(processMemory);

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Write(TValue value) => memoryAdapter.Write(processMemory, value);

    /// <summary>Freezes the memory area that the anchor is attached to, constantly overwriting its value, until the
    /// resulting instance is disposed.</summary>
    /// <returns>A <see cref="TimerValueFreezer{TValue}"/> instance that can be disposed to
    /// stop freezing the value, and provides a subscribable event raised when a recurrent write operation fails.
    /// </returns>
    public TimerValueFreezer<TValue> Freeze(TValue value)
    {
        // Use an arbitrary 150 updates per second as the default interval (a bit more than the standard 144FPS).
        return new TimerValueFreezer<TValue>(this, value, TimeSpan.FromSeconds(1 / 150f));
    }
    
    /// <summary>
    /// Provides a <see cref="ValueWatcher{TValue}"/> instance that periodically reads the
    /// value from the anchor and raises events when the value changes, until it is disposed.
    /// </summary>
    /// <param name="refreshInterval">Target time interval between each read operation in the watcher.</param>
    /// <returns>A <see cref="ValueWatcher{TValue}"/> instance that periodically reads the
    /// value from the anchor and raises events when the value changes, until it is disposed.</returns>
    public ValueWatcher<TValue> Watch(TimeSpan refreshInterval) => new(this, refreshInterval);
    
    /// <summary>
    /// Provides a <see cref="ValueWatcher{TValue}"/> instance that periodically reads the
    /// value from the anchor and raises events when the value changes, until it is disposed.
    /// </summary>
    /// <param name="updatesPerSecond">Target number of reads per second of the watcher.</param>
    /// <returns>A <see cref="ValueWatcher{TValue}"/> instance that periodically reads the
    /// value from the anchor and raises events when the value changes, until it is disposed.</returns>
    public ValueWatcher<TValue> Watch(int updatesPerSecond) => new(this, TimeSpan.FromSeconds(1f / updatesPerSecond));
}